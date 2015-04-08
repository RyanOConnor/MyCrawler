using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace WebCrawler
{
    class Domain
    {
        private string domainName { get; set; }
        private Queue<HtmlRecord> workQueue = new Queue<HtmlRecord>();
        private Timer timer { get; set; }
        private const int DomainDelayPeriod = 2000;

        public Domain(string domainName)
        {
            this.domainName = domainName;
        }

        public void InitTimer()
        {
            timer = new Timer(new TimerCallback(Update), null, 0, DomainDelayPeriod);
        }
        
        public void Update(object stateInfo)
        {
            if (workQueue.Count != 0)
            {
                HtmlRecord record = Dequeue();

                if (record.waitTime != 0)
                    ThreadPool.QueueUserWorkItem(new WaitCallback(record.SleepThenUpdate));
                else
                    record.BeginUpdate();
            }
            else
            {
                timer.Dispose();
                WebCrawler.Instance.RemoveDomain(this.domainName);
            }
        }

        public void Enqueue(HtmlRecord page)
        {
            lock (workQueue)
            {
                workQueue.Enqueue(page);
            }
        }

        private HtmlRecord Dequeue()
        {
            lock(workQueue)
            {
                return workQueue.Dequeue();
            }
        }
    }
}
