using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace WebCrawlerNode
{
    public enum CrawlerStatus { Starting, Waiting, SendingData, ShuttingDown }
    public enum JobStatus
    {
        Requesting, HandlingResponse, LoadingPages, RankingPages,
        Finished, ErrorRequesting, ErrorLoading, None
    }

    class WebCrawler
    {
        private Thread send { get; set; }
        public CrawlerStatus status { get; private set; }
        private Dictionary<ObjectId, HtmlRecord> jobSet { get; set; }
        private Dictionary<string, Domain> domainDictionary { get; set; }
        private BlockingCollection<HtmlRecord> messageQueue { get; set; }
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
            messageQueue = new BlockingCollection<HtmlRecord>();
            allowSend = new ManualResetEvent(false);
            waitForShutdown = new ManualResetEvent(false);
            jobSet = new Dictionary<ObjectId, HtmlRecord>();
            domainDictionary = new Dictionary<string, Domain>();
        }

        public void Start(Uri appServerUri)
        {
            if (send == null)
            {
                RestAPI api = new RestAPI();
                JObject obj = api.Authenticate("http://localhost:51748/Handler.ashx");
                string cert = api.ParseAuthenticate(obj);

                send = new Thread(Send);
                send.Start();
            }
        }

        public void Send()
        {
            RestAPI api = new RestAPI();
            while (true)
            {
                HtmlRecord record = messageQueue.Take();
                byte[] recordBytes = BSON.Serialize(record);
                JObject obj = null;
                try
                {
                    obj = api.ReturnFinishedJob(recordBytes);
                    bool successful = obj.GetValue("Successful").Value<bool>();
                    if (successful)
                    {
                        ServerResponse response = api.ParseResponse(obj);
                        if (response != ServerResponse.Success)
                        {
                            messageQueue.Add(record);
                        }
                        else
                            System.Diagnostics.Debug.Print("[" + DateTime.Now.ToString() + "]" + 
                                " Sent: " + record.domain.AbsoluteUri);
                    }
                    else
                    {
                        messageQueue.Add(record);
                    }
                }
                catch(WebException ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                    messageQueue.Add(record);
                }
            }
        }

        public void TrackJob(HtmlRecord record)
        {
            lock (jobSet)
            {
                jobSet.Add(record.recordid, record);
            }
        }

        public void StopTrackingJob(ObjectId jobId)
        {
            lock (jobSet)
            {
                jobSet.Remove(jobId);
            }
        }

        public JobStatus GetJobStatus(ObjectId recordid)
        {
            lock (jobSet)
            {
                JobStatus status = JobStatus.None;
                if(jobSet.ContainsKey(recordid))
                {
                    HtmlRecord record = jobSet[recordid];
                    status = record.jobStatus;
                    if (status == JobStatus.ErrorLoading ||
                        status == JobStatus.ErrorRequesting ||
                        status == JobStatus.LoadingPages)
                    {
                        record.KillProcess();
                    }
                }                
                return status;
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
            }
        }

        public void RemoveDomain(string domainKey)
        {
            lock (domainDictionary)
            {
                domainDictionary.Remove(domainKey);
            }
        }

        public void EnqueueResult(HtmlRecord record)
        {
            messageQueue.Add(record);
        }
    }
}
