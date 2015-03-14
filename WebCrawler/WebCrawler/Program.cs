using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Threading;
using System.Net;

namespace WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 10000;
            ServicePointManager.Expect100Continue = false;

            //bool firstInstance;
            //Mutex mutex = new Mutex(false, @"C:\8086", out firstInstance);

            //if(!firstInstance)
            //{
                // another instance of this application is running
            //}
            //else
            //{
                Thread MainThread = new Thread(WebCrawler.Instance.Start);
                MainThread.Start();
            //}

            //GC.KeepAlive(mutex);
        }
    }
}
