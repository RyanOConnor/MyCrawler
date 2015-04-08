using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MongoDB.Bson;
using System.Net;
using MongoDB.Driver;
using System.Diagnostics;

namespace WebApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach(string ipAddress in args)
            {
                IPAddress ip = IPAddress.Parse(ipAddress);
                IPEndPoint ep = new IPEndPoint(ip, 11000);
                CrawlerManager.Instance.AllowCrawlerIP(ep);
            }

            Database.Instance.Start();

            Thread dataThread = new Thread(DataManager.Instance.Start);
            dataThread.Start();

            string ipAd = "192.168.1.132";
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(ipAd), 11000);
            CrawlerManager.Instance.AllowCrawlerIP(endpoint);
            Thread crawlerThread = new Thread(() => CrawlerManager.Instance.StartListening(IPAddress.Parse(ipAd)));
            crawlerThread.Start();

            UserManager.Instance.Start();

        }
    }
}
