using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace WebCrawler
{
    class DomainDictionary
    {
        private readonly Dictionary<string, Domain> domainDictionary = new Dictionary<string, Domain>();
        public int Count { get; private set; }

        public HTMLPage Dequeue()
        {
            lock (domainDictionary)
            {
                int index = 0;                                          
                List<string> keys = domainDictionary.Keys.ToList();

                // reimplement while loop with Timer class and Timer callback function
                while (domainDictionary[keys[index]].IsNotAllowedToUpdate())
                {
                    if (index == keys.Count - 1)
                        index = 0;
                    else
                        index++;
                }

                HTMLPage value = domainDictionary[keys[index]].Dequeue();

                if (domainDictionary[keys[index]].Count == 0)
                {
                    domainDictionary.Remove(keys[index]);
                }

                Count = domainDictionary.Count;
                //printDomains();
                return value;
            }
        }

        public void printDomains()
        {
            Console.Clear();
            foreach(KeyValuePair<string, Domain> pair in domainDictionary)
            {
                Console.WriteLine("{0} items - {1}", pair.Value.Count, pair.Key);
            }
            
        }

        public void Enqueue(string key, HTMLPage value)
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
                }
                Count = domainDictionary.Count;
            }
        }
    }

    class Domain
    {
        private string domainName { get; set; }
        public DateTime lastUpdated { get; private set; }
        private readonly Queue<HTMLPage> workQueue = new Queue<HTMLPage>();
        public int Count { get { return workQueue.Count; } }
        private readonly int DOMAIN_DELAY_PERIOD = 2000;

        public Domain(string domainName)
        {
            this.domainName = domainName;
            lastUpdated = DateTime.Now.AddMilliseconds(-(DOMAIN_DELAY_PERIOD + 1));
        }

        public HTMLPage Dequeue()
        {
            lastUpdated = DateTime.Now;
            return workQueue.Dequeue();
        }

        public HTMLPage Peek()
        {
            return workQueue.Peek();
        }

        public void Enqueue(HTMLPage page)
        {
            workQueue.Enqueue(page);
        }

        public bool IsNotAllowedToUpdate()
        {
            return (int)(DateTime.Now - lastUpdated).TotalMilliseconds < DOMAIN_DELAY_PERIOD;
        }
    }
}
