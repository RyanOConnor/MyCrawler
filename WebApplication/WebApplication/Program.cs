using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MongoDB.Bson;
using System.Net;

namespace WebApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach(string ipAddress in args)
            {
                IPAddress ip = IPAddress.Parse(ipAddress);
                IPEndPoint ep = new IPEndPoint(ip, 11000);
                CrawlerManager.Instance.AllowCrawlerIP(ep);
            }

            Thread dataThread = new Thread(DataManager.Instance.Start);
            dataThread.Start();

            string ipAd = "192.168.1.132";
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(ipAd), 11000);
            CrawlerManager.Instance.AllowCrawlerIP(endpoint);
            Thread crawlerThread = new Thread(() => CrawlerManager.Instance.StartListener(ipAd));
            crawlerThread.Start();

            HTMLRecord page1 = new HTMLRecord("http://www.reddit.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".title", "href" },
                                        new List<string>() { "java", "c#", "javascript", "parallelism", "threading", "project", "big four", "facebook" });
            HTMLRecord page2 = new HTMLRecord("http://www.wired.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".clearfix", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page3 = new HTMLRecord("http://www.cnn.com/tech",
                                        DateTime.Now,
                                        new List<string>() { "h3", ".cd__headline", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page4 = new HTMLRecord("https://news.ycombinator.com/",
                                        DateTime.Now,
                                        new List<string>() { "td", ".title", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page5 = new HTMLRecord("http://www.reddit.com/?count=50&after=t3_2y5kn8",
                                       DateTime.Now,
                                       new List<string>() { "a", ".title", "href" },
                                       new List<string>() { "java", "c#", "javascript", "parallelism", "threading", "project", "big four", "facebook" });
            HTMLRecord page6 = new HTMLRecord("http://www.reddit.com/?count=100&after=t3_2y5d88",
                                       DateTime.Now,
                                       new List<string>() { "a", ".title", "href" },
                                       new List<string>() { "java", "c#", "javascript", "parallelism", "threading", "project", "big four", "facebook" });
            HTMLRecord page8 = new HTMLRecord("http://youtube.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".yt-uix-sessionlink", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page9 = new HTMLRecord("http://www.bbcamerica.com/",
                                        DateTime.Now,
                                        new List<string>() { "h3", ".entry-title", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page12 = new HTMLRecord("http://meta.wikimedia.org/wiki/List_of_Wikipedias",
                                        DateTime.Now,
                                        new List<string>() { "a", ".extiw", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page14 = new HTMLRecord("https://en.forums.wordpress.com/forum/themes",
                                        DateTime.Now,
                                        new List<string>() { "td", ".topictitle", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page15 = new HTMLRecord("http://yahoo.com/",
                                        DateTime.Now,
                                        new List<string>() { "h3", ".fw-b", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page16 = new HTMLRecord("http://www.amazon.com/s/ref=nb_sb_noss_1?url=search-alias%3Daps&field-keywords=stonewall+aioli",
                                        DateTime.Now,
                                        new List<string>() { "a", ".a-link-normal", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page17 = new HTMLRecord("https://www.flickr.com/20under20/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".artist-name", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page18 = new HTMLRecord("http://pinterest.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".pinImageWrapper", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page19 = new HTMLRecord("http://alexbeardstudio.tumblr.com/post/112441953420/close-up-on-the-new-fish-painting-the-gestural",
                                        DateTime.Now,
                                        new List<string>() { "a", ".avatar_frame", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page21 = new HTMLRecord("https://discussions.apple.com/community/apple_pay/using_apple_pay_in_stores",
                                        DateTime.Now,
                                        new List<string>() { "td", ".jive-table-cell-title", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page22 = new HTMLRecord("http://myspace.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".link", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page23 = new HTMLRecord("http://vimeo.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".responsive_border_lg", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page25 = new HTMLRecord("http://digg.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".story-title-link", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });

            /*CrawlerManager.Instance.DistributeWork(page8);
            CrawlerManager.Instance.DistributeWork(page9);
            //crawlerManager.distributeWorkAmongstCrawlers(page12);
            CrawlerManager.Instance.DistributeWork(page14);
            CrawlerManager.Instance.DistributeWork(page15);
            //Thread.Sleep(2000);
            CrawlerManager.Instance.DistributeWork(page17);
            CrawlerManager.Instance.DistributeWork(page18);
            CrawlerManager.Instance.DistributeWork(page19);
            //Thread.Sleep(2000);
            CrawlerManager.Instance.DistributeWork(page21);
            CrawlerManager.Instance.DistributeWork(page22);
            CrawlerManager.Instance.DistributeWork(page23);
            //Thread.Sleep(5000);
            CrawlerManager.Instance.DistributeWork(page25);
            CrawlerManager.Instance.DistributeWork(page1);
            CrawlerManager.Instance.DistributeWork(page2);
            CrawlerManager.Instance.DistributeWork(page3);
            //Thread.Sleep(2000);
            CrawlerManager.Instance.DistributeWork(page4);
            CrawlerManager.Instance.DistributeWork(page5);
            CrawlerManager.Instance.DistributeWork(page6);*/

            Console.WriteLine("Distributed work to crawlers....");
        }
    }
}
