using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Web.Script.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace WebApplication
{
    static class CrawlerManager
    {
        private static BlockingCollection<HTMLRecord> sendQueue { get; set; }
        private static List<CrawlerNode> workerNodes { get; set; }
        private static TcpListener listener { get; set; }

        static CrawlerManager()
        {
            sendQueue = new BlockingCollection<HTMLRecord>();
            workerNodes = new List<CrawlerNode>();
        }

        public static void startCrawlerServer()
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipLocal = new IPEndPoint(ip, 8888);
            listener = new TcpListener(ipLocal);
            listener.Start();
            waitForCrawler();
        }

        public static void waitForCrawler()
        {
            object obj = new object();
            listener.BeginAcceptTcpClient(new AsyncCallback(onCrawlerConnect), obj);
        }

        private static void onCrawlerConnect(IAsyncResult sync)
        {
            Console.WriteLine("Crawler connected...");

            try
            {
                TcpClient client = default(TcpClient);
                client = listener.EndAcceptTcpClient(sync);

                CrawlerNode node = new CrawlerNode(client);
                workerNodes.Add(node);
                node.startClient();
            }
            catch(Exception ex)
            {
                throw ex;
            }

            //waitForCrawler(); 
        }

        public static void distributeWorkAmongstCrawlers(HTMLRecord page)
        {
            CrawlerNode node = workerNodes.OrderByDescending(x => x.workQueue.Count).First();
            node.addWork(page);
        }

        public static void relayCrawlerResults(HTMLRecord page)
        {
            DataManager.updateEntry(page);
        }

        public static void getCrawlerStatus()
        {
            
        }

        public static void enqueue(HTMLRecord page)
        {
            sendQueue.Add(page);
        }
    }

    class CrawlerNode : HandleRequest
    {
        public string crawlerStatus { get; set; }
        public IPEndPoint crawlerIP { get; set; }
        public BlockingCollection<HTMLRecord> workQueue { get; set; }

        public CrawlerNode(TcpClient crawlerSocket)
            : base(crawlerSocket)
        {
            workQueue = new BlockingCollection<HTMLRecord>();
        }

        public void addWork(HTMLRecord record)
        {
            workQueue.Add(record);
        }

        public override string handleMessage(string message)
        {
            if(message == "next")
            {
                return sendWorkToCrawler();
            }
            else
            {
                CrawlerManager.relayCrawlerResults(deserializeJSON(message));
                return "sentToDatabase";
            }
        }

        private string sendWorkToCrawler()
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLRecord));
            MemoryStream stream = new MemoryStream();
            HTMLRecord record = workQueue.Take();       // Block here and wait for work

            ser.WriteObject(stream, record);
            return Encoding.ASCII.GetString(stream.ToArray());
        }

        private HTMLRecord deserializeJSON(string message)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLRecord));
            MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(message));
            return ser.ReadObject(stream) as HTMLRecord;
        }
    }
}
