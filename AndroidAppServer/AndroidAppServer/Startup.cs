using System;
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
        }
    }
}
