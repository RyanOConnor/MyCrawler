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
        private static BlockingCollection<HTMLPage> documentQueue { get; set; }
        private static PriorityQueue<int, HTMLPage> waitQueue { get; set; }
        private static ManualResetEvent loadWaitDone = new ManualResetEvent(false);
        private static ManualResetEvent loadDocDone = new ManualResetEvent(false);
        public static int cores = Environment.ProcessorCount;
        private static object syncRoot = new object();

        static WebCrawler()
        {
            status = CrawlerStatus.ok;
            documentQueue = new BlockingCollection<HTMLPage>();
            waitQueue = new PriorityQueue<int, HTMLPage>();
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

        public static void loadPages()
        {
            Thread loadDocQueue = new Thread(loadDocumentQueue);
            Thread loadWaitTree = new Thread(loadFromDelayCollection);
            loadDocQueue.Start();
            loadWaitTree.Start();
        }

        public static void sendErrorMessage(CrawlerStatus errorMsg)
        {
            // Send application error information
        }

        public static void enqueue(HTMLPage webPage)
        {
            //Console.WriteLine("enqueueing [{0}]...", webPage.domainURL.AbsoluteUri);
            documentQueue.Add(webPage);
            loadDocDone.Set();
        }

        public static void enqueueDelayCollection(HTMLPage webPage)
        {
            lock (syncRoot)
            {
                waitQueue.Enqueue(webPage.waitTime, webPage);
                loadWaitDone.Set();
            }
        }

        public static KeyValuePair<int, HTMLPage> dequeueDelayCollection()
        {
            lock(syncRoot)
            {
                return waitQueue.Dequeue();
            }
        }

        public static void loadFromDelayCollection()
        {
            int loopCounter = 0;
            ManualResetEvent allowLooping = new ManualResetEvent(false);

            while (true)
            {
                if (waitQueue.Count == 0)
                {
                    loadWaitDone.WaitOne();
                }

                KeyValuePair<int, HTMLPage> page = dequeueDelayCollection();

                if (page.Key > page.Value.millisecondsSinceWait())
                {
                    Thread.Sleep(page.Key - page.Value.millisecondsSinceWait());
                }

                page.Value.update();

                loadWaitDone.Reset();

                Console.WriteLine("loadFromWaitTree(): " + loopCounter);
                loopCounter++;
            }
        }

        public static void loadDocumentQueue()
        {
            int loopCounter = 0;
            while(true)
            {
                loadDocDone.Reset();
                foreach(HTMLPage page in documentQueue)
                {
                    documentQueue.Take().update();
                    loopCounter++;
                }
                Console.WriteLine("loadDocumentQueue(): " + loopCounter);
                loadDocDone.WaitOne();
            }
        }

        public static void signalDelayCollection()
        {
            loadWaitDone.Set();
        }
    }

    /*public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
    {
        #region IComparer<TKey> Members
        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);

            if (result == 0)
                return 1;
            else
                return result;
        }
        #endregion
    }*/

}
