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
    public enum RecordStatus { Processing, Waiting }
    public enum JobStatus
    {
        Requesting, HandlingResponse, LoadingPages, RankingPages,
        Finished, ErrorRequesting, ErrorLoading, None
    }

    public class Job
    {
        public JobStatus jobStatus { get; set; }
        public RecordStatus recordStatus { get; set; }
        public ObjectId recordid { get; set; }
        public DateTime deadline { get; set; }
        public Timer timer { get; set; }

        public Job(ObjectId recordid, JobStatus status, DateTime time)
        {
            this.recordid = recordid;
            jobStatus = jobStatus;
            deadline = time;
        }

        public void SetTimer(TimerCallback callback, int delayPeriod)
        {
            timer = new Timer(callback, recordid, delayPeriod, Timeout.Infinite);
        }

        public void DestroyTimer()
        {
            timer.Dispose();
        }
    }

    public class JobSchedule
    {
        public const int UpdateFrequency = 4;
        private Thread getJobThread { get; set; }
        private Dictionary<ObjectId, RecordStatus> jobStatus { get; set; }
        private HashSet<ObjectId> jobsProcessing { get; set; }
        private PriorityQueue<DateTime, ObjectId> _jobSchedule { get; set; }
        public PriorityQueue<DateTime, ObjectId> jobSchedule
        {
            get 
            {
                if (_jobSchedule == null)
                    _jobSchedule = new PriorityQueue<DateTime, ObjectId>();
                lock (_jobSchedule) 
                {
                    return _jobSchedule; 
                } 
            }
        }
        private Dictionary<ObjectId, Job> _jobSet { get; set; }
        private Dictionary<ObjectId, Job> jobSet
        {
            get 
            {
                if (_jobSet == null)
                    _jobSet = new Dictionary<ObjectId, Job>();
                lock (_jobSet) 
                {
                    return _jobSet; 
                } 
            }
        }

        public void Initialize(MongoCollection<HtmlRecord> records)
        {
            jobStatus = new Dictionary<ObjectId, RecordStatus>();
            jobsProcessing = new HashSet<ObjectId>();

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

        public KeyValuePair<DateTime, ObjectId> GetJob()
        {
            KeyValuePair<DateTime, ObjectId> pair = jobSchedule.Dequeue();
            ObjectId jobId = pair.Value;
            jobsProcessing.Add(jobId);
            jobStatus[jobId] = RecordStatus.Processing;
            SetStatusTimer(jobId);

            return pair;
        }

        public void UpdateSchedule(ObjectId recordid)
        {
            jobsProcessing.Remove(recordid);
            DateTime newDeadLine = DateTime.UtcNow.AddMinutes(UpdateFrequency);
            jobSchedule.Enqueue(newDeadLine, recordid);
            jobSet[recordid].deadline = newDeadLine;
            jobSet[recordid].DestroyTimer();
            jobStatus[recordid] = RecordStatus.Waiting;
        }

        public bool JobInSchedule(ObjectId recordid)
        {
            if (jobSet.ContainsKey(recordid))
                return true;
            else
                return false;
        }

        public void ReturnJob(KeyValuePair<DateTime, ObjectId> jobPair)
        {

        }

        public void AddNewJob(ObjectId recordid, DateTime deadline)
        {
            if (!jobSet.ContainsKey(recordid))
            {
                Job job = new Job(recordid, JobStatus.None, deadline);
                jobSet.Add(recordid, job);
                jobSchedule.Enqueue(deadline, recordid);
            }
        }

        public void SetStatusTimer(ObjectId recordid)
        {
            jobSet[recordid].SetTimer(new TimerCallback(CheckJobStatus), 
                                     (int)TimeSpan.FromMinutes(UpdateFrequency).TotalMilliseconds);
        }

        public void CheckJobStatus(object stateInfo)
        {
            ObjectId recordid = (ObjectId)stateInfo;
            if (jobsProcessing.Contains(recordid))
            {
                JobStatus status = CrawlerManager.Instance.CheckCrawlerJobStatus(recordid);
                if (status == JobStatus.ErrorLoading || status == JobStatus.ErrorRequesting || status == JobStatus.LoadingPages)
                {
                    jobsProcessing.Remove(recordid);
                    jobSchedule.Enqueue(DateTime.UtcNow, recordid);
                }
            }
        }
    }
}
