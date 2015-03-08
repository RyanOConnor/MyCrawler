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
        private static BlockingCollection<HTMLPage> workQueue { get; set; }
        private static PriorityQueue<DateTime, HTMLPage> waitQueue { get; set; }
        private static SortedDictionary<string, DateTime> restrictedDomains { get; set; }
        private static ManualResetEvent loadWaitDone = new ManualResetEvent(false);
        private static ManualResetEvent loadDocDone = new ManualResetEvent(false);
        public const int DOMAIN_DELAY_PERIOD = 2000;
        private static object delaySyncRoot = new object();
        private static object domainSyncRoot = new object();

        static WebCrawler()
        {
            status = CrawlerStatus.ok;
            workQueue = new BlockingCollection<HTMLPage>();
            waitQueue = new PriorityQueue<DateTime, HTMLPage>();
            restrictedDomains = new SortedDictionary<string, DateTime>();
        }

        public static void start()
        {
            // Start crawler to accept incoming crawl requests from application
            Thread mainThread = new Thread(new ThreadStart(loadPages));
            mainThread.Start();
        }

        public static void loadPages()
        {
            Thread loadDocQueue = new Thread(loadFromDocumentQueue);
            Thread loadWaitTree = new Thread(loadFromDelayCollection);
            loadDocQueue.Start();
            loadWaitTree.Start();
        }

        public static void sendErrorMessage(CrawlerStatus errorMsg)
        {
            // Send application error information
        }

        public static void loadFromDelayCollection()
        {
            ManualResetEvent allowLooping = new ManualResetEvent(false);
            while (true)
            {
                if (waitQueue.Count == 0)
                {
                    loadWaitDone.WaitOne();
                }

                KeyValuePair<DateTime, HTMLPage> page = dequeueDelayQueue();
                int domainRestriction = getDomainRestriction(page.Value);
                if(domainRestriction > 0)
                {
                    //page.Value.setWaitTime(DOMAIN_DELAY_PERIOD);
                    enqueueDelayQueue(page.Value, domainRestriction);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(page.Value.sleepThenUpdate));
                    addRestrictedDomain(page.Value);
                    Console.WriteLine("WaitQueue: {0} \t Restricted: {1}", waitQueue.Count, restrictedDomains.Count);
                }

                loadWaitDone.Reset();
            }
        }

        public static void loadFromDocumentQueue()
        {
            while (true)
            {
                if(workQueue.Count == 0)
                {
                    loadDocDone.WaitOne();
                }

                HTMLPage page = dequeueWorkQueue();
                int domainRestriction = getDomainRestriction(page);
                if(domainRestriction > 0)
                {
                    //page.setWaitTime(DOMAIN_DELAY_PERIOD);
                    enqueueDelayQueue(page, domainRestriction);
                }
                else
                {
                    page.beginUpdate();
                    addRestrictedDomain(page);
                }

                loadDocDone.Reset();
            }
        }

        private static void addRestrictedDomain(HTMLPage page)
        {
            lock (domainSyncRoot)
            {
                if (restrictedDomains.ContainsKey(page.domain.Host))
                {
                    restrictedDomains[page.domain.Host] = DateTime.Now;
                }
                else
                {
                    restrictedDomains.Add(page.domain.Host, DateTime.Now);
                }
            }
        }

        private static int getDomainRestriction(HTMLPage page)
        {
            lock(domainSyncRoot)
            {
                if(restrictedDomains.ContainsKey(page.domain.Host))
                {
                    int timeSinceLastUpdate = (int)(DateTime.Now - restrictedDomains[page.domain.Host]).TotalMilliseconds;

                    return DOMAIN_DELAY_PERIOD - timeSinceLastUpdate;

                    /*var list = restrictedDomains.Where(x => (int)(DateTime.Now - x.Value).TotalMilliseconds > DOMAIN_DELAY_PERIOD)
                                        .Select(x => x.Key).ToList();

                    foreach(string domain in list)
                    {
                        restrictedDomains.Remove(domain);
                    }*/
                } 
                else
                {
                    return 0;
                }
            }
        }

        public static void enqueueWorkQueue(HTMLPage webPage)
        {
            workQueue.Add(webPage);
            loadDocDone.Set();
        }

        public static HTMLPage dequeueWorkQueue()
        {
            return workQueue.Take();
        }

        public static void enqueueDelayQueue(HTMLPage webPage, int delay)
        {
            lock (delaySyncRoot)
            {
                int priorityLevel = delay;
                Console.WriteLine("Priority: {0} - {1}", priorityLevel, webPage.domain.AbsoluteUri);
                waitQueue.Enqueue(DateTime.Now, webPage);
                loadWaitDone.Set();
            }
        }

        public static KeyValuePair<DateTime, HTMLPage> dequeueDelayQueue()
        {
            lock (delaySyncRoot)
            {
                return waitQueue.Dequeue();
            }
        }
    }
}
