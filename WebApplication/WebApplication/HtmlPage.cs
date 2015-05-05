using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace WebApplication
{
    public interface IHtmlPage
    {
        ObjectId recordid { get; set; }
        string url { get; set; }
        Uri domain { get; set; }
        IHtmlResultss getHtmlResults(ObjectId resultsid);
    }

    public interface IHtmlResultss
    {
        ObjectId resultsid { get; set; }
        List<UserLink> getLinkFeedResults(HashSet<string> keywords);
    }

    public interface ILink
    {
        string url { get; set; }
        string innerText { get; set; }
        int getPageRank(HashSet<string> keywords);
    }

    public class UserLink : Link
    {
        int pageRank { get; set; }

        public UserLink(string url, string innerText, int pageRank)
        {
            this.url = url;
            this.innerText = innerText;
            this.pageRank = pageRank;
        }
    }

    public interface ITransferrable
    {
        List<HtmlResultss> getResultsFromDB(List<ObjectId> resultsids);
    }

    public class CrawlerObject : HtmlPage, ITransferrable
    {
        public List<HtmlResultss> results { get; set; }

        public List<HtmlResultss> getResultsFromDB(List<ObjectId> resultsids)
        {

            return new List<HtmlResultss>();
        }
    }

    public class HtmlPage : IHtmlPage
    {
        [BsonId]
        public ObjectId recordid { get; set; }
        public string url { get; set; }
        public Uri domain { get; set; }
        public DateTime timeStamp { get; set; }
        public List<ObjectId> resultsIds { get; set; }
        public HttpStatusCode serverResponse { get; set; }

        public HtmlPage(Uri domain)
        {
            this.url = domain.AbsoluteUri;
            this.domain = domain;
            timeStamp = DateTime.UtcNow;
            resultsIds = new List<ObjectId>();
        }

        public List<ObjectId> getResultsIds()
        {
            return resultsIds;
        }
    }

    public class HtmlResultss : IHtmlResultss
    {
        [BsonId]
        public ObjectId resultsid { get; set; }
        public string htmlTags { get; set; }
        public List<ILink> links { get; set; }

        public List<UserLink> getLinkFeedResults(HashSet<string> keywords)
        {
            List<UserLink> userResults = new List<UserLink>();
            foreach (ILink link in links)
            {
                userResults.Add(new UserLink(link.url, link.innerText, link.getPageRank(keywords)));
            }
            return userResults;
        }
    }

    public class Link : ILink
    {
        public string url { get; set; }
        public string innerText { get; set; }
        private Dictionary<string, int> wordCount { get; set; }

        public int getPageRank(HashSet<string> keywords)
        {
            int pageRank = 0;
            foreach (string keyword in keywords)
            {
                if (wordCount.ContainsKey(keyword))
                    pageRank += wordCount[keyword];
            }
            return pageRank;
        }
    }
}
