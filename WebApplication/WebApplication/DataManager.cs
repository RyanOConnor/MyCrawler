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
        private JobSchedule _jobSchedule = new JobSchedule();
        public JobSchedule jobSchedule
        {
            get { lock (_jobSchedule) { return _jobSchedule; } }
        }
        private ManualResetEvent processJobs = new ManualResetEvent(false);
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
            MongoCollection<HtmlRecord> htmlCollection = Database.Instance.htmlCollection;
            jobSchedule.Initialize(htmlCollection);

            Thread scheduler = new Thread(ScheduleJobs);
            scheduler.Start();
        }

        public void AddCrawlerEvent(CrawlerNode node)
        {
            node.UpdateReceived += new EventHandler<HtmlRecord>(UpdateEntry);
        }

        private void ScheduleJobs()
        {
            while (true)
            {
                if (jobSchedule.jobSchedule.Count == 0)
                {
                    processJobs.WaitOne();
                }

                ObjectId job = jobSchedule.GetJob();

                HtmlRecord record = RetrieveEntryById(job);
                CrawlerManager.Instance.DistributeWork(record);

                processJobs.Reset();
            }
        }

        public HtmlResults CreateEntry(HtmlResults newUpdate, ObjectId userId)
        {
            Type updateType = newUpdate.GetType();
            MongoCursor<HtmlRecord> records = Database.Instance.htmlCollection.FindAs<HtmlRecord>(Query.EQ("url", newUpdate.domain.AbsoluteUri));

            try
            {
                HtmlResults results = null;
                if (records.Count() == 0)
                {
                    HtmlRecord newHtmlRecord = new HtmlRecord(newUpdate.domain);
                    if (updateType == typeof(LinkFeed))
                    {
                        LinkFeed linkFeed = newUpdate as LinkFeed;
                        results = newHtmlRecord.AddLinkFeed(linkFeed, userId) as LinkFeedResults;
                    }
                    else if (updateType == typeof(TextUpdate))
                    {
                        TextUpdate textUpdate = newUpdate as TextUpdate;
                        results = newHtmlRecord.AddTextUpdate(textUpdate, userId) as TextUpdateResults;
                    }

                    Database.Instance.htmlCollection.Save(newHtmlRecord, WriteConcern.Acknowledged);
                    jobSchedule.AddNewJob(newHtmlRecord.id, newHtmlRecord.timeStamp);
                    processJobs.Set();

                    return results;
                }
                else if (records.Count() == 1)
                {
                    HtmlRecord recordToModify = records.First();
                    if (updateType == typeof(LinkFeed))
                    {
                        LinkFeed linkFeed = newUpdate as LinkFeed;
                        results = recordToModify.AddLinkFeed(linkFeed, userId);
                    }
                    else if (updateType == typeof(TextUpdate))
                    {
                        TextUpdate textUpdate = newUpdate as TextUpdate;
                        results = recordToModify.AddTextUpdate(textUpdate, userId) as TextUpdateResults;
                    }

                    Database.Instance.htmlCollection.Save(recordToModify, WriteConcern.Acknowledged);

                    return results;
                }
                else
                {
                    throw new Exception();
                }
            }
            catch(MongoWriteConcernException ex)
            {
                // If duplicate found, just call modify record
                Console.WriteLine(ex.ToString());
                return ModifyEntry(newUpdate, userId);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public HtmlResults ModifyEntry(HtmlResults modifiedEntry, ObjectId userId)
        {
            HtmlResults newResults = null;
            MongoCollection<HtmlRecord> htmlCollection = Database.Instance.htmlCollection;
            MongoCursor<HtmlRecord> records = htmlCollection.FindAs<HtmlRecord>(Query.EQ("Results.k", modifiedEntry.jobId));

            if (records.Count() == 1)
            {
                HtmlRecord recordToModify = records.First();
                HtmlResults results = recordToModify.results[modifiedEntry.jobId];
                if (results.GetType() == typeof(LinkFeedResults))
                {
                    LinkFeedResults feedResults = results as LinkFeedResults;
                    feedResults.RemoveOwner(userId);
                    htmlCollection.Save(recordToModify);

                    newResults = CreateEntry(modifiedEntry, userId) as LinkFeedResults;
                }
                else if (results.GetType() == typeof(TextUpdateResults))
                {
                    TextUpdateResults textResults = results as TextUpdateResults;
                    textResults.RemoveOwner(userId);
                    htmlCollection.Save(recordToModify);

                    newResults = CreateEntry(modifiedEntry, userId) as TextUpdateResults;
                }

                return newResults;
            }
            else
            {
                throw new Exception();
            }
        }

        public HtmlResults ManualRequest(ObjectId jobId)
        {
            try
            {
                IMongoQuery query = Query.EQ("Results.k", jobId);
                HtmlRecord record = Database.Instance.htmlCollection.FindOne(query);
                return record.results[jobId];
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public HtmlRecord RetrieveEntryById(ObjectId id)
        {
            try
            {
                IMongoQuery queryId = Query.EQ("_id", id);
                HtmlRecord entity = Database.Instance.htmlCollection.FindOne(queryId);
                return entity;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public void UpdateEntry(object sender, HtmlRecord record)
        {
            try
            {
                record.timeStamp = DateTime.UtcNow;
                jobSchedule.UpdateSchedule(record.id);
                Database.Instance.htmlCollection.Save(typeof(HtmlRecord), record);
                UserManager.Instance.UpdateUsersByRecord(record);
                processJobs.Set();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }
    }

    
}
