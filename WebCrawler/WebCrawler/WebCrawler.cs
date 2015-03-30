using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using HtmlAgilityPack;

namespace WebCrawler
{
    public enum CrawlerStatus { STARTING, WAITING, SENDING_DATA, SHUTTING_DOWN };

    public class WebCrawler
    {
        public CrawlerStatus status { get; private set; }
        private Dictionary<string, Domain> domainDictionary { get; set; }
        private Queue<HTMLPage> resultQueue { get; set; }
        private ManualResetEvent waitForShutdown { get; set; }
        private ManualResetEvent waitForResults { get; set; }
        public const int TIMEOUT_PERIOD = 60000;
        private static WebCrawler _instance;
        public static WebCrawler Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new WebCrawler();
                return _instance;
            }
        }
        
        protected WebCrawler()
        {
            Initialize();
        }

        public void Initialize()
        {
            status = CrawlerStatus.STARTING;
            resultQueue = new Queue<HTMLPage>();
            waitForResults = new ManualResetEvent(false);
            waitForShutdown = new ManualResetEvent(false);
            domainDictionary = new Dictionary<string, Domain>();
            SocketClient.Instance.ClientConnected += new EventHandler<MessageEventArgs>(OnClientConnection);
            SocketClient.Instance.MessageSubmitted += new EventHandler(OnMessageSent);
            SocketServer.Instance.MessageReceived += new EventHandler<MessageEventArgs>(OnMessageReceived);
            SocketServer.Instance.ServerConnected += new EventHandler(OnServerConnection);
        }

        public void Start(string ipAd)
        {
            Thread startClient = new Thread(() => SocketClient.Instance.StartClient(ipAd));
            startClient.Start();
            //Thread responseProcess = new Thread(WebCrawler.Instance.SendResults);
            //responseProcess.Start();

            while (status != CrawlerStatus.SHUTTING_DOWN)
            {
                waitForShutdown.WaitOne();
            }
        }

        public void OnClientConnection(object sender, MessageEventArgs args)
        {
            SocketServer.Instance.StartListener(Encoding.UTF8.GetString(args.Message));
        }

        public void OnServerConnection(object sender, EventArgs args)
        {
            SocketClient.Instance.Send(Encoding.UTF8.GetBytes("ready"));
        }

        public void OnMessageReceived(object sender, MessageEventArgs args)
        {
            if(Encoding.UTF8.GetString(args.Message) == "ready")
            {
                ThreadPool.QueueUserWorkItem(Send);
            }
            else if (Encoding.UTF8.GetString(args.Message) == "SHUTDOWN")
            {
                status = CrawlerStatus.SHUTTING_DOWN;
                waitForShutdown.Set();
            }
            else
            {
                HTMLPage page = BSON.Deserialize<HTMLPage>(args.Message);
                Console.WriteLine("\nReceived: \n" + page.Domain.AbsoluteUri);
                EnqueueWork(page);

                SocketClient.Instance.Send(Encoding.UTF8.GetBytes("ready"));
            }
        }

        public void OnMessageSent(object sender, EventArgs args)
        {

        }

        public void Send(object obj)
        {
            if (resultQueue.Count == 0)
                waitForResults.WaitOne();

            if (resultQueue.Count != 0)
            {
                HTMLPage page = DequeueResult();
                byte[] bson = BSON.Serialize<HTMLPage>(page);
                SocketClient.Instance.Send(bson);
                Console.WriteLine("\nSent Data from: \n" + page.URL);
            }
            waitForResults.Reset();
        }

        public void EnqueueWork(HTMLPage page)
        {
            lock (domainDictionary)
            {
                string key = page.Domain.Host;
                if (domainDictionary.ContainsKey(key))
                {
                    domainDictionary[key].Enqueue(page);
                }
                else
                {
                    domainDictionary[key] = new Domain(key);
                    domainDictionary[key].Enqueue(page);
                    domainDictionary[key].InitTimer();
                }
            }
        }

        public void RemoveDomain(string domainKey)
        {
            lock (domainDictionary)
            {
                domainDictionary.Remove(domainKey);
            }
        }

        public void EnqueueResult(HTMLPage page)
        {
            lock (resultQueue)
            {
                resultQueue.Enqueue(page);
                waitForResults.Set();
            }
        }

        public HTMLPage DequeueResult()
        {
            lock (resultQueue)
            {
                return resultQueue.Dequeue();
            }
        }
    }
}
