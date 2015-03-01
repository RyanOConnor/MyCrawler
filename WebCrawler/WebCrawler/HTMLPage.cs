using System;
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
using Fizzler;
using CsQuery;

namespace WebCrawler
{
    [DataContract]
    public class HTMLPage
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

        public HttpStatusCode updateHTMLAgilityPack()
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();
            HtmlWeb web = new HtmlWeb();
            web.Load(domainURL.AbsoluteUri);
            
            sw.Stop();

            return web.StatusCode;
        }

        public HttpStatusCode update()
        {
            int a = ServicePointManager.DefaultConnectionLimit;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(domainURL.AbsoluteUri);
            request.Method = WebRequestMethods.Http.Get;
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            request.Proxy = null;//WebRequest.DefaultWebProxy;

            HttpWebResponse response = getResponse(request);
            string html = decompressHtml(response);
   
            sw.Stop();
            Console.WriteLine("\n\t\t Finished [{0}] in {1}ms.", domainURL.AbsoluteUri, sw.ElapsedMilliseconds);
            return response.StatusCode;
        }

        private HttpWebResponse getResponse(HttpWebRequest request)
        {
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception ex)
            {
                // NOTIFY APPLICATION FROM HERE
                Console.WriteLine("ERROR: [{0}] returned '{1}'", domainURL.Host, response.StatusCode);
                Console.WriteLine(ex.ToString());
            }

            return response;
        }

        private string decompressHtml(HttpWebResponse response)
        {
            string decompressedHTML = string.Empty;
            try
            {
                using (Stream stream = getResponseStream(response, 10000))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        decompressedHTML = reader.ReadToEnd();
                    }
                }
                Console.WriteLine("Loading [{0}]...", domainURL.AbsoluteUri);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return decompressedHTML;
        }

        private static Stream getResponseStream(HttpWebResponse response, int timeOut)
        {
            Stream stream;
            if (response.ContentEncoding.ToLower() == "gzip")
            {
                stream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress);
                Console.WriteLine("DOWNLOADING USING GZIPSTREAM");
            }
            else if (response.ContentEncoding.ToLower() == "deflate" || response.ContentEncoding.ToUpperInvariant() == "zlib")
            {
                stream = new DeflateStream(response.GetResponseStream(), CompressionMode.Decompress);
                Console.WriteLine("DOWNLOADING USING DEFLATESTREAM");
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
            // TODO: ... implement utilizing CsQuery library
            return HTML.DocumentNode;
        }

        public HtmlNode filterByKeywords()
        {
            // TODO: ... implement utilizing CsQuery library
            return HTML.DocumentNode;
        }

        public bool allowedByRobotsTXT()
        {
            // load robots.txt, parse, if Uri is disallowed then return false

            return true;
        }
    }
}
