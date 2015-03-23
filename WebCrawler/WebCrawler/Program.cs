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

            string ipAddress = "192.168.1.132";
            Thread MainThread = new Thread(() => WebCrawler.Instance.Start(ipAddress));
            MainThread.Start();

        }
    }
}
