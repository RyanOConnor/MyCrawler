using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.Serialization;
using System.Diagnostics;
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
        private ManualResetEvent manualEvent { get; set; }

        public ChildPage(string url, DateTime timeStamp, ManualResetEvent manualEvent)
            :base(url, timeStamp)
        {
            this.manualEvent = manualEvent;
        }

        protected override void getResponse(IAsyncResult webRequest)
        {
            HttpWebRequest request = (HttpWebRequest)webRequest.AsyncState;
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.EndGetResponse(webRequest);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string htmlString = decompressHtml(response);

                    htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(htmlString);

                    Console.WriteLine("\n[{0}] - {2}seconds - Loaded {1}", DateTime.Now.TimeOfDay, domain.Host, (DateTime.Now - this.timeStamp).TotalSeconds);
                    manualEvent.Set();
                }
                else
                {
                    this.setWaitTime(10000);
                    WebCrawler.Enqueue(this);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(url);
                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine(ex.ToString());
                this.setWaitTime(waitTime + 10000);
                if (waitTime < 60000)
                {
                    WebCrawler.Enqueue(this);
                }
                else
                {
                    // NOTIFY APPLICATION
                    manualEvent.Set();
                }
            }
        }
    }

    [DataContract]
    public class HTMLPage
    {
        [DataMember]
        private string id { get; set; }
        [DataMember]
        public string url { get; private set; }
        [DataMember]
        public Uri domain { get; protected set; }
        [DataMember]
        public DateTime timeStamp { get; protected set; }
        [DataMember]
        private List<string> htmlTags { get; set; }
        [DataMember]
        private List<string> keywords { get; set; }
        [DataMember]
        private List<string> rankedResults { get; set; }
        protected HtmlDocument htmlDoc { get; set; }
        private List<ChildPage> childPages { get; set; }
        public int waitTime { get; set; }

        public HTMLPage(string url, DateTime timeStamp)
        {
            this.url = url;
            this.timeStamp = timeStamp;
            this.domain = new Uri(url);
        }

        public HTMLPage(string id, string url, DateTime timeStamp, List<string> tags, List<string> keywords)
        {
            this.id = id;
            this.htmlTags = tags;
            this.keywords = keywords;
            domain = new Uri(url);
        }

        [OnDeserialized]
        private void setUri(StreamingContext context)
        {
            domain = new Uri(url);
        }

        public void setWaitTime(int milliseconds)
        {
            this.waitTime = milliseconds;
            this.timeStamp = DateTime.Now;
        }

        public int millisecondsLeftToWait()
        {
            return waitTime - (int)((DateTime.Now - this.timeStamp).TotalMilliseconds);
        }

        public void sleepThenUpdate(object stateInfo)
        {
            if (millisecondsLeftToWait() > 0)
                Thread.Sleep(millisecondsLeftToWait());

            beginUpdate();
        }

        public void beginUpdate()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(domain.AbsoluteUri);
            request.Method = WebRequestMethods.Http.Get;
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            request.Proxy = null;

            IAsyncResult response = request.BeginGetResponse(new AsyncCallback(getResponse), request);
        }

        protected virtual void getResponse(IAsyncResult webRequest)
        {
            HttpWebRequest request = (HttpWebRequest)webRequest.AsyncState;
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.EndGetResponse(webRequest);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string html = decompressHtml(response);

                    HashSet<string> results = filterByTags(html);

                    if (keywords.Count != 0)
                    {
                        List<ChildPage> linkedToPages = loadChildPages(results.ToList());

                        rankedResults = filterByKeyWords(linkedToPages);

                        SocketServer.sendHTMLPage(this);
                    }
                }
                else
                {
                    this.setWaitTime(10000);
                    WebCrawler.Enqueue(this);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(url);
                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine(ex.ToString());
                this.setWaitTime(waitTime + 10000);
                if (waitTime < 60000)
                {
                    WebCrawler.Enqueue(this);
                }
                else
                {
                    // NOTIFY APPLICATION
                }
            }
        }

        protected string decompressHtml(HttpWebResponse response)
        {
            string decompressedHTML = string.Empty;
            Encoding encoding = getEncodingFromHeader(response);
            try
            {
                using (Stream stream = getResponseStream(response, 10000))
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

        protected Stream getResponseStream(HttpWebResponse response, int timeOut)
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

        protected Encoding getEncodingFromHeader(HttpWebResponse response)
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

        private HashSet<string> filterByTags(string html)
        {
            // (at application or client level, do not allow "noindex" or "nofollow" tags)
            htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            string query = htmlTags[0];
            for(int i = 1; i < htmlTags.Count-1; i++)
            {
                if(htmlTags[i].ElementAt(0) == '.' || htmlTags[i].ElementAt(0) == '#')
                {
                    query += htmlTags[i];
                }
                else
                {
                    query += ' ' + htmlTags[i];
                }
            }

            IEnumerable<string> queryResults = null;
            if(htmlTags[htmlTags.Count-1] == "href")
            {
                queryResults = htmlDoc.DocumentNode.QuerySelectorAll(query).Select(x => x.Attributes["href"].Value);
            }
            else if(htmlTags[htmlTags.Count-1] == "text")
            {
                queryResults = htmlDoc.DocumentNode.QuerySelectorAll(query).Select(x => x.InnerText).ToList();
            }
            else
            { 
                throw new Exception();
            }

            HashSet<string> links = new HashSet<string>(queryResults);

            return fixUrls(links);
        }

        // Parallelized to leave one core available for HTTP requests
        private List<string> filterByKeywords2(List<ChildPage> childPages)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<Thread> threads = new List<Thread>();
            List<List<KeyValuePair<string, int>>> segments = new List<List<KeyValuePair<string, int>>>();
             
            int segLen = (int)Math.Ceiling((double)childPages.Count / (Environment.ProcessorCount - 1));
            int mod = childPages.Count % (int)segLen;
            List<int[]> positions = new List<int[]>();

            for(int i = 0; i < childPages.Count; i += segLen)
            {
                Thread thread = null;
                if(i == childPages.Count - mod)
                {
                    int[] pos = new int[2] { i, i + mod };
                    positions.Add(pos);
                    thread = new Thread(() => segments.Add(new List<KeyValuePair<string, int>>(doFilterByKeywords(childPages, pos[0], pos[1]))));
                }
                else
                {
                    int[] pos = new int[2] { i, i + segLen };
                    positions.Add(pos);
                    thread = new Thread(() => segments.Add(new List<KeyValuePair<string, int>>(doFilterByKeywords(childPages, pos[0], pos[1]))));
                }
                thread.Start();
                threads.Add(thread);
            }

            foreach (Thread thread in threads)
                thread.Join();

            List<KeyValuePair<string, int>> results = new List<KeyValuePair<string, int>>();
            foreach(List<KeyValuePair<string,int>> segment in segments)
            {
                results.AddRange(segment);
            }

            var sortedList = from entry in results orderby entry.Value descending select entry;


            sw.Stop();
            Console.WriteLine("\nManual Threading: {0}ms", sw.ElapsedMilliseconds);

            return sortedList.Select(x => x.Key).ToList();
        }

        private List<KeyValuePair<string, int>> doFilterByKeywords(List<ChildPage> childPages, int startPos, int endPos)
        {
            List<KeyValuePair<string, int>> results = new List<KeyValuePair<string, int>>();

            for (int i = startPos; i < endPos; i++)
            {
                string innerText = childPages[i].htmlDoc.DocumentNode.InnerText.ToLower();
                int pageScore = 0;
                foreach(string keyword in keywords)
                {
                    if(innerText.Contains(keyword.ToLower()))
                    {
                        pageScore += Regex.Matches(innerText, keyword).Count;
                    }
                    else
                    {
                        continue;
                    }
                }
                results.Add(new KeyValuePair<string, int>(childPages[i].domain.AbsoluteUri, pageScore));
            }

            return results;
        }

        private List<string> filterByKeyWords(List<ChildPage> childPages)
        {
            List<KeyValuePair<string, int>> results = new List<KeyValuePair<string, int>>();

            foreach(ChildPage page in childPages)
            {
                string innerText = page.htmlDoc.DocumentNode.InnerText.ToLower();
                int pageScore = 0;
                foreach (string keyword in keywords)
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
                results.Add(new KeyValuePair<string, int>(page.domain.AbsoluteUri, pageScore));
            }

            var sortedList = from entry in results orderby entry.Value descending select entry;

            printRankedPages(sortedList);
           
            return sortedList.Select(x => x.Key).ToList();
        }

        private void printRankedPages(IOrderedEnumerable<KeyValuePair<string, int>> pages)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (KeyValuePair<string, int> pair in pages)
            {
                Console.WriteLine("[PageScore]: {0} \t [Page]: {1}", pair.Value, pair.Key);
            }
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private List<ChildPage> loadChildPages(List<string> results)
        {
            childPages = new List<ChildPage>();

            ManualResetEvent signal = new ManualResetEvent(false);
            Thread parallelWait = new Thread(() => waitForChildPages(signal, results.Count));
            parallelWait.Start();

            foreach (string url in results)
            {
                ChildPage page = new ChildPage(url, DateTime.Now, signal);
                WebCrawler.Enqueue(page);
                childPages.Add(page);
            }

            parallelWait.Join();

            return childPages;
        }

        private void waitForChildPages(ManualResetEvent signal, int count)
        {
            int numberOfTasks = count;

            while (numberOfTasks != 0)
            {
                signal.WaitOne();
                numberOfTasks--;
                signal.Reset();
            }
        }

        private HashSet<string> fixUrls(HashSet<string> urls)
        {
            urls.Remove(domain.AbsoluteUri);

            HashSet<string> fixedUrlSet = new HashSet<string>();
            foreach(string url in urls)
            {
                Uri fixedUri;
                if(Uri.TryCreate(domain, url, out fixedUri))
                {
                    fixedUrlSet.Add(fixedUri.AbsoluteUri);
                }
                else
                {
                    // do regular expression checking and string concatenation....
                    Match regx = Regex.Match(url, @"\.[a-z]{2,3}(\.[a-z]{2,3})?");
                    if (!regx.Success)
                    {
                        fixedUrlSet.Add(domain.Scheme + "://" + domain.Host + url);
                    }
                }
            }

            return fixedUrlSet;
        }

        private bool allowedByRobotsTXT()
        {
            // load robots.txt, parse, if Uri is disallowed then return false

            return true;
        }
    }
}
