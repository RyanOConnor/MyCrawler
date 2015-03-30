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
        private BlockingCollection<HtmlRecord> sendQueue { get; set; }
        private Dictionary<IPEndPoint, CrawlerNode> crawlerNodes { get; set; }
        private HashSet<IPEndPoint> nodeIPAddresses { get; set; }
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
            sendQueue = new BlockingCollection<HtmlRecord>();
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

                if (nodeIPAddresses.Contains(handler.LocalEndPoint))
                {
                    CrawlerNode node = new CrawlerNode(handler);
                    crawlerNodes.Add((IPEndPoint)node.receiveSocket.socket.LocalEndPoint, node);
                    node.Start();
                }
                else
                {
                    throw new Exception("Invalid IP attempting to connect");
                }
            }
            catch(SocketException ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public void OnMessageReceived(object sender, MessageEventArgs args)
        {
            try
            {
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void DistributeWork(HtmlRecord page)
        {
            while (crawlerNodes.Count == 0) ;
            CrawlerNode node = crawlerNodes.OrderByDescending(x => x.Value.workQueue.Count).First().Value;
            node.AddWork(page);
        }

        public void GetCrawlerStatus(CrawlerNode node)
        {
            
        }

        public void Enqueue(HtmlRecord page)
        {
            sendQueue.Add(page);
        }
    }

    public class CrawlerNode : SocketServer
    {
        public string crawlerStatus { get; set; }
        public BlockingCollection<HtmlRecord> workQueue { get; set; }
        public ManualResetEvent allowSend = new ManualResetEvent(false);
        public ManualResetEvent waitForWork = new ManualResetEvent(false);
        public AutoResetEvent allowDBUpdate = new AutoResetEvent(false);
        public event EventHandler<HtmlRecord> UpdateReceived;
        private SocketClient crawlerClient { get; set; }

        public CrawlerNode(Socket socket)
        {
            this.receiveSocket = new SocketHandle(socket);
            workQueue = new BlockingCollection<HtmlRecord>();
            this.MessageReceived += new EventHandler<MessageEventArgs>(OnMessageReceived);
            DataManager.Instance.AddCrawlerEvent(this);
            crawlerClient = new SocketClient();
            crawlerClient.ClientConnected += new EventHandler(OnClientConnected);
            crawlerClient.MessageSubmitted += new EventHandler(OnMessageSubmitted);

            IPEndPoint localEndPoint = socket.LocalEndPoint as IPEndPoint;
            crawlerClient.StartClient(localEndPoint.Address);
        }

        public void Start()
        {
            this.Receive(null);
        }

        public void OnClientConnected(object sender, EventArgs args)
        {
            crawlerClient.Send(Encoding.UTF8.GetBytes("ready"));
        }

        public void OnMessageReceived(object sender, MessageEventArgs args)
        {
            try
            {
                if (Encoding.UTF8.GetString(args.Message) == "ready")
                {
                    ThreadPool.QueueUserWorkItem(Send);
                }
                else
                {
                    HtmlRecord record = BSON.Deserialize<HtmlRecord>(args.Message);

                    EventHandler<HtmlRecord> updateReceived = UpdateReceived;
                    if (updateReceived != null)
                    {
                        updateReceived.BeginInvoke(null, record, OnDatabaseUpdated, null);
                    }

                    Console.WriteLine("\n\tRecieved Data from: \n\t" + record.URL);

                    crawlerClient.Send(Encoding.UTF8.GetBytes("ready"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void OnDatabaseUpdated(IAsyncResult result)
        {
            var async = (System.Runtime.Remoting.Messaging.AsyncResult)result;
            var invokedMethod = (EventHandler<HtmlRecord>)async.AsyncDelegate;
            invokedMethod.EndInvoke(result);
        }

        public void Send(object obj)
        {
            if (workQueue.Count == 0)
                allowSend.WaitOne();

            if (workQueue.Count != 0)
            {
                HtmlRecord record = workQueue.Take();
                byte[] bson = BSON.Serialize<HtmlRecord>(record);
                crawlerClient.Send(bson);
                Console.WriteLine("\nSent Data from: \n" + record.URL);
            }
            allowSend.Reset();
        }

        public void OnMessageSubmitted(object sender, EventArgs args)
        {

        }

        public void AddWork(HtmlRecord record)
        {
            workQueue.Add(record);
            allowSend.Set();
        }
    }
}

