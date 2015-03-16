using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Runtime.Serialization.Json;
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
            SocketServer.Connected += new EventHandler(OnConnection);
            SocketServer.MessageSubmitted += new EventHandler(OnMessageSent);
            SocketServer.MessageReceived += new EventHandler<MessageEventArgs>(OnMessageReceived);
        }

        public void Start()
        {
            SocketServer.StartClient();
            Thread responseProcess = new Thread(SendResults);
            responseProcess.Start();

            while (status != CrawlerStatus.SHUTTING_DOWN)
            {
                waitForShutdown.WaitOne();
            }
        }

        public void OnConnection(object sender, EventArgs args)
        {
            SocketServer.Send("ready");
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
                SocketServer.Send(status.ToString());
            }
            else
            {
                EnqueueWork(DeserializeJSON(args.Message));
                SocketServer.Send("ready");
            }
        }

        public void OnMessageSent(object sender, EventArgs args)
        {
            SocketServer.Receive();
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
                SocketServer.Send(SerializeToJSON(page));
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

        private HTMLPage DeserializeJSON(string message)
        {
            try
            {
                if (message.EndsWith("<EOF>"))
                {
                    string token = "<EOF>";
                    char[] eof = token.ToCharArray();
                    message = message.TrimEnd(eof);
                }
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLPage));
                MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(message));
                return ser.ReadObject(stream) as HTMLPage;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        private string SerializeToJSON(HTMLPage page)
        {
            try
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLPage));
                MemoryStream stream = new MemoryStream();
                ser.WriteObject(stream, page);
                return Encoding.ASCII.GetString(stream.ToArray());
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }
    }
}
