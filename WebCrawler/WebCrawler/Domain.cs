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

        public void initTimer()
        {
            timer = new Timer(new TimerCallback(update), null, 0, DOMAIN_DELAY_PERIOD);
        }

        public void update(object stateInfo)
        {
            if (workQueue.Count != 0)
            {
                HTMLPage page = workQueue.Dequeue();

                if (page.waitTime != 0)
                    ThreadPool.QueueUserWorkItem(new WaitCallback(page.sleepThenUpdate));
                else
                    page.beginUpdate();
            }
            else
            {
                timer.Dispose();
                WebCrawler.removeDomain(this.domainName);
            }
        }

        public void Enqueue(HTMLPage page)
        {
            workQueue.Enqueue(page);
        }
    }
}
