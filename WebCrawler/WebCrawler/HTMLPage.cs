using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Fizzler;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;

namespace WebCrawler
{
    [DataContract]
    class HTMLPage// : HTMLRecord
    {
        [DataMember]
        public string id { get; private set; }
        [DataMember]
        public string url { get; private set; }
        [DataMember]
        public DateTime timeStamp { get; private set; }
        [DataMember]
        public List<string> htmlHashes { get; private set; }
        [DataMember]
        public Uri domainURL { get; protected set; }
        public HtmlDocument HTML { get; protected set; }
        public int waitTime { get; set; }
        public int pageScore { get; protected set; }

        public HTMLPage(string id, string url, DateTime timeStamp, List<string> htmlHashes)
        {
            this.id = id;
            this.url = url;
            this.timeStamp = timeStamp;
            this.htmlHashes = htmlHashes;
            domainURL = new Uri(url);
        }

        [OnDeserialized]
        public void setUri(StreamingContext context)
        {
            domainURL = new Uri(url);
        }

        public HttpStatusCode update()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(domainURL.AbsoluteUri);
            request.Method = WebRequestMethods.Http.Get;
            request.Headers.Add("Accept-Encoding", "gzip,deflate");
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            HttpWebResponse web = null;

            try
            {
                web = (HttpWebResponse)request.GetResponse();
            }
            catch(Exception ex)
            {
                Console.WriteLine("ERROR: [{0}] returned '{1}'", domainURL.Host, web.StatusCode);
            }

            try
            {
                string decompressedHTML;
                using (Stream stream = getResponseStream(web, 10000))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        decompressedHTML = reader.ReadToEnd();
                    }
                }


                Console.WriteLine("Loading [{0}]...", domainURL.AbsoluteUri);


                //HTML = web.Load(domainURL.AbsoluteUri);
                if (web.StatusCode != HttpStatusCode.OK)
                {
                    // do something?
                    // throw new Exception();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Web Exception Thrown");
            }

            sw.Stop();
            Console.WriteLine("\n\t\t Finished [{0}] in {1}ms.", domainURL.AbsoluteUri, sw.ElapsedMilliseconds);
            return web.StatusCode;
        }

        private static Stream getResponseStream(HttpWebResponse response, int timeOut)
        {
            Stream stream;
            if(response.ContentEncoding.ToUpperInvariant() == "GZIP")
            {
                stream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress);
            }
            else if(response.ContentEncoding.ToUpperInvariant() == "DEFLATE" || response.ContentEncoding.ToUpperInvariant() == "ZLIB")
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

        public void scorePage()
        {
            // Only called if keywords have been set
            // Increment pagescore for each keyword found
        }

        public void fixUrl()
        {

        }

        public HtmlNode filterByTags()
        {
            // (at application or client level, do not allow "noindex" or "nofollow" tags)
            // TODO: ... 
            return HTML.DocumentNode;
        }

        public HtmlNode filterByKeywords()
        {
            // TODO: ...
            return HTML.DocumentNode;
        }

        public bool allowedByRobotsTXT()
        {
            // load robots.txt, parse, if Uri is disallowed then return false
            HtmlWeb web = new HtmlWeb();
            web.Load(domainURL.Host + "/robots.txt");


            return true;
        }
    }

    /*class HTMLPage
    {
        public string id { get; protected set; }           // Batch ID from Web Application to determine where this resulting web page is sent
        public Uri DomainURL { get; protected set; }
        public HtmlDocument HTML { get; protected set; }
        public DateTime lastGenerated { get; protected set; }
        public int waitTime { get; private set; }
        public int pageScore { get; protected set; }

        public HTMLPage(string id, Uri domain, DateTime lastGenerated)
        {
            this.id = id;
            this.DomainURL = domain;
            this.lastGenerated = lastGenerated;
        }

        public HttpStatusCode update()
        {
            // Load web page
            HtmlWeb web = new HtmlWeb();
            web.UserAgent = "SET USERAGENT";

            HTML = web.Load(DomainURL.AbsoluteUri);
            if(web.StatusCode != HttpStatusCode.OK)
            {
                // do something?
            }
            return web.StatusCode;
        }

        public void scorePage()
        {
            // Only called if keywords have been set
            // Increment pagescore for each keyword found
        }

        public void fixUrl()
        {

        }
    }

    // Professor says inheritance can be slow, maybe re-implement?
    class InitialPage : HTMLPage
    {
        public List<string> tags { get; private set; } 
        public List<string> keywords { get; private set; }
        public List<string> contentHashes { get; protected set; }       // For checking against update generated here to determine change in content

        public InitialPage(string id, Uri domain, DateTime lastGenerated, List<string> tags, 
                           List<string> keywords, List<string> contentHashes)
            :base(id, domain, lastGenerated)
        {
            this.tags = tags;
            this.keywords = keywords;
            this.contentHashes = contentHashes;
        }

        public HtmlNode filterByTags()
        {
            // (at application or client level, do not allow "noindex" or "nofollow" tags)
            // TODO: ... 
            return HTML.DocumentNode;
        }

        public HtmlNode filterByKeywords()
        {
            // TODO: ...
            return HTML.DocumentNode;
        }

        public bool allowedByRobotsTXT()
        {
            // load robots.txt, parse, if Uri is disallowed then return false
            return true;
        }
    }*/
}
