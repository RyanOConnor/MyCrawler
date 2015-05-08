using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace AndroidAppServer
{
    public class DataManager
    {
        private Thread scheduleJobsProc { get; set; }
        private ManualResetEvent processEvent = new ManualResetEvent(false);
        private JobSchedule _jobSchedule = new JobSchedule();
        public JobSchedule jobSchedule
        {
            get { lock (_jobSchedule) { return _jobSchedule; } }
        }
        private static DataManager _instance;
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new DataManager();
                return _instance;
            }
        }

        public void Start()
        {
            jobSchedule.Initialize(Database.Instance.htmlCollection);

            scheduleJobsProc = new Thread(ScheduleJobs);
            scheduleJobsProc.Start();
        }

        private void ScheduleJobs()
        {
            try
            {
                while (true)
                {
                    KeyValuePair<DateTime, ObjectId> jobPair = jobSchedule.GetJob();
                    if (jobPair.Key > DateTime.UtcNow)
                    {
                        Thread.Sleep((int)(jobPair.Key - DateTime.UtcNow).TotalMilliseconds);
                    }

                    HtmlRecord record = RetrieveEntryById(jobPair.Value);
                    CrawlerManager.Instance.DistributeWork(record);

                    processEvent.Reset();
                }
            }
            catch (ThreadInterruptedException)
            {
                scheduleJobsProc = new Thread(ScheduleJobs);
                scheduleJobsProc.Start();
            }
        }

        public IHtmlRecord CreateHtmlRecord(Uri domain)
        {
            IHtmlRecord record = Database.Instance.htmlCollection.FindOneAs<HtmlRecord>
                                           (Query.EQ("url", domain.AbsoluteUri)) as IHtmlRecord;
            if (record == null)
            {
                record = new HtmlRecord(domain);
            }

            return record;
        }

        public IHtmlRecord GetHtmlRecord(ObjectId recordid)
        {
            return Database.Instance.htmlCollection.Find(Query.EQ("_id", recordid))
                                                   .SetLimit(1).First();
        }

        public void SaveHtmlRecord(IHtmlRecord record)
        {
            Database.Instance.htmlCollection.Save(record, WriteConcern.Acknowledged);
            if(!jobSchedule.JobInSchedule(record.recordid))
            {
                jobSchedule.AddNewJob(record.recordid, DateTime.UtcNow);
                scheduleJobsProc.Interrupt();
            }
        }

        public HtmlRecord RetrieveEntryById(ObjectId id)
        {
            IMongoQuery queryId = Query.EQ("_id", id);
            HtmlRecord entity = Database.Instance.htmlCollection.FindOne(queryId);
            return entity;
        }

        public void UpdateEntry(HtmlRecord record)
        {
            record.timeStamp = DateTime.UtcNow;
            jobSchedule.UpdateSchedule(record.recordid);
            Database.Instance.htmlCollection.Save(typeof(HtmlRecord), record);
            processEvent.Set();
        }
    }
}
