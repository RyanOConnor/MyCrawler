using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MongoDB.Bson;
using System.Net;
using MongoDB.Driver;

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

            Database.Instance.Start();

            Thread dataThread = new Thread(DataManager.Instance.Start);
            dataThread.Start();

            string ipAd = "192.168.1.132";
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(ipAd), 11000);
            CrawlerManager.Instance.AllowCrawlerIP(endpoint);
            Thread crawlerThread = new Thread(() => CrawlerManager.Instance.StartListener(ipAd));
            crawlerThread.Start();

            UserManager.Start();


            CreateLargeAmountOfUsers();


        }

        public static void CreateLargeAmountOfUsers()
        {
            const int NumberOfUsers = 999;
            string baseName = "user";
            string basePassword = "password";

            for(int i = 0; i < NumberOfUsers; i++)
            {
                string userName = baseName + i.ToString();
                string password = basePassword + i.ToString();
                UserManager.CreateUser(userName, Encoding.UTF8.GetBytes(password));
            }

            MongoCursor<User> userRecords = UserManager.UserCollection.FindAll();
            List<NewRecord> testRecords = GenerateUserInput();

            foreach (User user in userRecords)
            {
                foreach (NewRecord record in testRecords)
                {
                    UserManager.AddLinkToUser(user.Id, user.UserName, record);
                }
            }
        }

        public static void CreateTestUsers()
        {
            const int RecordsPerUser = 3;
            string baseName = "user";
            string basePassword = "password";

            List<NewRecord> testRecords = GenerateUserInput();
            int numUsers = testRecords.Count / 3;

            for (int i = 0; i < numUsers; i++)
            {
                string userName = baseName + i.ToString();
                string password = basePassword + i.ToString();
                UserManager.CreateUser(userName, Encoding.UTF8.GetBytes(password));
            }

            List<List<NewRecord>> recordSegments = new List<List<NewRecord>>();
            int counter = 0;
            while(counter < testRecords.Count)
            {
                List<NewRecord> temp = new List<NewRecord>();
                for(int j = counter; j < counter + RecordsPerUser; j++)
                {
                    temp.Add(testRecords[j]);
                }
                recordSegments.Add(temp);
                counter += 3;
            }

            MongoCursor<User> userRecords = UserManager.UserCollection.FindAll();

            int iterator = 0;
            foreach(User user in userRecords)
            {
                foreach(NewRecord record in recordSegments[iterator])
                {
                    UserManager.AddLinkToUser(user.Id, user.UserName, record);
                }
                iterator++;
            }
        }

        public static List<NewRecord> GenerateUserInput()
        {
            NewRecord page1 = new NewRecord("http://www.reddit.com/",
                                        new List<string>() { "a", ".title", "href" },
                                        new List<string>() { "java", "c#", "javascript", "parallelism", "threading", "project", "big four", "facebook" });
            NewRecord page2 = new NewRecord("http://www.wired.com/",
                                        new List<string>() { "a", ".clearfix", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page3 = new NewRecord("http://www.cnn.com/tech",
                                        new List<string>() { "h3", ".cd__headline", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page4 = new NewRecord("https://news.ycombinator.com/",
                                        new List<string>() { "td", ".title", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page5 = new NewRecord("http://www.reddit.com/?count=50&after=t3_2y5kn8",
                                       new List<string>() { "a", ".title", "href" },
                                       new List<string>() { "java", "c#", "javascript", "parallelism", "threading", "project", "big four", "facebook" });
            NewRecord page6 = new NewRecord("http://www.reddit.com/?count=100&after=t3_2y5d88",
                                       new List<string>() { "a", ".title", "href" },
                                       new List<string>() { "java", "c#", "javascript", "parallelism", "threading", "project", "big four", "facebook" });
            NewRecord page8 = new NewRecord("http://youtube.com/",
                                        new List<string>() { "a", ".yt-uix-sessionlink", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page9 = new NewRecord("http://www.bbcamerica.com/",
                                        new List<string>() { "h3", ".entry-title", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page14 = new NewRecord("https://en.forums.wordpress.com/forum/themes",
                                        new List<string>() { "td", ".topictitle", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page15 = new NewRecord("http://yahoo.com/",
                                        new List<string>() { "h3", ".fw-b", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page16 = new NewRecord("http://www.amazon.com/s/ref=nb_sb_noss_1?url=search-alias%3Daps&field-keywords=stonewall+aioli",
                                        new List<string>() { "a", ".a-link-normal", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page17 = new NewRecord("https://www.flickr.com/20under20/",
                                        new List<string>() { "a", ".artist-name", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page18 = new NewRecord("http://pinterest.com/",
                                        new List<string>() { "a", ".pinImageWrapper", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page19 = new NewRecord("http://alexbeardstudio.tumblr.com/post/112441953420/close-up-on-the-new-fish-painting-the-gestural",
                                        new List<string>() { "a", ".avatar_frame", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page21 = new NewRecord("https://discussions.apple.com/community/apple_pay/using_apple_pay_in_stores",
                                        new List<string>() { "td", ".jive-table-cell-title", "a", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page22 = new NewRecord("http://myspace.com/",
                                        new List<string>() { "a", ".link", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page23 = new NewRecord("http://vimeo.com/",
                                        new List<string>() { "a", ".responsive_border_lg", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });
            NewRecord page25 = new NewRecord("http://digg.com/",
                                        new List<string>() { "a", ".story-title-link", "href" },
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });

            List<NewRecord> testRecords = new List<NewRecord> { page1, page2, page3, page4, page5, page6, page8,
                                                                page9, page14, page15, page16, page17, page18,
                                                                page19, page21, page22, page23, page25};

            return testRecords;
        }

        public static void GenerateHTMLRecords()
        {
            /*HTMLRecord page1 = new HTMLRecord("http://www.reddit.com/",
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
                                        new List<string>() { "tesla", "facebook", "snapchat", "virus" });*/

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
