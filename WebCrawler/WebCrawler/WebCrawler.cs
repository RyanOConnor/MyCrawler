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
using MongoDB.Bson;

namespace WebCrawler
{
    public enum PriorityLevel { Ready, Highest, High, Normal, Low, Lowest };
    public enum CrawlerStatus { Starting, Waiting, SendingData, ShuttingDown };
    public enum JobStatus { Requesting, HandlingResponse, LoadingPages, RankingPages, 
                            Finished, ErrorRequesting, ErrorLoading};

    public class WebCrawler : SocketServer
    {
        public CrawlerStatus status { get; private set; }
        private Dictionary<ObjectId, HtmlRecord> jobSet { get; set; }
        private Dictionary<string, Domain> domainDictionary { get; set; }
        private PriorityQueue<PriorityLevel, Message> messageQueue { get; set; }
        private Dictionary<ObjectId, Message> backupQueue { get; set; }
        private ManualResetEvent waitForShutdown { get; set; }
        private ManualResetEvent allowSend { get; set; }
        public const int TimeoutPeriod = 60000;
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
            status = CrawlerStatus.Starting;
            messageQueue = new PriorityQueue<PriorityLevel, Message>();
            allowSend = new ManualResetEvent(false);
            waitForShutdown = new ManualResetEvent(false);
            jobSet = new Dictionary<ObjectId, HtmlRecord>();
            backupQueue = new Dictionary<ObjectId, Message>();
            domainDictionary = new Dictionary<string, Domain>();
            ClientConnected += new EventHandler<ClientConnectedArgs>(OnClientConnection);
            MessageReceived += new EventHandler<MessageEventArgs>(OnMessageReceived);
            ServerConnected += new EventHandler(OnServerConnection);
        }

        public void Start(string ipAd)
        {
            Thread startClient = new Thread(() => StartClient(ipAd));
            startClient.Start();

            while (status != CrawlerStatus.ShuttingDown)
            {
                waitForShutdown.WaitOne();
            }
        }

        public void OnClientConnection(object sender, ClientConnectedArgs args)
        {
            StartListening();
        }

        public void OnServerConnection(object sender, EventArgs args)
        {
            Message readyMessage = new ReadyMessage(ObjectId.Empty);
            Send(BSON.Serialize<Message>(readyMessage));
        }

        public void OnMessageReceived(object sender, MessageEventArgs args)
        {
            Message response = null;

            if(args.message is ReadyMessage)
            {
                ReadyMessage readyMessage = args.message as ReadyMessage;

                RemoveFromBackup(readyMessage.idReceived);
                if (messageQueue.Count == 0)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(WaitSend));
                }
                else
                {
                    response = messageQueue.DequeueValue();
                }
            }
            else if(args.message is RecordMessage)
            {
                RecordMessage recordMessage = args.message as RecordMessage;

                EnqueueWork(recordMessage.htmlRecord);
                TrackJob(recordMessage.htmlRecord);

                response = new ReadyMessage(recordMessage.id);
            }
            else if(args.message is StatusRequest)
            {
                StatusRequest statusRequest = args.message as StatusRequest;

                ObjectId jobId = statusRequest.requestedId;
                JobStatus status = GetJobStatus(jobId);

                if(status == JobStatus.ErrorLoading || status == JobStatus.ErrorRequesting || status == JobStatus.LoadingPages)
                {
                    StopTrackingJob(jobId);
                }
                KeyValuePair<ObjectId, JobStatus> statusPair = new KeyValuePair<ObjectId, JobStatus>
                                                                     (key: jobId, value: status);
                response = new StatusReport(statusPair);
            }
            else if (args.message is DestroyedBuffer)
            {
                response = new ResendMessage();
            }
            else if(args.message is ResendMessage)
            {
                response = RetrieveFromBackup();
            }

            if(response != null)
            {
                Send(response);
            }

            PrintJobs();
        }

        public void Send(Message message)
        {
            if (message is RecordMessage)
            {
                RecordMessage rm = message as RecordMessage;
                StopTrackingJob(rm.htmlRecord.recordid);
            }

            byte[] bson = BSON.Serialize<Message>(message);
            Send(bson);
        }

        public void WaitSend(object state)
        {
            allowSend.Reset();

            if (messageQueue.Count == 0)
                allowSend.WaitOne();

            Message message = DequeueMessage();
            if (message != null)
            {
                Send(message);
            }
        }

        public void AddToBackup(Message message)
        {
            lock(backupQueue)
            {
                if (!backupQueue.ContainsKey(message.id))
                {
                    backupQueue.Add(message.id, message);
                }
            }
        }

        public Message RetrieveFromBackup()
        {
            lock (backupQueue)
            {
                if (backupQueue.Count != 0)
                    return backupQueue.Values.First();
                else
                    return null;
            }
        }

        public void RemoveFromBackup(ObjectId messageId)
        {
            lock(backupQueue)
            {
                backupQueue.Remove(messageId);
            }
        }

        public void TrackJob(HtmlRecord record)
        {
            lock(jobSet)
            {
                jobSet.Add(record.recordid, record);
            }
        }

        public void StopTrackingJob(ObjectId jobId)
        {
            lock(jobSet)
            {
                jobSet.Remove(jobId);
            }
        }

        public JobStatus GetJobStatus(ObjectId jobId)
        {
            lock(jobSet)
            {
                return jobSet[jobId].jobStatus;
            }
        }

        public void EnqueueWork(HtmlRecord record)
        {
            lock (domainDictionary)
            {
                string key = record.domain.Host;
                if (domainDictionary.ContainsKey(key))
                {
                    domainDictionary[key].Enqueue(record);
                }
                else
                {
                    domainDictionary[key] = new Domain(key);
                    domainDictionary[key].Enqueue(record);
                    domainDictionary[key].InitTimer();
                }
                //PrintDomains();
            }
        }

        public void RemoveDomain(string domainKey)
        {
            lock (domainDictionary)
            {
                domainDictionary.Remove(domainKey);

                //PrintDomains();
            }
        }

        public void EnqueueResult(HtmlRecord record)
        {
            lock (messageQueue)
            {
                Message message = new RecordMessage(record);
                messageQueue.Enqueue(PriorityLevel.Normal, message);
                allowSend.Set();
            }
        }

        public void EnqueueMessage(PriorityLevel priority, Message message)
        {
            lock(messageQueue)
            {
                messageQueue.Enqueue(priority, message);
                //waitForResults.Set();
            }
        }

        public Message DequeueMessage()
        {
            lock (messageQueue)
            {
                if (messageQueue.Count != 0)
                {
                    return messageQueue.DequeueValue();
                }
                else
                {
                    return null;
                }
            }
        }

        public void PrintDomains()
        {
            Console.Clear();
            Console.WriteLine("Domains\n\n");
            foreach(KeyValuePair<string, Domain> pair in domainDictionary)
            {
                Console.WriteLine(pair.Key);
            }
        }

        public void PrintJobs()
        {
            Console.Clear();
            Console.WriteLine("Jobs\n\n");
            foreach(HtmlRecord record in jobSet.Values)
            {
                Console.WriteLine("[{0}]\t\t{1}", record.jobStatus.ToString(), record.url);
            }
        }
    }
}
