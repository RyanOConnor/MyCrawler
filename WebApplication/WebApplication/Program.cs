using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace WebApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            HTMLRecord page1 = new HTMLRecord("managerpage1111",
                                        "http://reddit.com/r/cscareerquestions",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page2 = new HTMLRecord("managerpage2222",
                                        "http://www.wired.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page3 = new HTMLRecord("managerpage3333",
                                        "http://www.cnn.com/tech",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page4 = new HTMLRecord("managerpage3333",
                                        "https://news.ycombinator.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page5 = new HTMLRecord("managerpage3333",
                                        "http://facebook.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page6 = new HTMLRecord("managerpage3333",
                                        "http://twitter.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page7 = new HTMLRecord("managerpage3333",
                                        "http://google.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page8 = new HTMLRecord("managerpage3333",
                                        "http://youtube.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page9 = new HTMLRecord("managerpage3333",
                                        "http://wordpress.org/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page10 = new HTMLRecord("managerpage3333",
                                        "http://adobe.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page11 = new HTMLRecord("managerpage3333",
                                        "http://blogspot.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page12 = new HTMLRecord("managerpage3333",
                                        "http://wikipedia.org/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page13 = new HTMLRecord("managerpage1111",
                                        "http://linkedin.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page14 = new HTMLRecord("managerpage2222",
                                        "http://wordpress.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page15 = new HTMLRecord("managerpage3333",
                                        "http://yahoo.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page16 = new HTMLRecord("managerpage3333",
                                        "http://amazon.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page17 = new HTMLRecord("managerpage3333",
                                        "http://flickr.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page18 = new HTMLRecord("managerpage3333",
                                        "http://pinterest.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page19 = new HTMLRecord("managerpage3333",
                                        "http://tumblr.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            //HTMLRecord page20 = new HTMLRecord("managerpage3333",
            //                            "http://w3.org/",
            //                            DateTime.Now,
            //                            new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page21 = new HTMLRecord("managerpage3333",
                                        "http://apple.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page22 = new HTMLRecord("managerpage3333",
                                        "http://myspace.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page23 = new HTMLRecord("managerpage3333",
                                        "http://vimeo.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page24 = new HTMLRecord("managerpage3333",
                                        "http://microsoft.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLRecord page25 = new HTMLRecord("managerpage3333",
                                        "http://digg.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });

            CrawlerManager.startCrawlerServer();

            Console.WriteLine("Hit enter to load work queue...");
            Console.ReadLine();

            CrawlerManager.distributeWorkAmongstCrawlers(page1);
            CrawlerManager.distributeWorkAmongstCrawlers(page2);
            CrawlerManager.distributeWorkAmongstCrawlers(page3);
            CrawlerManager.distributeWorkAmongstCrawlers(page4);
            CrawlerManager.distributeWorkAmongstCrawlers(page5);
            CrawlerManager.distributeWorkAmongstCrawlers(page6);
            CrawlerManager.distributeWorkAmongstCrawlers(page7);
            CrawlerManager.distributeWorkAmongstCrawlers(page8);
            CrawlerManager.distributeWorkAmongstCrawlers(page9);
            CrawlerManager.distributeWorkAmongstCrawlers(page10);
            CrawlerManager.distributeWorkAmongstCrawlers(page11);
            CrawlerManager.distributeWorkAmongstCrawlers(page12);
            CrawlerManager.distributeWorkAmongstCrawlers(page13);
            CrawlerManager.distributeWorkAmongstCrawlers(page14);
            CrawlerManager.distributeWorkAmongstCrawlers(page15);
            //crawlermanager.distributeWorkAmongstCrawlers(page16);
            CrawlerManager.distributeWorkAmongstCrawlers(page17);
            CrawlerManager.distributeWorkAmongstCrawlers(page18);
            CrawlerManager.distributeWorkAmongstCrawlers(page19);
            //CrawlerManager.distributeWorkAmongstCrawlers(page20);
            CrawlerManager.distributeWorkAmongstCrawlers(page21);
            CrawlerManager.distributeWorkAmongstCrawlers(page22);
            CrawlerManager.distributeWorkAmongstCrawlers(page23);
            CrawlerManager.distributeWorkAmongstCrawlers(page24);
            CrawlerManager.distributeWorkAmongstCrawlers(page25);

            Console.WriteLine("Distributed work to crawlers....");
            Console.ReadLine();
        }
    }
}
