using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace WebApplication
{
    public class DataManager
    {
        private MongoServer Server { get; set; }
        private MongoDatabase Database { get; set; }
        private MongoCollection<HTMLRecord> Collection { get; set; }
        private MongoClient dbClient { get; set; }
        private Queue<HTMLRecord> writeQueue = new Queue<HTMLRecord>();
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

        public DataManager()
        {
        }

        public void Start()
        {
            dbClient = new MongoClient();
            Server = dbClient.GetServer();
            Database = Server.GetDatabase("CrawlData");
            Collection = Database.GetCollection<HTMLRecord>("PageQueue");
            
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
                MongoCursor<HTMLRecord> records = Collection.FindAll();
                foreach (HTMLRecord record in records)
                {
                    EnqueueJobSchedule(record);
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

        public void CreateEntry(ObjectId urlid, string url)
        {

        }

        public void ModifyEntry(ObjectId urlid, object changes)
        {

        }

        public HTMLRecord RetrieveEntry(ObjectId id)
        {
            var queryId = Query.EQ("_id", id);
            var entity = Collection.FindOne(queryId);
            return entity;
        }

        public void UpdateEntry(object sender, HTMLRecord page)
        {
            try
            {
                page.TimeStamp = DateTime.UtcNow.AddMinutes(UPDATE_FREQUENCY);
                EnqueueJobSchedule(page);
                Collection.Save(page);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public HTMLRecord findUrlById(ObjectId id)
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
        }

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
    public class HTMLRecord
    {
        [DataMember]
        public ObjectId Id { get; set; }
        [DataMember]
        public string URL { get; set; }
        [DataMember]
        public DateTime TimeStamp { get; set; }
        [DataMember]
        public List<string> HtmlTags { get; set; }
        [DataMember]
        public List<string> Keywords { get; set; }
        [DataMember][BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, int> RankedResults { get; set; }

        public HTMLRecord(string url, DateTime timeStamp, List<string> tags, List<string> keywords)
        {
            this.URL = url;
            this.TimeStamp = timeStamp;
            this.HtmlTags = tags;
            this.Keywords = keywords;
        }
    }
}
