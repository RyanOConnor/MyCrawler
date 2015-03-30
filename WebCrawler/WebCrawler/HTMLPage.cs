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
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using System.Threading;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using System.Diagnostics;

namespace WebCrawler
{
    public class ChildPage : HTMLPage
    {
        public IReadOnlyCollection<ObjectId> JobIds { get; set; }
        public HtmlDocument HtmlDoc { get; private set; }
        public event EventHandler<ChildPage> WebPageLoaded;

        public ChildPage(string url, DateTime timeStamp, IReadOnlyCollection<ObjectId> ids)
            :base(url, timeStamp)
        {
            JobIds = ids;
        }

        protected override void HandleResponse(HttpWebResponse response)
        {
            string type = string.Empty;
            try
            {
                type = response.Headers["content-type"];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
            finally
            {
                if (NotBinaryFileType(type))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string htmlString = DecompressHtml(response);

                        HtmlDoc = new HtmlDocument();
                        HtmlDoc.LoadHtml(htmlString);
                    }
                }

                InvokeLoadedEvent();
            }
        }

        public void InvokeLoadedEvent()
        {
            EventHandler<ChildPage> webPageLoaded = this.WebPageLoaded;
            if(webPageLoaded != null)
            {
                webPageLoaded(null, this);
            }
        }

        private bool NotBinaryFileType(string type)
        {
            if (type.Contains("text") || type.Contains("html"))
                return true;
            else
                return false;
        }
    }

    public class HTMLPage : Serializable
    {
        [BsonId]
        public ObjectId Id { get; private set; }
        public string URL { get; set; }
        public Uri Domain { get; private set; }
        public DateTime TimeStamp { get; private set; }
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        private Dictionary<ObjectId, HtmlResults> Results { get; set; }
        public HttpStatusCode ServerResponse { get; set; }
        [BsonIgnore]
        public int WaitTime { get; private set; }
        [BsonIgnore]
        private List<ChildPage> ChildPages { get; set; }
        [BsonIgnore]
        private ManualResetEvent WaitForPages;

        public HTMLPage(string url, DateTime timeStamp)
        {
            this.URL = url;
            this.TimeStamp = timeStamp;
            this.Domain = new Uri(url);
        }

        [OnDeserialized]
        private void Initialize(StreamingContext context)
        {
            Domain = new Uri(URL);
            WaitForPages = new ManualResetEvent(false);
        }

        public void SetWaitTime(int milliseconds)
        {
            this.WaitTime = milliseconds;
            this.TimeStamp = DateTime.Now;
        }

        public int MillisecondsLeftToWait()
        {
            return WaitTime - (int)((DateTime.Now - this.TimeStamp).TotalMilliseconds);
        }

        public void SleepThenUpdate(object stateInfo)
        {
            if (MillisecondsLeftToWait() > 0)
                Thread.Sleep(MillisecondsLeftToWait());

            BeginUpdate();
        }

        public void BeginUpdate()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(Domain.AbsoluteUri);
            request.Method = WebRequestMethods.Http.Get;
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            request.ProtocolVersion = HttpVersion.Version10;
            //request.Timeout = 10000;
            request.KeepAlive = true;
            request.Proxy = null;

            IAsyncResult response = request.BeginGetResponse(new AsyncCallback(GetResponse), request);
        }

        private void GetResponse(IAsyncResult webRequest)
        {
            HttpWebRequest request = (HttpWebRequest)webRequest.AsyncState;
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.EndGetResponse(webRequest);
                Console.WriteLine("\n\t\t\tLoading {0}", Domain.AbsoluteUri);
            }
            catch (WebException webEx)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(URL);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(webEx.ToString());

                if (WaitTime < WebCrawler.TIMEOUT_PERIOD)
                {
                    HttpWebResponse resp = webEx.Response as HttpWebResponse;
                    if (resp != null)
                    {
                        HttpStatusCode statuscode = resp.StatusCode;
                        switch (statuscode)
                        {
                            case (HttpStatusCode.Forbidden):
                                ServerResponse = statuscode;
                                break;
                                //throw webEx;

                            case (HttpStatusCode.BadRequest):
                                break;
                                //throw webEx;

                            default:
                                SetWaitTime(WaitTime + 10000);
                                WebCrawler.Instance.EnqueueWork(this);
                                break;
                        }
                    }
                    else
                    {
                        SetWaitTime(WaitTime + 10000);
                        WebCrawler.Instance.EnqueueWork(this);
                    }
                }
                else
                {
                    if(this is ChildPage)
                    {
                        ChildPage page = this as ChildPage;
                        page.InvokeLoadedEvent();
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
            finally
            {
                if (response != null)
                    HandleResponse(response);                    
            }
        }

        protected virtual void HandleResponse(HttpWebResponse response)
        {
            string htmlString = DecompressHtml(response);

            if (Results.Any(obj => obj.Value.GetType() == typeof(TextUpdate)))
            {
                foreach (TextUpdate textUpdate in Results.Values)
                {
                    textUpdate.FilterByTags(htmlString);
                }
            }

            if (Results.Any(obj => obj.Value.GetType() == typeof(LinkFeed)))
            {
                MultiValueDictionary<string, ObjectId> links = new MultiValueDictionary<string, ObjectId>();
                foreach (LinkFeed feed in Results.Values)
                {
                    List<string> filteredLinks = feed.FilterByTags(htmlString);
                    foreach (string link in filteredLinks)
                    {
                        links.Add(link, feed.JobId);
                    }
                }

                LoadChildPages(links);

                foreach(ChildPage page in ChildPages)
                {
                    foreach(ObjectId jobId in page.JobIds)
                    {
                        Results[jobId].AddChildPage(page);
                    }
                }

                foreach(LinkFeed feed in Results.Values)
                {
                    feed.RankByKeywords();
                }
            }

            WebCrawler.Instance.EnqueueResult(this);
        }

        private void LoadChildPages(MultiValueDictionary<string, ObjectId> links)
        {
            ChildPages = new List<ChildPage>();
            WaitForPages = new ManualResetEvent(false);

            foreach (KeyValuePair<string, IReadOnlyCollection<ObjectId>> pair in links)
            {
                ChildPage page = new ChildPage(pair.Key, DateTime.Now, pair.Value);
                
                page.WebPageLoaded += new EventHandler<ChildPage>(OnWebPageLoaded);
                WebCrawler.Instance.EnqueueWork(page);
            }

            while (ChildPages.Count != links.Count)
            {
                WaitForPages.Reset();
                WaitForPages.WaitOne();
            }
        }

        private void OnWebPageLoaded(object sender, ChildPage page)
        {
            lock (ChildPages)
            {
                ChildPages.Add(page);
                WaitForPages.Set();
            }
        }

        protected string DecompressHtml(HttpWebResponse response)
        {
            string decompressedHTML = string.Empty;
            Encoding encoding = GetEncodingFromHeader(response);
            try
            {
                Stream stream = GetResponseStream(response, 10000);
                try
                {
                    StreamReader reader = new StreamReader(stream, encoding);
                    try
                    {
                        decompressedHTML = reader.ReadToEnd().Trim();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        throw ex;
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
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
            if(string.IsNullOrEmpty(response.CharacterSet))
            {
                Match regx = Regex.Match(response.ContentType, @";\s*charset\s*=\s*(?<charset>.*)", RegexOptions.IgnoreCase);
                if(regx.Success)
                {
                    charset = regx.Groups["charset"].Value.Trim(new char[] { '\'', '"' });
                }
            }
            else
            {
                charset = response.CharacterSet;
            }

            if(!string.IsNullOrEmpty(charset))
            {
                try
                {
                    encoding = Encoding.GetEncoding(charset);
                }
                catch(ArgumentException ex)
                {
                    Console.WriteLine(ex.ToString());
                    throw ex;
                }
            }

            return encoding;
        }

        private void PrintRankedPages(IOrderedEnumerable<KeyValuePair<string, int>> pages)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (KeyValuePair<string, int> pair in pages)
            {
                Console.WriteLine("[PageScore]: {0} \t [Page]: {1}", pair.Value, pair.Key);
            }
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private bool AllowedByRobotsTXT()
        {
            // load robots.txt, parse, if Uri is disallowed then return false

            return true;
        }
    }
}
