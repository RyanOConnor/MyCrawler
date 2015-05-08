using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JsonServices;
using JsonServices.Web;

namespace WebCrawlerNode
{
    /// <summary>
    /// Summary description for Handler
    /// </summary>
    public class Handler : JsonHandler
    {
        public Handler()
        {
            this.service.Name = "CrawlerNodeApi";
            this.service.Description = "JSON API for CloudCrawler worker node implementation";
            InterfaceConfiguration IConfig = new InterfaceConfiguration("Crawler_Node_RestAPI", typeof(IServiceApi), typeof(ServiceApi));
            this.service.Interfaces.Add(IConfig);
        }
    }
}