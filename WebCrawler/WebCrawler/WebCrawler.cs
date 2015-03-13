using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using System.Collections.Concurrent;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Diagnostics;
using HtmlAgilityPack;

namespace WebCrawler
{
    public enum CrawlerStatus { ok };

    static class WebCrawler
    {
        public static CrawlerStatus status { get; private set; }
        private static Dictionary<string, Domain> domainDictionary { get; set; }

        static WebCrawler()
        {
            status = CrawlerStatus.ok;
            domainDictionary = new Dictionary<string, Domain>();
        }

        public static void start()
        {
            // Start crawler to accept incoming crawl requests from application
            //Thread mainThread = new Thread(new ThreadStart(loadWebPages));
            //mainThread.Start();
        }

        public static void sendErrorMessage(CrawlerStatus errorMsg)
        {
            // Send application error information
        }

        public static void Enqueue(HTMLPage page)
        {
            lock (domainDictionary)
            {
                string key = page.domain.Host;
                if (domainDictionary.ContainsKey(key))
                {
                    domainDictionary[key].Enqueue(page);
                }
                else
                {
                    domainDictionary[key] = new Domain(key);
                    domainDictionary[key].Enqueue(page);
                    domainDictionary[key].initTimer();
                }
            }
        }

        public static void removeDomain(string domainKey)
        {
            lock (domainDictionary)
            {
                domainDictionary.Remove(domainKey);
            }
        }
    }
}
