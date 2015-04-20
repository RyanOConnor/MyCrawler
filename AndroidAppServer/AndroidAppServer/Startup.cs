﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Text;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(AndroidAppServer.Startup))]
namespace AndroidAppServer
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            Thread webApp = new Thread(Start);
            webApp.Start();
        }

        public void Start()
        {
            Database.Instance.Start();
            DataManager.Instance.Start();
            //IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("192.168.1.132"), 11000);
            //CrawlerManager.Instance.AllowCrawlerIP(endPoint);
            //CrawlerManager.Instance.StartListening(endPoint.Address);

            //CreateUser();
        }

        public void CreateUser()
        {
            IServiceApi api = new ServiceApi();
            //api.CreateNewAccount(DateTime.Now.ToString(), "success?");
            
        }
    }
}