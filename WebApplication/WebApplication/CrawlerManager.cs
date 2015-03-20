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
    public enum CrawlerStatus { STARTING, WAITING, SENDING_DATA, SHUTTING_DOWN };
    public class CrawlerManager : SocketServer
    {
        private BlockingCollection<HTMLRecord> sendQueue { get; set; }
        private Dictionary<IPEndPoint, CrawlerNode> crawlerNodes { get; set; }
        private HashSet<IPEndPoint> nodeIPAddresses { get; set; }
        public static event EventHandler<HTMLRecord> UpdateReceived;
        private static CrawlerManager _instance;
        public static CrawlerManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CrawlerManager();
                return _instance;
            }
        }

        public CrawlerManager()
        {
            sendQueue = new BlockingCollection<HTMLRecord>();
            crawlerNodes = new Dictionary<IPEndPoint, CrawlerNode>();
            nodeIPAddresses = new HashSet<IPEndPoint>();
            this.MessageReceived += new EventHandler<MessageEventArgs>(OnMessageReceived);
        }

        public void AllowCrawlerIP(IPEndPoint endPoint)
        {
            lock(nodeIPAddresses)
            {
                if (!nodeIPAddresses.Contains(endPoint))
                    nodeIPAddresses.Add(endPoint);
            }
        }

        protected override void ConnectCallBack(IAsyncResult result)
        {
            listenerSignal.Set();

            try
            {
                Socket listener = (Socket)result.AsyncState;
                Socket handler = listener.EndAccept(result);
                CrawlerNode node;

                if (nodeIPAddresses.Contains(handler.LocalEndPoint))
                {
                    node = new CrawlerNode(handler);
                    crawlerNodes.Add((IPEndPoint)node.socket.LocalEndPoint, node);
                    node.Start();
                    node.socket.BeginReceive(node.buffer, 0, CrawlerNode.BUFFER_SIZE, 0,
                                                new AsyncCallback(ReceiveCallBack), node);
                }
                else
                {
                    throw new Exception("Invalid IP attempting to connect");
                }
            }
            catch(SocketException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void OnMessageReceived(object sender, MessageEventArgs args)
        {
            try
            {
                CrawlerNode node = (CrawlerNode)sender;

                if (args.Message == "ready")
                {
                    node.AllowSend();
                    CrawlerManager.Instance.Recieve(node);
                }
                else
                {
                    HTMLRecord record = (HTMLRecord)DeserializeJSON(args.Message, typeof(HTMLRecord));

                    EventHandler<HTMLRecord> updateReceived = UpdateReceived;
                    if (updateReceived != null)
                    {
                        updateReceived(null, record);
                    }

                    Console.WriteLine("Recieved Data from: \n" + record.URL);
                    node.Reset();
                    Instance.Recieve(node);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void DistributeWork(HTMLRecord page)
        {
            while (crawlerNodes.Count == 0) ;
            CrawlerNode node = crawlerNodes.OrderByDescending(x => x.Value.workQueue.Count).First().Value;
            node.AddWork(page);
        }


        public void getCrawlerStatus()
        {
            
        }

        public void Enqueue(HTMLRecord page)
        {
            sendQueue.Add(page);
        }
    }

    class CrawlerNode : SocketHandle
    {
        public string crawlerStatus { get; set; }
        public ConcurrentQueue<HTMLRecord> workQueue { get; set; }
        public ManualResetEvent waitForJob = new ManualResetEvent(false);

        public CrawlerNode(Socket socket)
            :base(socket, ConsoleColor.Cyan)
        {
            workQueue = new ConcurrentQueue<HTMLRecord>();
        }

        public void Start()
        {
            Thread sendThread = new Thread(Send);
            sendThread.Start();
        }

        public void AddWork(HTMLRecord record)
        {
            workQueue.Enqueue(record);
            waitForJob.Set();
        }

        public void AllowSend()
        {
            waitForJob.Set();
        }

        public void Send()
        {
            while (true)
            {
                waitForJob.WaitOne();

                HTMLRecord record = null;
                if (workQueue.TryDequeue(out record))
                {
                    string message = CrawlerManager.Instance.SerializeToJSON(record, typeof(HTMLRecord));
                    CrawlerManager.Instance.Send(this, message);
                }

                waitForJob.Reset();
            }
        }

        /*public void Send()
        {
            while (true)
            {
                if (workQueue.Count == 0)
                    waitForJob.WaitOne();

                HTMLRecord record = workQueue.Take();
                string message = CrawlerManager.Instance.SerializeToJSON(record, typeof(HTMLRecord));
                CrawlerManager.Instance.Send(this, message + "<EOF>");

                waitForJob.Reset();
            }
        }*/
    }
}

