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
            Thread responseProcess = new Thread(WebCrawler.Instance.SendResults);
            responseProcess.Start();

            while (status != CrawlerStatus.SHUTTING_DOWN)
            {
                waitForShutdown.WaitOne();
            }
        }

        public void OnClientConnection(object sender, MessageEventArgs args)
        {
            SocketServer.Instance.StartListener(args.Message);
        }

        public void OnServerConnection(object sender, EventArgs args)
        {
            SocketClient.Instance.Send("ready");
        }

        public void OnMessageReceived(object sender, MessageEventArgs args)
        {
            if(args.Message == "SHUTDOWN")
            {
                status = CrawlerStatus.SHUTTING_DOWN;
                waitForShutdown.Set();
            }
            else if(args.Message == "status")
            {
                SocketClient.Instance.Send(status.ToString());
            }
            else
            {
                HTMLPage page = JSON.Deserialize<HTMLPage>(args.Message);
                Console.WriteLine("\nReceived: \n" + page.Domain.AbsoluteUri);
                EnqueueWork(page);
                SocketClient.Instance.Send("ready");
            }
        }

        public void OnMessageSent(object sender, EventArgs args)
        {

        }

        public void SendResults()
        {
            while(status != CrawlerStatus.SHUTTING_DOWN)
            {
                waitForResults.Reset();
                if (resultQueue.Count == 0)
                {
                    status = CrawlerStatus.WAITING;
                    waitForResults.WaitOne();
                }
                else
                {
                    status = CrawlerStatus.SENDING_DATA;
                }
                HTMLPage page = DequeueResult();
                string json = JSON.Serialize<HTMLPage>(page);
                SocketClient.Instance.Send(json);
                Console.WriteLine("\nSent: \n" + page.Domain.AbsoluteUri);
            }
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
