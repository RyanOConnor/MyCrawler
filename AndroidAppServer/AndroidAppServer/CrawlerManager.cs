using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using MongoDB.Bson;

namespace AndroidAppServer
{
    class CrawlerManager
    {
        private Dictionary<Uri, CrawlerNode> crawlerNodes = new Dictionary<Uri, CrawlerNode>();
        private Dictionary<ObjectId, CrawlerNode> jobSet = new Dictionary<ObjectId, CrawlerNode>();
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

        public void AddCrawlerNode(Uri domain)
        {
            CrawlerNode node;
            if (!crawlerNodes.ContainsKey(domain))
            {
                node = new CrawlerNode(domain);
                crawlerNodes.Add(node.nodeDomain, node);
            }
            else
            {
                node = crawlerNodes[domain];
                node.KillSendProcess();
            }
            node.Start();
        }

        public void DistributeWork(HtmlRecord record)
        {
            while (crawlerNodes.Count == 0) ;
            var nodes = crawlerNodes.OrderBy(x => x.Value.messageQueue.Count)
                                    .OrderBy(y => y.Value.messageQueue
                                                         .Where(z => z.domain.Host == record.domain.Host)
                                                         .Count());
            CrawlerNode node = nodes.ElementAt(0).Value;
            jobSet[record.recordid] = node;
            node.EnqueueHtmlRecord(record);
        }

        public void RemoveJob(ObjectId recordid)
        {
            if (jobSet.ContainsKey(recordid))
            {
                jobSet.Remove(recordid);
            }
        }

        public JobStatus CheckCrawlerJobStatus(ObjectId recordid)
        {
            if (jobSet.ContainsKey(recordid))
                return jobSet[recordid].HandleJobStatus(recordid);
            else
                return JobStatus.None;
        }

        private class CrawlerNode
        {
            public Uri nodeDomain { get; private set; }
            public BlockingCollection<HtmlRecord> messageQueue = new BlockingCollection<HtmlRecord>();
            private Thread sendProcess { get; set; }
            private const int maxRequestAttempts = 10;
            private bool processDestroyed = false;

            public CrawlerNode(Uri nodeDomain)
            {
                this.nodeDomain = nodeDomain;
            }

            public void Start()
            {
                sendProcess = new Thread(this.Send);
                sendProcess.Start();
            }

            public void Send()
            {
                RestAPI api = new RestAPI();
                while (!processDestroyed)
                {
                    HtmlRecord record = messageQueue.Take();
                    foreach (HtmlResults results in record.results.Values)
                    {
                        results.links = null;
                    }
                    byte[] recordString = record.ToBson<HtmlRecord>();
                    try
                    {
                        JObject obj = api.EnqueueJob(recordString);
                        bool messageReceived = obj.GetValue("Successful").Value<bool>();
                        if (messageReceived)
                        {
                            ServerResponse response = api.ParseResponse(obj);
                            if (response != ServerResponse.Success)
                            {
                                messageQueue.Add(record);
                            }
                            else
                                System.Diagnostics.Debug.Print("[" + DateTime.Now.ToString() + "] Sent: " + 
                                                                record.domain.AbsoluteUri);
                        }
                        else
                        {
                            messageQueue.Add(record);
                        }
                    }
                    catch (WebException ex)
                    {
                        System.Diagnostics.Debug.Print(ex.ToString());
                        messageQueue.Add(record);
                    }
                }
            }

            public void KillSendProcess()
            {
                processDestroyed = true;
                sendProcess.Join();
                sendProcess = new Thread(this.Send);
                sendProcess.Start();
            }

            public JobStatus HandleJobStatus(ObjectId recordid)
            {
                RestAPI api = new RestAPI();
                string recordIdStr = recordid.ToString();
                JObject obj = api.StatusRequest(recordIdStr);
                bool messageReceived = obj.GetValue("Successful").Value<bool>();
                int attempts = 0;
                while ( ( !messageReceived ) && ( attempts < maxRequestAttempts) )
                {
                    obj = api.StatusRequest(recordIdStr);
                    messageReceived = obj.GetValue("Successful").Value<bool>();
                    attempts++;
                }
                Tuple<ServerResponse, JobStatus> response = api.ParseStatusResponse(obj);
                return response.Item2;
            }

            public void EnqueueHtmlRecord(HtmlRecord record)
            {
                messageQueue.Add(record);
            }
        }
    }
}
