using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace WebCrawler
{
    static class DomainDictionary
    {
        private static Dictionary<string, Domain> domainDictionary = new Dictionary<string, Domain>();

        public static void Enqueue(string key, HTMLPage value)
        {
            lock (domainDictionary)
            {
                if (domainDictionary.ContainsKey(key))
                {
                    domainDictionary[key].Enqueue(value);
                }
                else
                {
                    domainDictionary[key] = new Domain(key);
                    domainDictionary[key].Enqueue(value);
                    domainDictionary[key].initTimer();
                }
            }
        }

        public static void removeDomain(string domainKey)
        {
            lock (domainDictionary)
            {
                domainDictionary.Remove(domainKey);
            }
        }
    }

    class Domain
    {
        private string domainName { get; set; }
        public DateTime lastUpdated { get; private set; }
        private Queue<HTMLPage> workQueue = new Queue<HTMLPage>();
        private ManualResetEvent loadPage = new ManualResetEvent(false);
        public int Count { get { return workQueue.Count; } }
        private Timer timer { get; set; }
        private const int DOMAIN_DELAY_PERIOD = 2000;

        public Domain(string domainName)
        {
            this.domainName = domainName;
            lastUpdated = DateTime.Now.AddMilliseconds(-(DOMAIN_DELAY_PERIOD + 1));
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
                DomainDictionary.removeDomain(this.domainName);
            }
        }

        public void Enqueue(HTMLPage page)
        {
            workQueue.Enqueue(page);
        }
    }
}
