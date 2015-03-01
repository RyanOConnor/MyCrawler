using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Threading;
using System.Net;

namespace WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.Expect100Continue = false;

            SocketServer.startClient();
            WebCrawler.start();


            /*HTMLPage page1 = new HTMLPage("WAAAIIIIIITTTT",
                                        "http://gizmodo.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLPage page2 = new HTMLPage("WAAAIIIIIITTTT",
                                        "http://www.arstechnica.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            HTMLPage page3 = new HTMLPage("WAAAIIIIIITTTT",
                                        "http://www.theverge.com/",
                                        DateTime.Now,
                                        new List<string>() { "asdfasdf234234", " 234324234" });
            page1.waitTime = 2000;
            page2.waitTime = 2000;
            page3.waitTime = 2000;
            WebCrawler.waitEnqueue(page1);
            WebCrawler.waitEnqueue(page2);
            WebCrawler.waitEnqueue(page3);*/

            /*WebCrawler.start();
            Console.WriteLine("Hit enter to connect to server...");
            Console.ReadLine();

            Server2.startClient();

            Console.WriteLine("Hit enter to initiate queue dump...");
            Console.ReadLine();

            Server2.allowNextTransmission();*/

            //Server.start();
            //Server.allowNextTransmission();
        }

        static void testJson()
        {
            /*HTMLRecord page = new HTMLPage("J345jkl345lkjswerwen4",
                                    new Uri("http://www.wired.com/"),
                                    DateTime.Now,
                                    new List<string> { "a", "class", "module-homepage" },
                                    new List<string> { "mars", "hololens", "tesla" },
                                    new List<string> { "J345jkl345lkjswerwen4", "J345jkl345lkjswerwen4", "J345jkl345lkjswerwen4" });

            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(HTMLPage));
            ser.WriteObject(stream, page);

            stream.Position = 0;
            StreamReader sr = new StreamReader(stream);
            Console.Write("JSON form of HTMLPage object: ");
            Console.WriteLine(sr.ReadToEnd());*/
        }
    }
}
