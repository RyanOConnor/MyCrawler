using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using HtmlAgilityPack;
using System.Net;

namespace WebCrawler
{
    public class ChildPage : HtmlRecord
    {
        public IReadOnlyCollection<ObjectId> jobIds { get; set; }
        public HtmlDocument htmlDoc { get; private set; }
        public event EventHandler<ChildPage> WebPageLoaded;
        public event EventHandler LoadError;

        public ChildPage(string url, DateTime timeStamp, IReadOnlyCollection<ObjectId> ids)
            : base(url, timeStamp)
        {
            jobIds = ids;
        }

        protected override void HandleResponse(HttpWebResponse response)
        {
            string type = string.Empty;
            try
            {
                type = response.Headers["content-type"];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                InvokeLoadErrorEvent();
            }
            finally
            {
                if (NotBinaryFileType(type))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string htmlString = DecompressHtml(response);

                        htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(htmlString);
                    }
                }

                InvokeLoadedEvent();
            }
        }

        public void InvokeLoadedEvent()
        {
            EventHandler<ChildPage> webPageLoaded = this.WebPageLoaded;
            if (webPageLoaded != null)
            {
                webPageLoaded(null, this);
            }
        }

        public void InvokeLoadErrorEvent()
        {
            EventHandler loadError = this.LoadError;
            if (loadError != null)
            {
                loadError(null, EventArgs.Empty);
            }
        }

        private bool NotBinaryFileType(string type)
        {
            if (type.Contains("text") || type.Contains("html"))
                return true;
            else
                return false;
        }
    }
}
