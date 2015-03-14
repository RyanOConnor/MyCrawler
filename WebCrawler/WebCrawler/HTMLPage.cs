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

namespace WebCrawler
{
    public class ChildPage : HTMLPage
    {
        private ManualResetEvent ManualEvent { get; set; }
        public HtmlDocument HtmlDoc { get; private set; }

        public ChildPage(string url, DateTime timeStamp, ManualResetEvent manualEvent)
            :base(url, timeStamp)
        {
            this.ManualEvent = manualEvent;
        }

        protected override void GetResponse(IAsyncResult webRequest)
        {
            HttpWebRequest request = (HttpWebRequest)webRequest.AsyncState;
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.EndGetResponse(webRequest);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string htmlString = DecompressHtml(response);

                    HtmlDoc = new HtmlDocument();
                    HtmlDoc.LoadHtml(htmlString);

                    Console.WriteLine("\n[{0}] - {2}seconds - Loaded {1}", DateTime.Now.TimeOfDay, Domain.Host, (DateTime.Now - this.TimeStamp).TotalSeconds);
                    ManualEvent.Set();
                }
                else
                {
                    this.SetWaitTime(10000);
                    WebCrawler.Instance.EnqueueWork(this);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Domain.AbsoluteUri);
                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine(ex.ToString());
                this.SetWaitTime(WaitTime + 10000);
                if (WaitTime < 60000)
                {
                    WebCrawler.Instance.EnqueueWork(this);
                }
                else
                {
                    // NOTIFY APPLICATION
                    ManualEvent.Set();
                }
            }
        }
    }

    [DataContract]
    public class HTMLPage
    {
        [DataMember]
        public string ID { get; private set; }
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
        [DataMember]
        private List<string> rankedResults;
        public int WaitTime { get; private set; }

        public HTMLPage(string url, DateTime timeStamp)
        {
            this.URL = url;
            this.TimeStamp = timeStamp;
            this.Domain = new Uri(url);
        }

        public HTMLPage(string id, string url, DateTime timeStamp, List<string> tags, List<string> keywords)
        {
            this.ID = id;
            this.HtmlTags = tags;
            this.Keywords = keywords;
            Domain = new Uri(url);
        }

        [OnDeserialized]
        private void SetUri(StreamingContext context)
        {
            Domain = new Uri(URL);
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
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            request.Proxy = null;

            IAsyncResult response = request.BeginGetResponse(new AsyncCallback(GetResponse), request);
        }

        protected virtual void GetResponse(IAsyncResult webRequest)
        {
            HttpWebRequest request = (HttpWebRequest)webRequest.AsyncState;
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.EndGetResponse(webRequest);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string htmlString = DecompressHtml(response);
                    HashSet<string> results = FilterByTags(htmlString);

                    if (Keywords.Count != 0)
                    {
                        List<ChildPage> linkedToPages = LoadChildPages(results.ToList());
                        rankedResults = FilterByKeyWords(linkedToPages);
                    }

                    WebCrawler.Instance.EnqueueResult(this);
                }
                else
                {
                    this.SetWaitTime(10000);
                    WebCrawler.Instance.EnqueueWork(this);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(URL);
                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine(ex.ToString());
                this.SetWaitTime(WaitTime + 10000);
                if (WaitTime < 60000)
                {
                    WebCrawler.Instance.EnqueueWork(this);
                }
                else
                {
                    // NOTIFY APPLICATION
                }
            }
        }

        protected string DecompressHtml(HttpWebResponse response)
        {
            string decompressedHTML = string.Empty;
            Encoding encoding = GetEncodingFromHeader(response);
            try
            {
                using (Stream stream = GetResponseStream(response, 10000))
                {
                    using (StreamReader reader = new StreamReader(stream, encoding))
                    {
                        decompressedHTML = reader.ReadToEnd().Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            string query = HtmlTags[0];
            for(int i = 1; i < HtmlTags.Count-1; i++)
            {
                if(HtmlTags[i].ElementAt(0) == '.' || HtmlTags[i].ElementAt(0) == '#')
                {
                    query += HtmlTags[i];
                }
                else
                {
                    query += ' ' + HtmlTags[i];
                }
            }

            IEnumerable<string> queryResults = null;
            if(HtmlTags[HtmlTags.Count-1] == "href")
            {
                queryResults = htmlDoc.DocumentNode.QuerySelectorAll(query).Select(x => x.Attributes["href"].Value);
            }
            else if(HtmlTags[HtmlTags.Count-1] == "text")
            {
                queryResults = htmlDoc.DocumentNode.QuerySelectorAll(query).Select(x => x.InnerText).ToList();
            }
            else
            { 
                throw new Exception();
            }

            HashSet<string> links = new HashSet<string>(queryResults);

            return FixUrls(links);
        }

        private List<string> FilterByKeyWords(List<ChildPage> childPages)
        {
            List<KeyValuePair<string, int>> results = new List<KeyValuePair<string, int>>();

            foreach(ChildPage page in childPages)
            {
                string innerText = page.HtmlDoc.DocumentNode.InnerText.ToLower();
                int pageScore = 0;
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
                results.Add(new KeyValuePair<string, int>(page.Domain.AbsoluteUri, pageScore));
            }

            var sortedList = from entry in results orderby entry.Value descending select entry;

            PrintRankedPages(sortedList);
           
            return sortedList.Select(x => x.Key).ToList();
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

        private List<ChildPage> LoadChildPages(List<string> results)
        {
            List<ChildPage> childPages = new List<ChildPage>();

            ManualResetEvent signal = new ManualResetEvent(false);
            Thread parallelWait = new Thread(() => WaitForChildPages(signal, results.Count));
            parallelWait.Start();

            foreach (string url in results)
            {
                ChildPage page = new ChildPage(url, DateTime.Now, signal);
                WebCrawler.Instance.EnqueueWork(page);
                childPages.Add(page);
            }

            parallelWait.Join();

            return childPages;
        }

        private void WaitForChildPages(ManualResetEvent signal, int count)
        {
            int numberOfTasks = count;

            while (numberOfTasks != 0)
            {
                signal.WaitOne();
                numberOfTasks--;
                signal.Reset();
            }
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
