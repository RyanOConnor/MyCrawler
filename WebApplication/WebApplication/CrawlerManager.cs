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
    public static class CrawlerManager
    {
        private static BlockingCollection<HTMLRecord> sendQueue { get; set; }
        private static BlockingCollection<HTMLRecord> workQueue { get; set; }
        private static List<CrawlerNode> workerNodes { get; set; }
        private static Socket crawlerSocket { get; set; }

        static CrawlerManager()
        {
            sendQueue = new BlockingCollection<HTMLRecord>();
            workQueue = new BlockingCollection<HTMLRecord>();
            workerNodes = new List<CrawlerNode>();
        }

        public static string sendWorkToCrawler()
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLRecord));
            MemoryStream stream = new MemoryStream();
            HTMLRecord record = workQueue.Take();       // Block here and wait for work

            ser.WriteObject(stream, record);
            return Encoding.ASCII.GetString(stream.ToArray());
        }

        public static HTMLRecord deserializeJSON(string message)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLRecord));
            MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(message));
            return ser.ReadObject(stream) as HTMLRecord;
        }

        public static void distributeWorkAmongstCrawlers(HTMLRecord record)
        {
            workQueue.Add(record);
        }

        /*public static void distributeWorkAmongstCrawlers(HTMLRecord page)
        {
            CrawlerNode node = workerNodes.OrderByDescending(x => x.workQueue.Count).First();
            node.addWork(page);
        }*/

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

    class CrawlerNode : SocketHandle
    {
        public string crawlerStatus { get; set; }
        public BlockingCollection<HTMLRecord> workQueue { get; set; }

        public CrawlerNode(Socket crawlerSocket)
            :base(crawlerSocket, ConsoleColor.Black, 0)
        {
            workQueue = new BlockingCollection<HTMLRecord>();
        }

        public void addWork(HTMLRecord record)
        {
            workQueue.Add(record);
        }
    }
}

