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
            this.service.Name = "AndroidClientApi";
            this.service.Description = "JSON API for CloudCrawler Android Client";
            InterfaceConfiguration IConfig = new InterfaceConfiguration("Android_Client_RestAPI", typeof(IServiceApi), typeof(ServiceApi));
            this.service.Interfaces.Add(IConfig);
        }
    }
}