using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.Serialization;
using System.IO.Compression;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace WebCrawlerNode
{
    public class HtmlRecord : Serializable
    {
        [BsonId]
        public ObjectId recordid { get; set; }
        public string url { get; set; }
        public Uri domain { get; set; }
        public DateTime timeStamp { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ObjectId, HtmlResults> results { get; set; }
        public HttpStatusCode serverResponse { get; set; }
        [BsonIgnore]
        public int waitTime { get; private set; }
        [BsonIgnore]
        private List<ChildPage> childPages { get; set; }
        [BsonIgnore]
        private ManualResetEvent waitForPages;
        [BsonIgnore]
        public DateTime timeCreated { get; set; }
        [BsonIgnore]
        public JobStatus jobStatus { get; set; }
        [BsonIgnore]
        public volatile bool jobDestroyed = false;

        public HtmlRecord()
        { }

        public HtmlRecord(string url, DateTime timeStamp)
        {
            this.url = url;
            this.timeCreated = timeStamp;
            this.domain = new Uri(url);
        }

        public void Initialize()
        {
            foreach(HtmlResults result in results.Values)
            {
                result.links = new Dictionary<string, Link>();
            }
        }

        public void SetWaitTime(int milliseconds)
        {
            this.waitTime = milliseconds;
            this.timeCreated = DateTime.Now;
        }

        public int MillisecondsLeftToWait()
        {
            return waitTime - (int)((DateTime.Now - this.timeCreated).TotalMilliseconds);
        }

        public void SleepThenUpdate(object stateInfo)
        {
            if (MillisecondsLeftToWait() > 0)
            {
                lock (this)
                {
                    Monitor.Wait(this, TimeSpan.FromMilliseconds(MillisecondsLeftToWait()));
                }
            }

            if (!jobDestroyed)
            {
                BeginUpdate();
            }
        }

        public void BeginUpdate()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(domain.AbsoluteUri);
            request.Method = WebRequestMethods.Http.Get;
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            request.ProtocolVersion = HttpVersion.Version10;
            request.ServicePoint.ConnectionLeaseTimeout = 5000;
            request.ServicePoint.MaxIdleTime = 5000;
            request.KeepAlive = false;
            request.Proxy = null;

            jobStatus = JobStatus.Requesting;
            IAsyncResult response = request.BeginGetResponse(new AsyncCallback(GetResponse), request);
        }

        private void GetResponse(IAsyncResult webRequest)
        {
            HttpWebRequest request = (HttpWebRequest)webRequest.AsyncState;
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.EndGetResponse(webRequest);
                System.Diagnostics.Debug.Print("\tLoading: " + domain.AbsoluteUri);
            }
            catch (WebException webEx)
            {
                System.Diagnostics.Debug.Print("\t[" + domain.AbsoluteUri + "]");
                System.Diagnostics.Debug.Print("\t" + webEx.ToString());

                if (this is HtmlRecord)
                {
                    jobStatus = JobStatus.ErrorRequesting;
                }
                else
                {
                    ChildPage page = this as ChildPage;
                    page.InvokeLoadErrorEvent();
                }

                if (waitTime < WebCrawler.TimeoutPeriod)
                {
                    HttpWebResponse resp = webEx.Response as HttpWebResponse;
                    if (resp != null)
                    {
                        HttpStatusCode statuscode = resp.StatusCode;
                        switch (statuscode)
                        {
                            case HttpStatusCode.Forbidden:
                                serverResponse = statuscode;
                                KillProcess();
                                break;

                            case HttpStatusCode.BadRequest:
                                serverResponse = statuscode;
                                KillProcess();
                                break;

                            default:
                                SetWaitTime(waitTime + 10000);
                                WebCrawler.Instance.EnqueueWork(this);
                                break;
                        }
                    }
                    else
                    {
                        SetWaitTime(waitTime + 10000);
                        WebCrawler.Instance.EnqueueWork(this);
                    }
                }
                else
                {
                    if (this is ChildPage)
                    {
                        // Allow parent thread to continue, disregard this web page
                        ChildPage page = this as ChildPage;
                        page.InvokeLoadedEvent();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            finally
            {
                if (response != null)
                {
                    HandleResponse(response);
                }
            }
        }

        protected virtual void HandleResponse(HttpWebResponse response)
        {
            string htmlString = DecompressHtml(response);

            jobStatus = JobStatus.HandlingResponse;
            if (htmlString != string.Empty)
            {
                MultiValueDictionary<string, ObjectId> links = new MultiValueDictionary<string, ObjectId>();
                foreach (HtmlResults feed in results.Values)
                {
                    HashSet<string> filteredLinks = feed.FilterByTags(htmlString, domain);
                    foreach (string link in filteredLinks)
                    {
                        links.Add(link, feed.resultsid);
                    }
                }

                jobStatus = JobStatus.LoadingPages;
                LoadChildPages(links);

                foreach (ChildPage page in childPages)
                {
                    foreach (ObjectId jobId in page.jobIds)
                    {
                        results[jobId].AddChildPage(page);
                    }
                }

                jobStatus = JobStatus.RankingPages;
                foreach (HtmlResults feed in results.Values)
                {
                    feed.ProcessKeywordScores();
                }

                jobStatus = JobStatus.Finished;
                timeStamp = DateTime.UtcNow;
                WebCrawler.Instance.EnqueueResult(this);
            }
        }

        private void LoadChildPages(MultiValueDictionary<string, ObjectId> links)
        {
            childPages = new List<ChildPage>();
            waitForPages = new ManualResetEvent(false);

            foreach (KeyValuePair<string, IReadOnlyCollection<ObjectId>> pair in links)
            {
                ChildPage page = new ChildPage(pair.Key, DateTime.Now, pair.Value);

                page.WebPageLoaded += new EventHandler<ChildPage>(OnWebPageLoaded);
                page.LoadError += new EventHandler(OnLoadError);
                WebCrawler.Instance.EnqueueWork(page);
            }

            while (childPages.Count != links.Count)
            {
                waitForPages.Reset();
                waitForPages.WaitOne();
            }
        }

        private void OnWebPageLoaded(object sender, ChildPage page)
        {
            lock (childPages)
            {
                childPages.Add(page);
                waitForPages.Set();
            }
        }

        private void OnLoadError(object sender, EventArgs args)
        {
            jobStatus = JobStatus.ErrorLoading;
        }

        protected string DecompressHtml(HttpWebResponse response)
        {
            string decompressedHTML = string.Empty;
            Encoding encoding = GetEncodingFromHeader(response);
            try
            {
                using (Stream stream = GetResponseStream(response, -1))
                {
                    using (StreamReader reader = new StreamReader(stream, encoding))
                    {
                        decompressedHTML = reader.ReadToEnd().Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                SetWaitTime(waitTime + 10000);
                if (waitTime < 60000)
                {
                    WebCrawler.Instance.EnqueueWork(this);
                }
            }

            return decompressedHTML;
        }

        protected Stream GetResponseStream(HttpWebResponse response, int timeOut)
        {
            Stream stream;
            if (response.ContentEncoding.ToLower() == "gzip")
            {
                stream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress);
            }
            else if (response.ContentEncoding.ToLower() == "deflate" || response.ContentEncoding.ToLower() == "zlib")
            {
                stream = new DeflateStream(response.GetResponseStream(), CompressionMode.Decompress);
            }
            else
            {
                stream = response.GetResponseStream();
                if (stream.CanTimeout)
                {
                    stream.ReadTimeout = timeOut;
                }
            }

            return stream;
        }

        protected Encoding GetEncodingFromHeader(HttpWebResponse response)
        {
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
            string charset = string.Empty;
            if (string.IsNullOrEmpty(response.CharacterSet))
            {
                Match regx = Regex.Match(response.ContentType, @";\s*charset\s*=\s*(?<charset>.*)", RegexOptions.IgnoreCase);
                if (regx.Success)
                {
                    charset = regx.Groups["charset"].Value.Trim(new char[] { '\'', '"' });
                }
            }
            else
            {
                charset = response.CharacterSet;
            }

            if (!string.IsNullOrEmpty(charset))
            {
                try
                {
                    encoding = Encoding.GetEncoding(charset);
                }
                catch (ArgumentException ex)
                {
                    System.Diagnostics.Debug.Print(ex.ToString());
                    throw ex;
                }
            }

            return encoding;
        }

        public void KillProcess()
        {
            foreach (ChildPage childPage in childPages)
            {
                if (childPage.htmlDoc == null)
                {
                    childPage.jobDestroyed = true;
                    lock (childPage)
                    {
                        Monitor.Pulse(childPage);
                    }
                }
            }
            lock (this)
            {
                this.jobDestroyed = true;
                Monitor.Pulse(this);
            }
        }
    }
}
