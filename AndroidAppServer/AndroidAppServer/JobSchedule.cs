using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AndroidAppServer
{
    public enum RecordStatus { Processing, Waiting };
    public enum JobStatus
    {
        Requesting, HandlingResponse, LoadingPages, RankingPages,
        Finished, ErrorRequesting, ErrorLoading, None
    };

    public class Job
    {
        public JobStatus jobStatus { get; set; }
        public RecordStatus recordStatus { get; set; }
        public ObjectId jobId { get; set; }
        public DateTime timeStamp { get; set; }
        public Timer timer { get; set; }

        public Job(ObjectId jobId, JobStatus status, DateTime time)
        {
            this.jobId = jobId;
            jobStatus = jobStatus;
            timeStamp = time;
        }

        public void SetTimer(TimerCallback callback, int delayPeriod)
        {
            timer = new Timer(callback, jobId, delayPeriod, Timeout.Infinite);
        }

        public void DestroyTimer()
        {
            timer.Dispose();
        }
    }

    public class JobSchedule
    {
        public const int UpdateFrequency = 4;
        private Dictionary<ObjectId, RecordStatus> jobStatus = new Dictionary<ObjectId, RecordStatus>();
        private HashSet<ObjectId> jobsProcessing = new HashSet<ObjectId>();
        private PriorityQueue<DateTime, ObjectId> _jobSchedule = new PriorityQueue<DateTime, ObjectId>();
        public PriorityQueue<DateTime, ObjectId> jobSchedule
        {
            get { lock (_jobSchedule) { return _jobSchedule; } }
        }
        private Dictionary<ObjectId, Job> _jobSet = new Dictionary<ObjectId, Job>();
        private Dictionary<ObjectId, Job> jobSet
        {
            get { lock (_jobSet) { return _jobSet; } }
        }

        public void Initialize(MongoCollection<HtmlRecord> records)
        {
            MongoCursor<HtmlRecord> cursor = records.FindAll();
            if (cursor.Count() > 0)
            {
                foreach (HtmlRecord record in cursor)
                {
                    Job job = new Job(record.recordid, JobStatus.None, record.timeStamp);
                    jobSet.Add(record.recordid, job);
                    jobSchedule.Enqueue(record.timeStamp, record.recordid);
                    jobStatus.Add(record.recordid, RecordStatus.Waiting);
                }
            }
        }

        public ObjectId GetJob()
        {
            int jobsInCirculation = jobSchedule.Count + jobsProcessing.Count;
            if (jobsInCirculation != jobSet.Count)
            {
                if (jobsInCirculation < jobSet.Count)
                {
                    foreach (Job job in jobSet.Values)
                    {
                        if (!jobsProcessing.Contains(job.jobId))
                        {
                            jobSchedule.Enqueue(DateTime.UtcNow, job.jobId);
                        }
                    }
                }
                else
                {
                    var scheduleDuplicates = jobSchedule.GroupBy(x => x.Value).Where(y => y.Count() > 1);
                    if (scheduleDuplicates.Count() > 0)
                    {
                        KeyValuePair<DateTime, ObjectId> duplicate = scheduleDuplicates.First().ElementAt(0);
                        jobSchedule.Remove(duplicate);
                    }
                    else if (jobSchedule.Where(x => !jobsProcessing.Contains(x.Value)).Any())
                    {
                        // Then the duplicate is both in the job schedule and dispatched to a crawler
                        //      Leave it scheduled and remove it from the processing set

                        var setIntersection = jobSchedule.Where(x => !jobsProcessing.Contains(x.Value));
                        foreach (KeyValuePair<DateTime, ObjectId> keyValue in setIntersection)
                        {
                            jobsProcessing.Remove(keyValue.Value);
                        }
                    }
                }
            }

            KeyValuePair<DateTime, ObjectId> pair = jobSchedule.Dequeue();
            ObjectId jobId = pair.Value;

            if (pair.Key > DateTime.UtcNow)
            {
                Thread.Sleep((int)(pair.Key - DateTime.UtcNow).TotalMilliseconds);
            }

            jobsProcessing.Add(jobId);
            jobStatus[jobId] = RecordStatus.Processing;
            SetStatusTimer(jobId);

            //PrintJobStatus();

            return jobId;
        }

        public void UpdateSchedule(ObjectId jobId)
        {
            jobsProcessing.Remove(jobId);
            DateTime deadline = DateTime.UtcNow.AddMinutes(UpdateFrequency);
            jobSchedule.Enqueue(deadline, jobId);
            jobSet[jobId].timeStamp = deadline;
            jobSet[jobId].DestroyTimer();
            jobStatus[jobId] = RecordStatus.Waiting;

            //PrintJobStatus();
        }

        public void AddNewJob(ObjectId jobId, DateTime time)
        {
            if (!jobSet.ContainsKey(jobId))
            {
                Job job = new Job(jobId, JobStatus.None, time);
                jobSet.Add(jobId, job);
                jobSchedule.Enqueue(DateTime.UtcNow, jobId);
            }
        }

        public void CheckStatus()
        {
            foreach (ObjectId jobId in jobsProcessing)
            {
                if (jobSet[jobId].timeStamp < DateTime.UtcNow.AddMinutes(UpdateFrequency))
                {
                    CrawlerManager.Instance.GetJobStatus(jobId);
                }
            }
        }

        public void SetStatusTimer(ObjectId jobId)
        {
            jobSet[jobId].SetTimer(new TimerCallback(CheckJobStatus), (int)TimeSpan.FromMinutes(UpdateFrequency).TotalMilliseconds);
        }

        public void CheckJobStatus(object stateInfo)
        {
            ObjectId jobId = (ObjectId)stateInfo;
            if (jobsProcessing.Contains(jobId))
            {
                CrawlerManager.Instance.GetJobStatus(jobId);
            }
        }

        public void OnStatusReceived(object sender, KeyValuePair<ObjectId, JobStatus> statusReport)
        {
            if (statusReport.Value == JobStatus.ErrorLoading || statusReport.Value == JobStatus.ErrorRequesting || statusReport.Value == JobStatus.LoadingPages)
            {
                jobsProcessing.Remove(statusReport.Key);
                jobSchedule.Enqueue(DateTime.UtcNow, statusReport.Key);
            }
        }

        private void PrintJobStatus()
        {
            Console.Clear();
            Console.Write("Job Schedule \t\t\t\t\t\t\t Processing\n\n");
            foreach (KeyValuePair<ObjectId, RecordStatus> pair in jobStatus)
            {
                if (pair.Value == RecordStatus.Waiting)
                    Console.WriteLine("[{0}] {1}", jobSet[pair.Key].timeStamp, pair.Key);
                else if (pair.Value == RecordStatus.Processing)
                    Console.WriteLine(" \t\t\t\t\t\t\t [{0}] {1}", jobSet[pair.Key].timeStamp, pair.Key);
            }

            if (jobSchedule.Count > 0)
            {
                Console.WriteLine("\n\nNext: [{0}] {1}", jobSchedule.Peek().Key, jobSchedule.Peek().Value);
            }
        }
    }
}
