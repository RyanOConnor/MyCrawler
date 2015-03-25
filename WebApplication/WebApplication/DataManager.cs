using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Serialization;
using System.Net;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace WebApplication
{
    public class DataManager
    {
        private MongoCollection<HTMLRecord> CrawlCollection { get; set; }
        private PriorityQueue<DateTime, ObjectId> jobSchedule = new PriorityQueue<DateTime, ObjectId>();
        private ManualResetEvent processJobs = new ManualResetEvent(false);
        public const int UPDATE_FREQUENCY = 4;
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
            CrawlCollection = Database.Instance.GetCollection<HTMLRecord>("CrawlData");

            LoadJobSchedule();
            Thread scheduler = new Thread(ScheduleJobs);
            scheduler.Start();
        }

        public void AddCrawlerEvent(CrawlerNode node)
        {
            node.UpdateReceived += new EventHandler<HTMLRecord>(UpdateEntry);
        }

        private void LoadJobSchedule()
        {
            lock (jobSchedule)
            {
                lock (CrawlCollection)  // MongoCursor is NOT thread safe
                {
                    MongoCursor<HTMLRecord> records = CrawlCollection.FindAll();
                    foreach (HTMLRecord record in records)
                    {
                        EnqueueJobSchedule(record);
                    }
                }
            }
        }

        private void ScheduleJobs()
        {
            while(true)
            {
                if(jobSchedule.Count == 0)
                {
                    processJobs.WaitOne();
                }

                KeyValuePair<DateTime, ObjectId> job = DequeueJobSchedule();

                if (job.Key > DateTime.UtcNow)
                {
                    double ms = (job.Key - DateTime.UtcNow).TotalMilliseconds;
                    Thread.Sleep((int)(job.Key - DateTime.UtcNow).TotalMilliseconds);
                }

                HTMLRecord record = RetrieveEntry(job.Value);
                CrawlerManager.Instance.DistributeWork(record);
                processJobs.Reset();
            }
        }

        public HTMLRecord CreateEntry(ObjectId userId, NewRecord newRecord)
        {
            HTMLRecord newHtmlRecord = new HTMLRecord(userId, newRecord);
            CrawlCollection.Save(newHtmlRecord);
            return newHtmlRecord;
        }

        public void ModifyEntry(ObjectId urlid, object changes)
        {

        }

        public HTMLRecord RetrieveEntry(ObjectId id)
        {
            try
            {
                IMongoQuery queryId = Query.EQ("_id", id);
                HTMLRecord entity = CrawlCollection.FindOne(queryId);
                return entity;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public void UpdateEntry(object sender, HTMLRecord page)
        {
            try
            {
                page.TimeStamp = DateTime.UtcNow.AddMinutes(UPDATE_FREQUENCY);
                EnqueueJobSchedule(page);
                CrawlCollection.Save(page);
                UserManager.UpdateUserLink(page.UserId, page);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        /*public HTMLRecord findUrlById(ObjectId id)
        {
            return new HTMLRecord("", DateTime.Now, new List<string>(),new List<string>());
        }

        public HTMLRecord findUrlByUrl(string url)
        {
            return new HTMLRecord( "", DateTime.Now, new List<string>(), new List<string>());
        }

        public List<HTMLRecord> findMultipleByDateTime(List<DateTime> timeStamps)
        {
            return new List<HTMLRecord>();
        }

        public HTMLRecord findSingleByDateTime(DateTime timeStamp)
        {
            return new HTMLRecord( "", DateTime.Now, new List<string>(), new List<string>());
        }*/

        public void EnqueueJobSchedule(HTMLRecord record)
        {
            lock(jobSchedule)
            {
                jobSchedule.Enqueue(record.TimeStamp, record.Id);
                processJobs.Set();
            }
        }

        public KeyValuePair<DateTime, ObjectId> DequeueJobSchedule()
        {
            lock(jobSchedule)
            {
                return jobSchedule.Dequeue();
            }
        }
    }

    
    [DataContract]
    public class HTMLRecord : NewRecord, Serializable
    {
        [DataMember]
        public ObjectId UserId { get; private set; }
        [DataMember][BsonId]
        public ObjectId Id { get; set; }
        [DataMember]
        public DateTime TimeStamp { get; set; }
        [DataMember][BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, int> RankedResults { get; set; }
        [DataMember]
        public HttpStatusCode ServerResponse { get; set; }

        public HTMLRecord(ObjectId userId, NewRecord details)
            :base(details.URL, details.HtmlTags, details.Keywords)
        {
            this.UserId = userId;
            TimeStamp = DateTime.UtcNow;
        }
    }

    [DataContract]
    public class NewRecord : Serializable
    {
        [DataMember]
        public string URL { get; set; }
        [DataMember]
        public List<string> HtmlTags { get; set; }
        [DataMember]
        public List<string> Keywords { get; set; }

        public NewRecord(string url, List<string> tags, List<string> keywords)
        {
            this.URL = url;
            this.HtmlTags = tags;
            this.Keywords = keywords;
        }
    }
}
