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
        private MongoCollection<HtmlRecord> CrawlCollection { get; set; }
        private PriorityQueue<DateTime, ObjectId> jobSchedule = new PriorityQueue<DateTime, ObjectId>();
        private ManualResetEvent processJobs = new ManualResetEvent(false);
        public const int UPDATE_FREQUENCY = 2;
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
            CrawlCollection = Database.Instance.GetCollection<HtmlRecord>("CrawlData");

            LoadJobSchedule();
            Thread scheduler = new Thread(ScheduleJobs);
            scheduler.Start();
        }

        public void AddCrawlerEvent(CrawlerNode node)
        {
            node.UpdateReceived += new EventHandler<HtmlRecord>(UpdateEntry);
        }

        private void LoadJobSchedule()
        {
            lock (jobSchedule)
            {
                lock (CrawlCollection)  // MongoCursor is NOT thread safe
                {
                    MongoCursor<HtmlRecord> records = CrawlCollection.FindAll();
                    int counter = 0;
                    foreach (HtmlRecord record in records)
                    {
                        EnqueueJobSchedule(record);
                        counter++;
                    }
                    Console.WriteLine("Loaded {0} objects into job schedule", counter);
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
                    Thread.Sleep((int)(job.Key - DateTime.UtcNow).TotalMilliseconds);
                }

                HtmlRecord record = RetrieveEntryById(job.Value);
                CrawlerManager.Instance.DistributeWork(record);
                processJobs.Reset();
            }
        }

        public KeyValuePair<ObjectId, HtmlResults> CreateEntry(NewRecord newRecord)
        {
            MongoCursor<HtmlRecord> records = CrawlCollection.FindAs<HtmlRecord>(Query.EQ("URL", newRecord.URL));

            if (records.Count() == 0)
            {
                HtmlRecord newHtmlRecord = new HtmlRecord(newRecord.URL);
                HtmlResults newResults = newHtmlRecord.AddLinkOwner(newRecord);
                CrawlCollection.Save(newHtmlRecord);
                EnqueueJobSchedule(newHtmlRecord);
                KeyValuePair<ObjectId, HtmlResults> pair = new KeyValuePair<ObjectId, HtmlResults>
                                                              (key: newHtmlRecord.Id, value: newResults);
                return pair;
            }
            else if (records.Count() == 1)
            {
                HtmlRecord recordToModify = records.First();
                HtmlResults newResults = recordToModify.AddLinkOwner(newRecord);
                CrawlCollection.Save(recordToModify);
                KeyValuePair<ObjectId, HtmlResults> pair = new KeyValuePair<ObjectId, HtmlResults>
                                                               (key: recordToModify.Id, value: newResults);
                return pair;
            }
            else
            {
                throw new Exception();
            }
        }

        public void ModifyEntry(ObjectId urlid, object changes)
        {

        }

        public HtmlRecord RetrieveEntryById(ObjectId id)
        {
            try
            {
                IMongoQuery queryId = Query.EQ("_id", id);
                HtmlRecord entity = CrawlCollection.FindOne(queryId);
                return entity;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public void UpdateEntry(object sender, HtmlRecord page)
        {
            try
            {
                page.TimeStamp = DateTime.UtcNow.AddMinutes(UPDATE_FREQUENCY);
                EnqueueJobSchedule(page);
                CrawlCollection.Save(typeof(HtmlRecord), page);
                UserManager.Instance.UpdateUsersByRecord(page);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public void EnqueueJobSchedule(HtmlRecord record)
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

    public class HtmlRecord : Serializable
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string URL { get; set; }
        public Uri Domain { get; set; }
        public DateTime TimeStamp { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ObjectId, HtmlResults> Results { get; set; }
        public HttpStatusCode ServerResponse { get; set; }

        public HtmlRecord(string url)
        {
            this.URL = url;
            Domain = new Uri(url);
            TimeStamp = DateTime.UtcNow;
            Results = new Dictionary<ObjectId, HtmlResults>();
        }

        public HtmlResults AddLinkOwner(NewRecord newRecord)
        {
            HtmlResults userResults = null;
            IEnumerable<HtmlResults> existingResults = Results.Values.Where(val => val.HtmlTags
                                                                     .SequenceEqual(newRecord.HtmlTags));
            if(existingResults.Count() == 1)
            {
                userResults = existingResults.First(x => x != null);
                if (newRecord.RecordType == typeof(TextUpdate) && userResults.GetType() == typeof(TextUpdate))
                {
                    userResults.AddResultsOwner(newRecord.UserId);
                    return userResults;
                }
                else if (newRecord.RecordType == typeof(LinkFeed) && userResults.GetType() == typeof(LinkFeed))
                {
                    LinkFeed feed = userResults as LinkFeed;
                    if(feed.Keywords.SequenceEqual(newRecord.Keywords))
                    {
                        feed.AddResultsOwner(newRecord.UserId);
                        return feed;
                    }
                }
            }
            else if(existingResults.Count() == 0)
            {
                userResults = null;

                if(newRecord.RecordType == typeof(TextUpdate))
                {
                    userResults = new TextUpdate(newRecord);
                }
                else if(newRecord.RecordType == typeof(LinkFeed))
                {
                    userResults = new LinkFeed(newRecord);
                }

                Results.Add(userResults.JobId, userResults);
                return userResults;
            }

            return userResults;
        }

        public HtmlResults FindExistingEntry(HtmlResults newResults)
        {
            var existingEntry = Results.Where(val => val.GetType() == newResults.GetType())
                                       .Where(val => val.Value.HtmlTags.Intersect(newResults.HtmlTags)
                                                                       .Count() == newResults.HtmlTags.Count());
            if (existingEntry.Count() == 1)
                return existingEntry.First(x => x.Value != null).Value;
            else
                throw new Exception();
        }

        public List<HtmlResults> GetResultsByType(Type type)
        {
            if (type == typeof(LinkFeed) || type == typeof(TextUpdate))
                return Results.Values.Where(val => val.GetType() == type).ToList();
            else
                return null;
        }
    }

    [BsonKnownTypes(typeof(LinkFeed), typeof(TextUpdate))]
    public class HtmlResults : Serializable
    {
        [BsonId]
        public ObjectId JobId { get; set; }
        public List<ObjectId> UserIDs { get; set; }
        public Uri Domain { get; set; }
        public List<string> HtmlTags { get; set; }
        public bool ChangeInContent { get; set; }

        public HtmlResults(ObjectId userid, string url, List<string> tags)
        {
            JobId = ObjectId.GenerateNewId();
            UserIDs = new List<ObjectId>();
            UserIDs.Add(userid);
            Domain = new Uri(url);
            HtmlTags = tags;
        }

        public void AddResultsOwner(ObjectId userid)
        {
            UserIDs.Add(userid);
        }
    }

    public class LinkFeed : HtmlResults, Serializable
    {
        public List<string> Keywords { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, int> RankedResults { get; set; }

        public LinkFeed(ObjectId userid, string url, List<string> tags, List<string> keywords)
            :base(userid, url, tags)
        {
            this.Keywords = keywords;
        }

        public LinkFeed(NewRecord newRecord)
            : base(newRecord.UserId, newRecord.URL, newRecord.HtmlTags)
        {
            this.Keywords = newRecord.Keywords;
        }
    }

    public class TextUpdate : HtmlResults, Serializable
    {
        public string PreviousText { get; set; }
        public string CurrentText { get; set; }

        public TextUpdate(ObjectId userid, string url, List<string> tags, string innerText)
            :base(userid, url, tags)
        {
            PreviousText = innerText;
        }

        public TextUpdate(NewRecord newRecord)
            : base(newRecord.UserId, newRecord.URL, newRecord.HtmlTags)
        {
            PreviousText = newRecord.EmbeddedText;
        }
    }
}
