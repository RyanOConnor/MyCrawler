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

        public LinkOwner CreateEntry(IHtmlResults newUpdate, ObjectId userid)
        {
            try
            {
                HtmlRecord record = Database.Instance.htmlCollection.FindOneAs<HtmlRecord>(Query.EQ("url", newUpdate.domain.AbsoluteUri));
                LinkOwner ownerResult = null;
                if(record == null)
                {
                    record = new HtmlRecord(newUpdate.domain);
                    ownerResult = record.AddResults(newUpdate, userid);
                    Database.Instance.htmlCollection.Save(record, WriteConcern.Acknowledged);
                    jobSchedule.AddNewJob(record.id, record.timeStamp);
                    processJobs.Set();
                }
                else
                {
                    ownerResult = record.AddResults(newUpdate, userid);
                    Database.Instance.htmlCollection.Save(record, WriteConcern.Acknowledged);
                }

                return ownerResult;
            }
            catch(MongoWriteConcernException ex)
            {
                Console.WriteLine(ex.ToString());
                // TODO: stop potential stack overflow exception
                return CreateEntry(newUpdate, userid);
            }
        }

        // replicates CreateEntry in the event of record duplication during a parallel "Save"
        //      
        /*public LinkOwner ModifyHtmlResults(IHtmlResults modifiedEntry, LinkOwner owner)
        {
            try
            {
                HtmlRecord record = Database.Instance.htmlCollection.FindOneAs<HtmlRecord>(Query.EQ("url", modifiedEntry.domain.AbsoluteUri));
                LinkOwner ownerResult = record.ModifyResults(modifiedEntry, owner);

                Database.Instance.htmlCollection.Save(record, WriteConcern.Acknowledged);
                return ownerResult;
            }
            catch(MongoWriteConcernException ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }*/

        public bool Edit(HtmlRecord record)
        {
            var result = Database.Instance.htmlCollection.FindAndModify(Query.And(Query.EQ("url", record.domain.AbsoluteUri),
                                                                                  Query.EQ("Version", record.version)),
                                                                        null,
                                                                        Update.Set("_id", record.id)
                                                                              .Set("url", record.url)
                                                                              .Set("domain", record.domain.ToBson())
                                                                              .Set("timeStamp", record.timeStamp)
                                                                              .Set("results", record.results.ToBson())
                                                                              .Set("serverResponse", record.serverResponse)
                                                                              .Inc("version", 1));
            return result.ModifiedDocument != null;
        }

        public LinkOwner ModifyOwnership(LinkOwner newOwnerResults)
        {
            try
            {
                HtmlRecord record = RetrieveEntryById(newOwnerResults.resultsid);
                LinkOwner linkOwner = record.ModifyOwner(newOwnerResults);

                Database.Instance.htmlCollection.Save(record, WriteConcern.Acknowledged);
                return linkOwner;
            }
            catch(MongoWriteConcernException ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        // User is unsubscribing altogether
        public void RemoveResultsOwner(LinkOwner entryToRemove)
        {
            try
            {
                HtmlRecord record = RetrieveEntryById(entryToRemove.resultsid);
                record.RemoveResultsOwner(entryToRemove);

                Database.Instance.htmlCollection.Save(record, WriteConcern.Acknowledged);
            }
            catch(MongoWriteConcernException ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public LinkOwner ManualRequest(LinkOwner owner)
        {
            try
            {
                IMongoQuery query = Query.EQ("results.k", owner.resultsid);
                HtmlRecord record = Database.Instance.htmlCollection.FindOne(query);
                return record.results[owner.resultsid].RetrieveResults(owner.userid);
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
