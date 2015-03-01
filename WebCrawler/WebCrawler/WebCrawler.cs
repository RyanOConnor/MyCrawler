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

namespace WebCrawler
{
    public enum CrawlerStatus { ok };

    static class WebCrawler
    {
        public static CrawlerStatus status { get; private set; }
        private static BlockingCollection<HTMLPage> documentQueue { get; set; }
        private static BlockingCollection<HTMLPage> waitQueue { get; set; }
        public static int cores = Environment.ProcessorCount;

        static WebCrawler()
        {
            status = CrawlerStatus.ok;
            documentQueue = new BlockingCollection<HTMLPage>();
            waitQueue = new BlockingCollection<HTMLPage>();
        }

        public static void start()
        {
            // Start crawler to accept incoming crawl requests from application
            Thread mainThread = new Thread(new ThreadStart(loadPages));
            mainThread.Start();
        }

        public static void crawl()
        {
            // handles all threads, loadPages and others included

        }

        public static void sendErrorMessage(CrawlerStatus errorMsg)
        {
            // Send application error information
        }

        public static void enqueue(HTMLPage webPage)
        {
            Console.WriteLine("enqueueing [{0}]...", webPage.domainURL.AbsoluteUri);
            documentQueue.Add(webPage);
        }

        public static void enqueueWait(HTMLPage webPage)
        {
            waitQueue.Add(webPage);
        }

        public static void loadPages()
        {
            // Multi-threaded download
            // Pull page off of waitQueue first if it's ready
            // If documentQueue.Dequeue().update() returns bad status code, send page to waitQueue
            //      if it returns a really bad status code then remove it and/or notify web application

            int i;
            //ThreadPool.SetMaxThreads(cores, cores);
            while(true)
            {
                for (i = 0; i < waitQueue.Count; i++)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(loadPageFromWaitQueue));
                }
                for (i = 0; i < documentQueue.Count; i++)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(loadPageFromDocumentQueue));
                }
            }
        }

        public static void loadPageFromWaitQueue(object stateInfo)
        {
            HTMLPage page = waitQueue.Take();
            Thread.Sleep(page.waitTime);
            HttpStatusCode code = page.update();
            if(code != HttpStatusCode.OK)
            {
                page.waitTime = 10000;      // set time to amount determined from status code, or arbitrary random
                waitQueue.Add(page);
                Console.WriteLine("\n[{0}] has been wait queued...", page.domainURL.AbsoluteUri);
            }
        }

        public static void loadPageFromDocumentQueue(object stateInfo)
        {
            HTMLPage page = documentQueue.Take();
            HttpStatusCode code = page.update();
            if(code != HttpStatusCode.OK)
            {
                page.waitTime = 10000;      // set time to amount determined from status code, or arbitrary random
                waitQueue.Add(page);
                Console.WriteLine("\n[{0}] has been wait queued...", page.domainURL.AbsoluteUri);
            }
        }
    }
}
