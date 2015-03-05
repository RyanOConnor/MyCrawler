using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using CsQuery;
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
                    string htmlStr = decompressHtml(response);

                    //Console.WriteLine("HTML from {0}: \n{1}", domain.AbsoluteUri, htmlStr);
                    Console.WriteLine("\nLoaded {0}...", domain.AbsoluteUri);

                    if (htmlStr == string.Empty)
                        Console.WriteLine();

                    htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(htmlStr);

                    //Console.WriteLine("\n\t\t Updated [{0}] in {1}ms, size: {2}bytes", domainURL.AbsoluteUri, sw.ElapsedMilliseconds, contentLength);
                }
                else
                {
                    this.setWaitTime(10000);          // if bad status code, place "this" on waitQueue
                    WebCrawler.enqueueDelayCollection(this);   //      maybe just skip this portion to not hold up parent thread
                }
            }
            catch (Exception ex)
            {
                // NOTIFY APPLICATION FROM HERE THAT PAGE COULD NOT BE LOADED
                Console.WriteLine(ex.ToString());
            }

            manualEvent.Set();
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
        protected HtmlDocument htmlDoc { get; set; }
        private List<string> htmlResults { get; set; }
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

        public int millisecondsSinceWait()
        {
            return (int)((DateTime.Now - this.timeStamp).TotalMilliseconds);
        }

        public void setWaitTime(int milliseconds)
        {
            this.waitTime = milliseconds;
            this.timeStamp = DateTime.Now;
        }

        public void update()
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
                        List<ChildPage> linkedPages = loadChildPages(results.ToList());

                        List<string> rankedResults = filterByKeywords(linkedPages);

                        Console.WriteLine();
                    }
                    //Console.WriteLine("\n\t\t Updated [{0}] in {1}ms, size: {2}bytes", domainURL.AbsoluteUri, sw.ElapsedMilliseconds, contentLength);
                }
                else
                {
                    this.waitTime = 10000;          // if bad status code, place "this" on waitQueue
                    WebCrawler.enqueueDelayCollection(this);
                }
            }
            catch (Exception ex)
            {
                // NOTIFY APPLICATION FROM HERE THAT PAGE COULD NOT BE LOADED
                Console.WriteLine(ex.ToString());
                throw ex;
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

        private List<ChildPage> loadChildPages(List<string> results)
        {
            int numberOfTasks = results.Count;
            int waitMultiplier = 1;
            childPages = new List<ChildPage>();

            ManualResetEvent signal = new ManualResetEvent(false);

            foreach(string url in results)
            {
                ChildPage childPage = new ChildPage(url, DateTime.Now, signal);

                if(childPage.domain.Host == this.domain.Host)
                {
                    childPage.setWaitTime(2000 * waitMultiplier);
                    childPage.timeStamp = DateTime.Now;
                    waitMultiplier++;
                }

                childPages.Add(childPage);
                WebCrawler.enqueueDelayCollection(childPage);
            }

            while(numberOfTasks != 0)
            {
                signal.WaitOne();
                numberOfTasks--;
                signal.Reset();
            }

            return childPages;
        }

        private void indexHtmlCsQuery(string html)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var dom = CQ.CreateDocument(html);

            List<string> results = cqFilterByTags(dom);
            sw.Stop();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n\t CsQuery found {0} links from {1} in {2}ms.", results.Count, domain.AbsoluteUri, sw.ElapsedMilliseconds);
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

            HashSet<string> links = null;
            if(htmlTags[htmlTags.Count-1] == "href")
            {
                var queryResult = htmlDoc.DocumentNode.QuerySelectorAll(query).Select(x => x.Attributes["href"].Value);
                links = new HashSet<string>(queryResult);
            }
            else if(htmlTags[htmlTags.Count-1] == "text")
            {
                var queryResult = htmlDoc.DocumentNode.QuerySelectorAll(query).Select(x => x.InnerText).ToList();
                links = new HashSet<string>(queryResult);
            }

            return fixUrls(links);
        }

        private List<string> cqFilterByTags(CQ html)
        {
            string query = htmlTags[0];
            for (int i = 1; i < htmlTags.Count - 1; i++)
            {
                if(htmlTags[i].ElementAt(0) == '.' || htmlTags[i].ElementAt(0) == '#')
                {
                    query += htmlTags[i];
                }
                else
                {
                    query += " " + htmlTags[i]; 
                }
            }

            List<string> links = null;
            if(htmlTags[htmlTags.Count-1] == "href")
            {
                links = html[query].Select(x => x.GetAttribute("href")).ToList();
            }
            else if(htmlTags[htmlTags.Count-1] == "text")
            {
                links = html[query].Select(x => x.InnerText).ToList();
            }

            return links;
        }

        // Parallelize this method to divide the list into equal parts
        private List<string> filterByKeywords(List<ChildPage> childPages)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<KeyValuePair<string, int>> results = new List<KeyValuePair<string, int>>();

            foreach (ChildPage page in childPages)
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

            Console.ForegroundColor = ConsoleColor.Cyan;
            foreach (KeyValuePair<string, int> pair in sortedList)
            {
                Console.WriteLine("[PageScore]: {0} \t [Page]: {1}", pair.Value, pair.Key);
            }
            Console.ForegroundColor = ConsoleColor.Gray;

            sw.Stop();
            Console.WriteLine("Normal foreach: {0}ms", sw.ElapsedMilliseconds);

            return sortedList.Select(x => x.Key).ToList();
        }

        private List<string> filterByKeyWords2(List<ChildPage> childPages)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<KeyValuePair<string, int>> results = new List<KeyValuePair<string, int>>();

            Parallel.ForEach(childPages, page =>
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
            });

            var sortedList = from entry in results orderby entry.Value descending select entry;

            Console.ForegroundColor = ConsoleColor.Cyan;
            foreach (KeyValuePair<string, int> pair in sortedList)
            {
                Console.WriteLine("[PageScore]: {0} \t [Page]: {1}", pair.Value, pair.Key);
            }
            Console.ForegroundColor = ConsoleColor.Gray;

            sw.Stop();
            Console.WriteLine("Parallel.ForEach: {0}ms", sw.ElapsedMilliseconds);

            return sortedList.Select(x => x.Key).ToList();
        }

        private HashSet<string> fixUrls(HashSet<string> urls)
        {
            HashSet<string> fixedUrls = new HashSet<string>(urls
                                            .Where(x => x.StartsWith("/"))
                                            .Select(x => domain.Scheme + "://" + domain.Host + x));
            /*foreach(string url in urls)
            {
                if(url.StartsWith("/"))
                {
                    url = domain.Scheme + "://" + domain.Host + url;
                }
            }
            for(int i = 0; i < urls.Count; i++)
            {
                if(urls[i].StartsWith("/"))
                {
                    urls[i] = domain.Scheme + "://" + domain.Host + urls[i];
                }
            }*/

            return fixedUrls;
        }

        private List<string> removeDuplicates(List<string> urls)
        {
            return new List<string>();
        }

        private bool allowedByRobotsTXT()
        {
            // load robots.txt, parse, if Uri is disallowed then return false

            return true;
        }


    }
}
