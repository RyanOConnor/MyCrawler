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
        public HtmlDocument HtmlDoc { get; private set; }
        public event EventHandler<ChildPage> WebPageLoaded;

        public ChildPage(string url, DateTime timeStamp)
            :base(url, timeStamp)
        {  }

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

    [DataContract]
    public class HTMLPage : Serializable
    {
        [DataMember]
        public ObjectId UserId { get; set; }
        [DataMember]
        public ObjectId Id { get; private set; }
        [DataMember]
        private string URL { get; set; }
        [DataMember]
        public Uri Domain { get; private set; }
        [DataMember]
        public DateTime TimeStamp { get; private set; }
        [DataMember]
        private List<string> HtmlTags { get; set; }
        [DataMember]
        private List<string> Keywords { get; set; }
        [DataMember][BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        private Dictionary<string, int> RankedResults { get; set; }
        [DataMember]
        private HttpStatusCode ServerResponse { get; set; }
        public int WaitTime { get; private set; }
        private List<ChildPage> ChildPages { get; set; }
        private ManualResetEvent WaitForpages;

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
            ChildPages = new List<ChildPage>();
            WaitForpages = new ManualResetEvent(false);
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
            request.Timeout = 10000;
            request.KeepAlive = true;
            request.Proxy = null;

            IAsyncResult response = request.BeginGetResponse(new AsyncCallback(GetResponse), request);
        }

        private void GetResponse(IAsyncResult webRequest)
        {
            HttpWebRequest request = (HttpWebRequest)webRequest.AsyncState;
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.EndGetResponse(webRequest);
                Console.WriteLine("\n\t\t\tLoading {0}", Domain.AbsoluteUri);

                HandleResponse(response);
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
                                throw webEx;

                            case (HttpStatusCode.BadRequest):
                                throw webEx;

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
        }

        protected virtual void HandleResponse(HttpWebResponse response)
        {
            string htmlString = DecompressHtml(response);
            HashSet<string> results = FilterByTags(htmlString);

            if (Keywords.Count != 0)
            {
                LoadChildPages(results.ToList());
                RankedResults = RankByKeywords(ChildPages);
            }
            else
            {
                RankedResults = results.ToDictionary(key => key, value => 0);
            }

            WebCrawler.Instance.EnqueueResult(this);
        }

        private List<ChildPage> LoadChildPages(List<string> results)
        {
            ChildPages = new List<ChildPage>();

            foreach (string url in results)
            {
                ChildPage page = new ChildPage(url, DateTime.Now);
                
                page.WebPageLoaded += new EventHandler<ChildPage>(OnWebPageLoaded);
                WebCrawler.Instance.EnqueueWork(page);
            }

            while (ChildPages.Count != results.Count)
            {
                WaitForpages.Reset();
                WaitForpages.WaitOne();
            }

            return ChildPages;
        }

        private void OnWebPageLoaded(object sender, ChildPage page)
        {
            lock (ChildPages)
            {
                ChildPages.Add(page);
                WaitForpages.Set();
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

        private HashSet<string> FilterByTags(string html)
        {
            // (at application or client level, do not allow "noindex" or "nofollow" tags)
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                string query = HtmlTags[0];
                for (int i = 1; i < HtmlTags.Count - 1; i++)
                {
                    if (HtmlTags[i].ElementAt(0) == '.' || HtmlTags[i].ElementAt(0) == '#')
                    {
                        query += HtmlTags[i];
                    }
                    else
                    {
                        query += ' ' + HtmlTags[i];
                    }
                }

                IEnumerable<string> queryResults = null;
                if (HtmlTags[HtmlTags.Count - 1] == "href")
                {
                    queryResults = htmlDoc.DocumentNode.QuerySelectorAll(query).Select(x => x.Attributes["href"].Value);
                }
                else if (HtmlTags[HtmlTags.Count - 1] == "text")
                {
                    queryResults = htmlDoc.DocumentNode.QuerySelectorAll(query).Select(x => x.InnerText).ToList();
                }
                else
                {
                    throw new Exception();
                }

                HashSet<string> links = new HashSet<string>(queryResults);
                links = FixUrls(links);

                return links;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        private Dictionary<string, int> RankByKeywords(List<ChildPage> childPages)
        {
            IOrderedEnumerable<KeyValuePair<string, int>> sortedList = null;
            try
            {
                List<KeyValuePair<string, int>> results = new List<KeyValuePair<string, int>>();

                foreach (ChildPage page in childPages)
                {
                    int pageScore = 0;
                    if (page.HtmlDoc != null)
                    {
                        string innerText = page.HtmlDoc.DocumentNode.InnerText.ToLower();
                        foreach (string keyword in Keywords)
                        {
                            if (innerText.Contains(keyword.ToLower()))
                            {
                                pageScore += Regex.Matches(innerText, keyword).Count;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    results.Add(new KeyValuePair<string, int>(page.Domain.AbsoluteUri, pageScore));
                }

                sortedList = from entry in results orderby entry.Value descending select entry;

                //PrintRankedPages(sortedList);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return sortedList.ToDictionary(pair => pair.Key, pair => pair.Value);
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

        private HashSet<string> FixUrls(HashSet<string> urls)
        {
            urls.Remove(Domain.AbsoluteUri);

            HashSet<string> fixedUrlSet = new HashSet<string>();
            foreach(string url in urls)
            {
                Uri fixedUri;
                if(Uri.TryCreate(Domain, url, out fixedUri))
                {
                    fixedUrlSet.Add(fixedUri.AbsoluteUri);
                }
                else
                {
                    // do regular expression checking and string concatenation....
                    Match regx = Regex.Match(url, @"\.[a-z]{2,3}(\.[a-z]{2,3})?");
                    if (!regx.Success)
                    {
                        fixedUrlSet.Add(Domain.Scheme + "://" + Domain.Host + url);
                    }
                }
            }

            return fixedUrlSet;
        }

        private bool AllowedByRobotsTXT()
        {
            // load robots.txt, parse, if Uri is disallowed then return false

            return true;
        }
    }
}
