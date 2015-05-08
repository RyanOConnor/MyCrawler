using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.WebSockets;
using Microsoft.Owin;
using Newtonsoft.Json.Linq;
using Owin;

[assembly: OwinStartup(typeof(WebCrawlerNode.Startup))]

namespace WebCrawlerNode
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            Thread crawler = new Thread(StartCrawler);
            crawler.Start();
        }

        public void StartCrawler()
        {
            ServicePointManager.DefaultConnectionLimit = 1000;
            ServicePointManager.Expect100Continue = false;

            Uri appServerUri = new Uri("http://localhost:80/AndroidWebApi2/Handler.ashx");
            WebCrawler.Instance.Start(appServerUri);
        }
    }
}
