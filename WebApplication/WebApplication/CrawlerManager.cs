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

        public void DistributeWork(HTMLRecord page)
        {
            while (crawlerNodes.Count == 0) ;
            CrawlerNode node = crawlerNodes.OrderByDescending(x => x.Value.workQueue.Count).First().Value;
            node.AddWork(page);
        }

        public void GetCrawlerStatus(CrawlerNode node)
        {
            
        }

        public void Enqueue(HTMLRecord page)
        {
            sendQueue.Add(page);
        }
    }

    public class CrawlerNode : SocketServer
    {
        public string crawlerStatus { get; set; }
        public ConcurrentQueue<HTMLRecord> workQueue { get; set; }
        public ManualResetEvent allowSend = new ManualResetEvent(false);
        public event EventHandler<HTMLRecord> UpdateReceived;
        private SocketClient crawlerClient { get; set; }

        public CrawlerNode(Socket socket)
        {
            this.receiveSocket = new SocketHandle(socket);
            workQueue = new ConcurrentQueue<HTMLRecord>();
            this.MessageReceived += new EventHandler<MessageEventArgs>(OnMessageReceived);
            DataManager.Instance.AddCrawlerEvent(this);
            crawlerClient = new SocketClient();
            crawlerClient.ClientConnected += new EventHandler(OnClientConnected);
            crawlerClient.MessageSubmitted += new EventHandler(OnMessageSubmitted);

            IPEndPoint localEndPoint = socket.LocalEndPoint as IPEndPoint;
            crawlerClient.StartClient(localEndPoint.Address.ToString());
        }

        public void Start()
        {
            this.Receive();
            Thread sendThread = new Thread(this.SendWork);
            sendThread.Start();
        }

        public void OnClientConnected(object sender, EventArgs args)
        {

        }

        public void OnMessageReceived(object sender, MessageEventArgs args)
        {
            try
            {
                if (args.Message == "ready")
                {
                    allowSend.Set();
                }
                else
                {
                    HTMLRecord record = (HTMLRecord)DeserializeJSON(args.Message, typeof(HTMLRecord));

                    EventHandler<HTMLRecord> updateReceived = UpdateReceived;
                    if (updateReceived != null)
                    {
                        updateReceived(null, record);
                    }

                    Console.WriteLine("\n\tRecieved Data from: \n\t" + record.URL);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void OnMessageSubmitted(object sender, EventArgs args)
        {

        }

        public void AddWork(HTMLRecord record)
        {
            workQueue.Enqueue(record);
            allowSend.Set();
        }

        public void SendWork()
        {
            while(true)
            {
                allowSend.WaitOne();

                HTMLRecord record = null;
                if(workQueue.TryDequeue(out record))
                {
                    string JSON = this.SerializeToJSON(record, typeof(HTMLRecord));
                    crawlerClient.Send(JSON);
                    Console.WriteLine("\nSent Data from: \n" + record.URL);
                }
                else
                {
                    Console.WriteLine();
                }

                allowSend.Reset();
            }
        }
    }
}

