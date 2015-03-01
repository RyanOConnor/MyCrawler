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
        private static NetworkStream stream { get; set; }

        static CrawlerManager()
        {
            sendQueue = new BlockingCollection<HTMLRecord>();
            workQueue = new BlockingCollection<HTMLRecord>();
            workerNodes = new List<CrawlerNode>();
        }

        private static void receiveCallBack(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            int received = socket.EndReceive(result);
            byte[] dataBuffer = new byte[received];
            //Array.Copy(buffer, dataBuffer, received);

            string message = Encoding.ASCII.GetString(dataBuffer);
            Console.WriteLine("Message received: " + message);

            string response = string.Empty;

            if(message == "next")
            {
                response = sendWorkToCrawler();
            }
            else if(message != string.Empty)
            {
                relayCrawlerResults(deserializeJSON(message));
            }

            byte[] data = Encoding.ASCII.GetBytes(response);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(sendCallBack), socket);
            Console.WriteLine("Message sent: " + response);
            //crawlerSocket.BeginAccept(new AsyncCallback(crawlerCallBack), null);
        }

        private static void sendCallBack(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            socket.EndSend(result);
        }

        private static string sendWorkToCrawler()
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLRecord));
            MemoryStream stream = new MemoryStream();
            HTMLRecord record = workQueue.Take();       // Block here and wait for work

            ser.WriteObject(stream, record);
            return Encoding.ASCII.GetString(stream.ToArray());
        }

        private static HTMLRecord deserializeJSON(string message)
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
        public TcpClient client { get; set; }
        public NetworkStream stream { get; set; }
        //public BlockingCollection<HTMLRecord> workQueue { get; set; }

        public CrawlerNode(Socket crawlerSocket)
            :base(crawlerSocket, ConsoleColor.Black, 0)
        {
            stream = new NetworkStream(crawlerSocket);
            //client = new TcpClient(crawlerSocket);
            //workQueue = new BlockingCollection<HTMLRecord>();
        }
        
        public void startNode()
        {
            stream = client.GetStream();
        }

        /*public void addWork(HTMLRecord record)
        {
            workQueue.Add(record);
        }*/

        /*public string handleMessage(string message)
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
        }*/


        }
}

