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
        private string DomainName { get; set; }
        private Queue<HTMLPage> workQueue = new Queue<HTMLPage>();
        private Timer Timer { get; set; }
        private const int DOMAIN_DELAY_PERIOD = 2000;

        public Domain(string domainName)
        {
            this.DomainName = domainName;
        }

        public void InitTimer()
        {
            Timer = new Timer(new TimerCallback(Update), null, 0, DOMAIN_DELAY_PERIOD);
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
                Timer.Dispose();
                WebCrawler.Instance.RemoveDomain(this.DomainName);
                Console.WriteLine(DomainName + " removed");
            }
        }

        public void Enqueue(HTMLPage page)
        {
            workQueue.Enqueue(page);
        }
    }
}
