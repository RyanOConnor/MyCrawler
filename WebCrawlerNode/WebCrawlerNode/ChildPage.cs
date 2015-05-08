using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MongoDB.Bson;
using System.Net;

namespace WebCrawlerNode
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
            string contentType = string.Empty;
            try
            {
                contentType = response.Headers["content-type"];
                if (NotBinaryFileType(contentType))
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                InvokeLoadErrorEvent();
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
            if (type == string.Empty)
                return false;
            else if (type.Contains("text") || type.Contains("html"))
                return true;
            else
                return false;
        }
    }
}
