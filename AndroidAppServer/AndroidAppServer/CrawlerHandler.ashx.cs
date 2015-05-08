using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JsonServices;
using JsonServices.Web;

namespace AndroidAppServer
{
    /// <summary>
    /// Summary description for CrawlerHandler
    /// </summary>
    public class CrawlerHandler : JsonHandler
    {
        public CrawlerHandler()
        {
            this.service.Name = "WebCrawlerApi";
            this.service.Description = "JSON API for CloudCrawler Web Crawler";
            InterfaceConfiguration IConfig = new InterfaceConfiguration("Crawler_Manager_RestAPI", 
                typeof(ICrawlerApi), typeof(CrawlerApi));
            this.service.Interfaces.Add(IConfig);
        }
    }
}