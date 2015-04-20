using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JsonServices;
using JsonServices.Web;

namespace AndroidAppServer
{
    /// <summary>
    /// Summary description for Handler
    /// </summary>
    public class Handler : JsonHandler
    {
        public Handler()
        {
            this.service.Name = "AndroidWebApi";
            this.service.Description = "JSON API for CloudCrawler Android Client";
            InterfaceConfiguration IConfig = new InterfaceConfiguration("RestAPI", typeof(IServiceApi), typeof(ServiceApi));
            this.service.Interfaces.Add(IConfig);
        }
        
    }
}