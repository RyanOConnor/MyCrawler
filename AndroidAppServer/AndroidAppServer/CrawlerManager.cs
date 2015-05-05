using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;
using MongoDB.Bson;

namespace AndroidAppServer
{
    public enum CrawlerStatus { STARTING, WAITING, SENDING_DATA, SHUTTING_DOWN };
    public class CrawlerManager : SocketServer
    {
        private Dictionary<IPEndPoint, CrawlerNode> crawlerNodes { get; set; }
        private Dictionary<ObjectId, CrawlerNode> jobSet { get; set; }
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
            crawlerNodes = new Dictionary<IPEndPoint, CrawlerNode>();
            nodeIPAddresses = new HashSet<IPEndPoint>();
            jobSet = new Dictionary<ObjectId, CrawlerNode>();
            this.MessageReceived += new EventHandler<MessageEventArgs>(OnMessageReceived);
        }

        public void AllowCrawlerIP(IPEndPoint endPoint)
        {
            lock (nodeIPAddresses)
            {
                if (!nodeIPAddresses.Contains(endPoint))
                    nodeIPAddresses.Add(endPoint);
            }
        }

        protected override void HandleConnection(object socket)
        {
            listenerSignal.Set();

            Socket handler = (Socket)socket;

            if (nodeIPAddresses.Contains(handler.LocalEndPoint))
            {
                CrawlerNode node = new CrawlerNode();
                node.UpdateReceived += new EventHandler<HtmlRecord>(OnUpdateReceived);
                node.Start(handler);
                crawlerNodes.Add((IPEndPoint)handler.LocalEndPoint, node);
            }
            else
            {
                throw new Exception("Invalid IP attempting to connect");
            }
        }

        public void OnMessageReceived(object sender, MessageEventArgs args)
        {

        }

        public void DistributeWork(HtmlRecord record)
        {
            while (crawlerNodes.Count == 0) ;
            CrawlerNode node = crawlerNodes.OrderByDescending(x => x.Value.messageQueue.Count).Last().Value;
            jobSet[record.recordid] = node;
            node.EnqueueHtmlRecord(record);
        }

        public void OnUpdateReceived(object sender, HtmlRecord record)
        {
            jobSet.Remove(record.recordid);
        }

        public void GetJobStatus(ObjectId jobId)
        {
            StatusRequest statusRequest = new StatusRequest(jobId);
            jobSet[jobId].EnqueueMessage(statusRequest);
        }
    }

    public class CrawlerNode : SocketServer
    {
        public BlockingCollection<Message> messageQueue = new BlockingCollection<Message>();
        public Dictionary<ObjectId, Message> backupQueue = new Dictionary<ObjectId, Message>();
        public ManualResetEvent allowSend = new ManualResetEvent(false);
        public event EventHandler<HtmlRecord> UpdateReceived;
        private event EventHandler<KeyValuePair<ObjectId, JobStatus>> StatusReceived;

        public CrawlerNode()
        {
            MessageReceived += new EventHandler<MessageEventArgs>(OnMessageReceived);
            DataManager.Instance.AddCrawlerEvent(this);
            ClientConnected += new EventHandler(OnClientConnected);
            MessageSubmitted += new EventHandler(OnMessageSubmitted);
            StatusReceived += new EventHandler<KeyValuePair<ObjectId, JobStatus>>(DataManager.Instance.jobSchedule.OnStatusReceived);
        }

        public void Start(Socket socket)
        {
            IPEndPoint localEndPoint = socket.LocalEndPoint as IPEndPoint;
            StartClient(localEndPoint.Address);

            Thread receive = new Thread(new ParameterizedThreadStart(HandleConnection));
            receive.Start(socket);
        }

        public void OnClientConnected(object sender, EventArgs args)
        {
            Message readyMessage = new ReadyMessage(ObjectId.Empty);
            Send(BSON.Serialize<Message>(readyMessage));
        }

        public void OnMessageReceived(object sender, MessageEventArgs args)
        {
            Message response = null;
            if (args.message is ReadyMessage)
            {
                ReadyMessage readyMessage = args.message as ReadyMessage;
                RemoveFromBackup(readyMessage.idReceived);

                if (messageQueue.Count == 0)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(WaitSend));
                }
                else
                {
                    response = messageQueue.Take();
                }
            }
            else if (args.message is RecordMessage)
            {
                RecordMessage recordMessage = args.message as RecordMessage;

                EventHandler<HtmlRecord> updateReceived = UpdateReceived;
                if (updateReceived != null)
                {
                    var delegates = updateReceived.GetInvocationList();
                    foreach (EventHandler<HtmlRecord> receiver in delegates)
                    {
                        receiver.BeginInvoke(null, recordMessage.htmlRecord, OnDatabaseUpdated, null);
                    }
                }

                response = new ReadyMessage(recordMessage.id);
            }
            else if (args.message is StatusReport)
            {
                StatusReport report = args.message as StatusReport;

                EventHandler<KeyValuePair<ObjectId, JobStatus>> statusReceived = StatusReceived;
                if (statusReceived != null)
                {
                    statusReceived.Invoke(null, report.statusReport);
                }

                response = new ReadyMessage(report.id);
            }
            else if (args.message is DestroyedBuffer)
            {
                response = new ResendMessage();
            }
            else if (args.message is ResendMessage)
            {
                response = RetrieveFromBackup();
            }

            if (response != null)
            {
                Send(response);
            }
        }

        public void OnDatabaseUpdated(IAsyncResult result)
        {
            var async = (System.Runtime.Remoting.Messaging.AsyncResult)result;
            var invokedMethod = (EventHandler<HtmlRecord>)async.AsyncDelegate;
            invokedMethod.EndInvoke(result);
        }

        public void Send(Message message)
        {
            byte[] bson = BSON.Serialize<Message>(message);
            Send(bson);
        }

        public void WaitSend(object state)
        {
            allowSend.Reset();

            if (messageQueue.Count == 0)
                allowSend.WaitOne();

            Message message = messageQueue.Take();
            byte[] bson = BSON.Serialize<Message>(message);
            Send(bson);
        }

        public void OnMessageSubmitted(object sender, EventArgs args)
        {

        }

        public void AddToBackup(Message message)
        {
            lock (backupQueue)
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
            lock (backupQueue)
            {
                backupQueue.Remove(messageId);
            }
        }

        public void EnqueueHtmlRecord(HtmlRecord record)
        {
            lock (messageQueue)
            {
                RecordMessage message = new RecordMessage(record);
                messageQueue.Add(message);
                allowSend.Set();
            }
        }

        public void EnqueueMessage(Message message)
        {
            lock (messageQueue)
            {
                messageQueue.Add(message);
                allowSend.Set();
            }
        }
    }
}
