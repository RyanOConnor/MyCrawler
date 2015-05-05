using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.ComponentModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace AndroidAppServer
{
    public interface IHtmlRecord
    {
        ObjectId recordid { get; set; }
        string url { get; set; }
        Uri domain { get; set; }
        IHtmlResults AddResults(string htmlTags, ObjectId userid);
        IHtmlResults GetHtmlResults(ObjectId resultsid);
        void RemoveResults(ObjectId resultsid, ObjectId userid);
    }

    public interface IHtmlResults
    {
        ObjectId resultsid { get; set; }
        string htmlTags { get; set; }
        void AddResultsOwner(ObjectId userid);
        List<UserLink> GetLinkFeedResults(HashSet<string> keywords);
    }

    public interface ILink
    {
        string url { get; set; }
        string innerText { get; set; }
        int GetPageRank(HashSet<string> keywords);
    }

    public class UserLink
    {
        string url { get; set; }
        string innerText { get; set; }
        int pageRank { get; set; }

        public UserLink(string url, string innerText, int pageRank)
        {
            this.url = url;
            this.innerText = innerText;
            this.pageRank = pageRank;
        }
    }

    public class HtmlRecord : IHtmlRecord
    {
        [BsonId]
        public ObjectId recordid { get; set; }
        public string url { get; set; }
        public Uri domain { get; set; }
        public DateTime timeStamp { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ObjectId, HtmlResults> results { get; set; }
        public HttpStatusCode serverResponse { get; set; }

        public HtmlRecord(Uri domain)
        {
            this.url = domain.AbsoluteUri;
            this.domain = domain;
            timeStamp = DateTime.UtcNow;
            results = new Dictionary<ObjectId, HtmlResults>();
            recordid = ObjectId.GenerateNewId();
        }

        public IHtmlResults GetHtmlResults(ObjectId resultsid)
        {
            return results[resultsid] as IHtmlResults;
        }

        public IHtmlResults AddResults(string htmlTags, ObjectId userid)
        {
            IEnumerable<IHtmlResults> existingResults = results.Values.Where(val => val.htmlTags == htmlTags);

            if (existingResults.Count() == 0)
            {
                HtmlResults newResults = new HtmlResults(htmlTags);
                newResults.AddResultsOwner(userid);
                results.Add(newResults.resultsid, newResults);
                return newResults as IHtmlResults;
            }
            else if (existingResults.Count() == 1)
            {
                existingResults.First().AddResultsOwner(userid);
                return existingResults.First();
            }
            else
            {
                throw new Exception();
            }
        }

        public void RemoveResults(ObjectId resultsid, ObjectId userid)
        {
            HtmlResults htmlResults = results[resultsid];
            htmlResults.RemoveResultsOwner(userid);
            if (htmlResults.owners.Count == 0)
            {
                results.Remove(resultsid);
            }
        }
    }

    public class HtmlResults : IHtmlResults
    {
        [BsonId]
        public ObjectId resultsid { get; set; }
        public string htmlTags { get; set; }
        public List<ILink> links { get; set; }
        public HashSet<ObjectId> owners { get; set; }

        public HtmlResults(string htmlTags)
        {
            this.htmlTags = htmlTags;
            owners = new HashSet<ObjectId>();
            links = new List<ILink>();
            resultsid = ObjectId.GenerateNewId();
        }

        public void AddResultsOwner(ObjectId userid)
        {
            owners.Add(userid);
        }

        public void RemoveResultsOwner(ObjectId userid)
        {
            if (owners.Contains(userid))
            {
                owners.Remove(userid);
            }
        }

        public List<UserLink> GetLinkFeedResults(HashSet<string> keywords)
        {
            List<UserLink> userResults = new List<UserLink>();
            foreach (ILink link in links)
            {
                userResults.Add(new UserLink(link.url, link.innerText, link.GetPageRank(keywords)));
            }
            return userResults;
        }
    }

    public class Link : ILink
    {
        public string url { get; set; }
        public string innerText { get; set; }
        // add picture property?
        private Dictionary<string, int> wordCount { get; set; }

        public int GetPageRank(HashSet<string> keywords)
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