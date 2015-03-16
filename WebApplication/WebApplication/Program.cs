using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MongoDB.Bson;

namespace WebApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread thread = new Thread(new ThreadStart(SocketServer.startListener));
            thread.Start();

            HTMLRecord page1 = new HTMLRecord(new ObjectId(),
                                        "http://www.reddit.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".title", "href" },
                                        new List<string>() { "java", "c#", "javascript", "parallelism", "threading", "project", "big four", "facebook" });
            HTMLRecord page2 = new HTMLRecord(new ObjectId(),
                                        "http://www.wired.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".clearfix", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page3 = new HTMLRecord(new ObjectId(),
                                        "http://www.cnn.com/tech",
                                        DateTime.Now,
                                        new List<string>() { "h3", ".cd__headline", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page4 = new HTMLRecord(new ObjectId(),
                                        "https://news.ycombinator.com/",
                                        DateTime.Now,
                                        new List<string>() { "td", ".title", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page5 = new HTMLRecord(new ObjectId(),
                                       "http://www.reddit.com/?count=50&after=t3_2y5kn8",
                                       DateTime.Now,
                                       new List<string>() { "a", ".title", "href" },
                                       new List<string>() { "java", "c#", "javascript", "parallelism", "threading", "project", "big four", "facebook" });
            HTMLRecord page6 = new HTMLRecord(new ObjectId(),
                                       "http://www.reddit.com/?count=100&after=t3_2y5d88",
                                       DateTime.Now,
                                       new List<string>() { "a", ".title", "href" },
                                       new List<string>() { "java", "c#", "javascript", "parallelism", "threading", "project", "big four", "facebook" });
            HTMLRecord page8 = new HTMLRecord(new ObjectId(),
                                        "http://youtube.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".yt-uix-sessionlink", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page9 = new HTMLRecord(new ObjectId(),
                                        "http://www.bbcamerica.com/",
                                        DateTime.Now,
                                        new List<string>() { "h3", ".entry-title", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page12 = new HTMLRecord(new ObjectId(),
                                        "http://meta.wikimedia.org/wiki/List_of_Wikipedias",
                                        DateTime.Now,
                                        new List<string>() { "a", ".extiw", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page14 = new HTMLRecord(new ObjectId(),
                                        "https://en.forums.wordpress.com/forum/themes",
                                        DateTime.Now,
                                        new List<string>() { "td", ".topictitle", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page15 = new HTMLRecord(new ObjectId(),
                                        "http://yahoo.com/",
                                        DateTime.Now,
                                        new List<string>() { "h3", ".fw-b", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page16 = new HTMLRecord(new ObjectId(),
                                        "http://www.amazon.com/s/ref=nb_sb_noss_1?url=search-alias%3Daps&field-keywords=stonewall+aioli",
                                        DateTime.Now,
                                        new List<string>() { "a", ".a-link-normal", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page17 = new HTMLRecord(new ObjectId(),
                                        "https://www.flickr.com/20under20/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".artist-name", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page18 = new HTMLRecord(new ObjectId(),
                                        "http://pinterest.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".pinImageWrapper", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page19 = new HTMLRecord(new ObjectId(),
                                        "http://alexbeardstudio.tumblr.com/post/112441953420/close-up-on-the-new-fish-painting-the-gestural",
                                        DateTime.Now,
                                        new List<string>() { "a", ".avatar_frame", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page21 = new HTMLRecord(new ObjectId(),
                                        "https://discussions.apple.com/community/apple_pay/using_apple_pay_in_stores",
                                        DateTime.Now,
                                        new List<string>() { "td", ".jive-table-cell-title", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page22 = new HTMLRecord(new ObjectId(),
                                        "http://myspace.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".link", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page23 = new HTMLRecord(new ObjectId(),
                                        "http://vimeo.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".responsive_border_lg", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            HTMLRecord page25 = new HTMLRecord(new ObjectId(),
                                        "http://digg.com/",
                                        DateTime.Now,
                                        new List<string>() { "a", ".story-title-link", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });

            //Console.WriteLine("Hit enter to load work queue...");
            //Console.ReadLine();

            CrawlerManager.distributeWorkAmongstCrawlers(page8);
            CrawlerManager.distributeWorkAmongstCrawlers(page9);
            //CrawlerManager.distributeWorkAmongstCrawlers(page12);
            CrawlerManager.distributeWorkAmongstCrawlers(page14);
            CrawlerManager.distributeWorkAmongstCrawlers(page15);
            //Thread.Sleep(2000);
            CrawlerManager.distributeWorkAmongstCrawlers(page17);
            CrawlerManager.distributeWorkAmongstCrawlers(page18);
            CrawlerManager.distributeWorkAmongstCrawlers(page19);
            //Thread.Sleep(2000);
            CrawlerManager.distributeWorkAmongstCrawlers(page21);
            CrawlerManager.distributeWorkAmongstCrawlers(page22);
            CrawlerManager.distributeWorkAmongstCrawlers(page23);
            //Thread.Sleep(5000);
            CrawlerManager.distributeWorkAmongstCrawlers(page25);
            CrawlerManager.distributeWorkAmongstCrawlers(page1);
            CrawlerManager.distributeWorkAmongstCrawlers(page2);
            CrawlerManager.distributeWorkAmongstCrawlers(page3);
            //Thread.Sleep(2000);
            CrawlerManager.distributeWorkAmongstCrawlers(page4);
            CrawlerManager.distributeWorkAmongstCrawlers(page5);
            CrawlerManager.distributeWorkAmongstCrawlers(page6);

            Console.WriteLine("Distributed work to crawlers....");
            //Console.ReadLine();
        }
    }
}
