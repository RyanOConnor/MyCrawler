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
        private Queue<HTMLPage> workQueue = new Queue<HTMLPage>();
        private Timer timer { get; set; }
        private const int DOMAIN_DELAY_PERIOD = 2000;

        public Domain(string domainName)
        {
            this.domainName = domainName;
        }

        public void InitTimer()
        {
            timer = new Timer(new TimerCallback(Update), null, 0, DOMAIN_DELAY_PERIOD);
        }
        
        public void Update(object stateInfo)
        {
            if (workQueue.Count != 0)
            {
                HTMLPage page = workQueue.Dequeue();

                if (page.WaitTime != 0)
                    ThreadPool.QueueUserWorkItem(new WaitCallback(page.SleepThenUpdate));
                else
                    page.BeginUpdate();
            }
            else
            {
                timer.Dispose();
                WebCrawler.Instance.RemoveDomain(this.domainName);
            }
        }

        public void Enqueue(HTMLPage page)
        {
            workQueue.Enqueue(page);
        }
    }
}
