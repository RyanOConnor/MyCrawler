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
        private static ManualResetEvent loadWaitDone = new ManualResetEvent(false);

        static WebCrawler()
        {
            status = CrawlerStatus.ok;
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

        /*public static void loadWebPages()
        {
            while(true)
            {
                if(workQueue.Count == 0)
                {
                    loadWaitDone.WaitOne();
                }

                HTMLPage page = workQueue.Dequeue();

                if (page.waitTime != 0)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(page.sleepThenUpdate));
                }
                else
                {
                    page.beginUpdate();
                }

                loadWaitDone.Reset();
            }
        }*/

        public static void enqueueWorkQueue(HTMLPage webPage)
        {
            DomainDictionary.Enqueue(webPage.domain.Host, webPage);
            loadWaitDone.Set();
        }
    }
}
