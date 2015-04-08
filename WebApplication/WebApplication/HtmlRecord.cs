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
    public class HtmlRecord : Serializable
    {
        [BsonId]
        public ObjectId id { get; set; }
        public string url { get; set; }
        public Uri domain { get; set; }
        public DateTime timeStamp { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ObjectId, HtmlResults> results { get; set; }
        public HttpStatusCode serverResponse { get; set; }

        public HtmlRecord(Uri domain)
        {
            this.url = domain.OriginalString;
            this.domain = domain;
            timeStamp = DateTime.UtcNow;
            results = new Dictionary<ObjectId, HtmlResults>();
        }

        public LinkFeedResults AddLinkFeed(LinkFeed linkFeed, ObjectId userId)
        {
            LinkFeedResults feedResults = null;
            IEnumerable<HtmlResults> existingResults = results.Values.Where(val => val.GetType() == typeof(LinkFeedResults) &&
                                                                                   val.htmlTags.SequenceEqual(linkFeed.htmlTags));

            if (existingResults.Count() == 0)
            {
                feedResults = new LinkFeedResults(linkFeed);
                feedResults.AddResultsOwner(userId);
                results.Add(feedResults.jobId, feedResults);

                return feedResults;
            }
            else if (existingResults.Count() == 1)
            {
                feedResults = existingResults.First() as LinkFeedResults;

                if (feedResults.keywords.SequenceEqual(linkFeed.keywords))
                {
                    feedResults.AddResultsOwner(userId);
                    return feedResults;
                }
                else
                {
                    feedResults = linkFeed as LinkFeedResults;
                    feedResults.AddResultsOwner(userId);
                    return feedResults;
                }
            }
            else
            {
                throw new Exception();
            }
        }

        public TextUpdateResults AddTextUpdate(TextUpdate textUpdate, ObjectId userId)
        {
            TextUpdateResults textResults = null;
            IEnumerable<HtmlResults> existingResults = results.Values.Where(val => val.GetType() == typeof(TextUpdateResults) &&
                                                                                   val.htmlTags.SequenceEqual(textUpdate.htmlTags));

            if (existingResults.Count() == 0)
            {
                textResults = textUpdate as TextUpdateResults;
                textResults.AddResultsOwner(userId);
                results.Add(textResults.jobId, textResults);

                return textResults;
            }
            else if (existingResults.Count() == 1)
            {
                textResults = existingResults.First() as TextUpdateResults;

                if (textResults.previousText == textUpdate.previousText)
                {
                    textResults.AddResultsOwner(userId);
                    return textResults;
                }
                else
                {
                    textResults = textUpdate as TextUpdateResults;
                    textResults.AddResultsOwner(userId);
                    return textResults;
                }
            }
            else
            {
                throw new Exception();
            }
        }
    }

    [BsonKnownTypes(typeof(LinkFeed), typeof(TextUpdate))]
    public class HtmlResults : Serializable
    {
        [BsonId]
        public ObjectId jobId = ObjectId.GenerateNewId();
        public Uri domain { get; set; }
        public List<string> htmlTags { get; set; }
        public bool changeInContent { get; set; }

        public HtmlResults(string url, List<string> tags)
        {
            domain = new Uri(url);
            htmlTags = tags;
        }
    }

    [BsonKnownTypes(typeof(LinkFeedResults))]
    public class LinkFeed : HtmlResults, Serializable
    {
        public List<string> keywords { get; set; }

        public LinkFeed(string url, List<string> tags, List<string> keywords)
            : base(url, tags)
        {
            this.keywords = keywords;
        }
    }

    public class LinkFeedResults : LinkFeed, Serializable
    {
        public List<ObjectId> users { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, int> rankedResults { get; set; }

        public LinkFeedResults(LinkFeed linkFeed)
            : base(linkFeed.domain.OriginalString, linkFeed.htmlTags, linkFeed.keywords)
        {
            rankedResults = new Dictionary<string, int>();
            users = new List<ObjectId>();
        }

        public ObjectId AddResultsOwner(ObjectId userid)
        {
            users.Add(userid);
            return jobId;
        }

        public void RemoveOwner(ObjectId userId)
        {
            users.Remove(userId);
        }
    }

    [BsonKnownTypes(typeof(TextUpdateResults))]
    public class TextUpdate : HtmlResults, Serializable
    {
        public string previousText { get; set; }
        public string currentText { get; set; }

        public TextUpdate(string url, List<string> tags, string innerText)
            : base(url, tags)
        {
            previousText = innerText;
        }
    }

    public class TextUpdateResults : TextUpdate, Serializable
    {
        public List<ObjectId> users { get; set; }

        public TextUpdateResults(string url, List<string> tags, string innerText)
            : base(url, tags, innerText)
        { }

        public ObjectId AddResultsOwner(ObjectId userid)
        {
            users.Add(userid);
            return jobId;
        }

        public void RemoveOwner(ObjectId userId)
        {
            users.Remove(userId);
        }
    }
}
