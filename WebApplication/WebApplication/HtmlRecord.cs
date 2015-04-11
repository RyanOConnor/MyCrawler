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
        public int version = 0;

        public HtmlRecord(Uri domain)
        {
            this.url = domain.AbsoluteUri;
            this.domain = domain;
            timeStamp = DateTime.UtcNow;
            results = new Dictionary<ObjectId, HtmlResults>();
        }

        public LinkOwner AddResults(IHtmlResults newResults, ObjectId userid)
        {
            IEnumerable<HtmlResults> existingResults = GetExistingResults(newResults);

            if(existingResults.Count() == 0)
            {
                if(newResults is LinkFeed)
                {
                    return AddNewLinkFeed(newResults as ILinkFeed, userid);
                }
                else
                {
                    return AddNewTextUpdate(newResults as ITextUpdate, userid);
                }
            }
            else if (existingResults.Count() == 1)
            {
                HtmlResults existingResult = existingResults.First() as HtmlResults;

                if (newResults is LinkFeed)
                {
                    ILinkFeed newLinkFeed = newResults as ILinkFeed;
                    LinkFeed existingLinkFeed = existingResult as LinkFeed;
                    FeedOwner feedOwner = existingLinkFeed.AddFeedOwner(userid, newLinkFeed.keywords);
                    return feedOwner;
                }
                else
                {
                    ITextUpdate newTextUpdate = newResults as ITextUpdate;
                    TextUpdate textUpdate = existingResult as TextUpdate;
                    TextOwner textOwner = textUpdate.AddTextOwner(userid, newTextUpdate.previousText);
                    return textOwner;
                }
            }
            else
            {
                throw new Exception();
            }
        }

        private IEnumerable<HtmlResults> GetExistingResults(IHtmlResults newResults)
        {
            return results.Values.Where(val => val.GetType() == newResults.GetType() &&
                                               val.domain.AbsoluteUri == newResults.domain.AbsoluteUri &&
                                               val.htmlTags.SequenceEqual(newResults.htmlTags));
        }

        private FeedOwner AddNewLinkFeed(ILinkFeed userLinkFeed, ObjectId userid)
        {
            LinkFeed newLinkFeed = new LinkFeed(userLinkFeed);
            FeedOwner feedOwner = newLinkFeed.AddFeedOwner(userid, userLinkFeed.keywords);
            results.Add(newLinkFeed.id, newLinkFeed);
            return feedOwner;
        }

        private TextOwner AddNewTextUpdate(ITextUpdate userTextUpdate, ObjectId userid)
        {
            TextUpdate newTextUpdate = new TextUpdate(userTextUpdate);
            TextOwner textOwner = newTextUpdate.AddTextOwner(userid, userTextUpdate.previousText);
            results.Add(newTextUpdate.id, newTextUpdate);
            return textOwner;
        }

        public LinkOwner ModifyResults(IHtmlResults modifiedResults, LinkOwner owner)
        {
            RemoveResultsOwner(owner);
            return AddResults(modifiedResults, owner.userid);
        }

        public LinkOwner ModifyOwner(LinkOwner owner)
        {
            if(results.ContainsKey(owner.resultsid))
            {
                results[owner.resultsid].RemoveOwner(owner);
            }

            if(results[owner.resultsid] is LinkFeed)
            {
                LinkFeed linkFeed = results[owner.resultsid] as LinkFeed;
                FeedOwner feedOwner = owner as FeedOwner;
                return linkFeed.AddFeedOwner(feedOwner.userid, feedOwner.keywords);
            }
            else
            {
                TextUpdate textUpdate = results[owner.resultsid] as TextUpdate;
                TextOwner textOwner = owner as TextOwner;
                return textUpdate.AddTextOwner(textOwner.userid, textOwner.previousText);
            }
        }

        public void RemoveResultsOwner(LinkOwner owner)
        {
            if (results.ContainsKey(owner.resultsid))
            {
                bool ownersNull = results[owner.resultsid].RemoveOwner(owner);
                if (ownersNull)
                {
                    results.Remove(owner.resultsid);
                }
            }
        }
    }

    public interface IHtmlResults
    {
        ObjectId id { get; set; }
        Uri domain { get; set; }
        List<string> htmlTags { get; set; }
    }

    public interface ILinkFeed : IHtmlResults
    {
        HashSet<string> keywords { get; set; }
        FeedOwner RetrievePageRanking(ObjectId userid);
    }

    public interface ITextUpdate : IHtmlResults
    {
        string previousText { get; set; }
        string currentText { get; set; }
    }

    [BsonKnownTypes(typeof(HtmlResults), typeof(LinkOwner))]
    public abstract class UserResultsBase
    {
        public Uri domain { get; set; }
        public List<string> htmlTags { get; set; }

        public UserResultsBase(Uri domain, List<string> tags)
        {
            this.domain = domain;
            htmlTags = tags;
        }
    }

    [BsonKnownTypes(typeof(LinkFeed), typeof(TextUpdate))]
    public class HtmlResults : UserResultsBase, ISupportInitialize, Serializable
    {
        public ObjectId id { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ObjectId, LinkOwner> linkOwners { get; set; }

        public HtmlResults(string url, List<string> tags)
            :base(new Uri(url), tags)
        {
            id = ObjectId.GenerateNewId();
            linkOwners = new Dictionary<ObjectId, LinkOwner>();
        }

        public void EndInit()
        { }

        public void BeginInit()
        { }

        public LinkOwner RetrieveResults(ObjectId userid)
        {
            if(linkOwners.ContainsKey(userid))
            {
                return linkOwners[userid];
            }
            else
            {
                return null;
            }
        }

        public virtual bool RemoveOwner(LinkOwner linkOwner)
        {
            linkOwners.Remove(linkOwner.userid);

            if (linkOwners.Count() == 0)
                return true;
            else
                return false;
        }
    }

    public class LinkFeed : HtmlResults, ILinkFeed, Serializable
    {
        public HashSet<string> keywords { get; set; }
        [BsonDictionaryOptionsAttribute(DictionaryRepresentation.ArrayOfDocuments)]
        private Dictionary<string, List<ObjectId>> keywordOwners { get; set; }

        public LinkFeed(string url, List<string> htmlTags, HashSet<string> keywords)
            : base(url, htmlTags)
        {
            this.keywords = keywords;
            keywordOwners = new Dictionary<string, List<ObjectId>>();
        }

        public LinkFeed(ILinkFeed userLinkFeed)
            : base(userLinkFeed.domain.AbsoluteUri, userLinkFeed.htmlTags)
        {
            keywordOwners = new Dictionary<string, List<ObjectId>>();
        }

        public FeedOwner AddFeedOwner(ObjectId userid, HashSet<string> keywords)
        {
            if (!linkOwners.ContainsKey(userid))
            {
                FeedOwner feedOwner = new FeedOwner(this.domain, this.htmlTags, keywords, userid, id);
                linkOwners.Add(feedOwner.userid, feedOwner);
                foreach (string keyword in feedOwner.keywords)
                {
                    if (keywordOwners.ContainsKey(keyword))
                    {
                        keywordOwners[keyword].Add(feedOwner.userid);
                    }
                    else
                    {
                        keywordOwners[keyword] = new List<ObjectId>();
                        keywordOwners[keyword].Add(feedOwner.userid);
                    }
                }
                return feedOwner;
            }
            else
            {
                return null;
            }
        }

        public override bool RemoveOwner(LinkOwner linkOwner)
        {
            FeedOwner feedOwner = linkOwner as FeedOwner;
            foreach (string keyword in (linkOwners[linkOwner.userid] as FeedOwner).keywords)
            {
                keywordOwners[keyword].Remove(feedOwner.userid);
                if(keywordOwners[keyword].Count == 0)
                {
                    keywordOwners.Remove(keyword);
                }
            }
            linkOwners.Remove(linkOwner.userid);

            if (linkOwners.Count() == 0)
                return true;
            else
                return false;
        }

        public FeedOwner RetrievePageRanking(ObjectId userid)
        {
            return linkOwners[userid] as FeedOwner;
        }
    }

    //[BsonKnownTypes(typeof(TextUpdateResults))]
    public class TextUpdate : HtmlResults, ITextUpdate, Serializable
    {
        public string previousText { get; set; }
        public string currentText { get; set; }

        public TextUpdate(string url, List<string> htmlTags, string previousText)
            :base(url, htmlTags)
        {
            this.previousText = previousText;
        }

        public TextUpdate(ITextUpdate userTextUpdate)
            : base(userTextUpdate.domain.AbsoluteUri, userTextUpdate.htmlTags)
        {
            previousText = userTextUpdate.previousText;
        }

        public TextOwner AddTextOwner(ObjectId userid, string text)
        {
 	         if(!linkOwners.ContainsKey(userid))
             {
                 TextOwner textOwner = new TextOwner(this.domain, this.htmlTags, userid, id, text);
                 linkOwners.Add(textOwner.userid, textOwner);
                 return textOwner;
             }
             else
             {
                 return null;
             }
        }
    }

    [BsonKnownTypes(typeof(FeedOwner), typeof(TextOwner))]
    public class LinkOwner : UserResultsBase
    {
        public ObjectId resultsid { get; set; }
        public ObjectId userid { get; set; }
        public bool changeInContent { get; set; }

        public LinkOwner(Uri domain, List<string> tags, ObjectId user, ObjectId results)
            :base(domain, tags)
        {
            resultsid = results;
            userid = user;
        }
    }

    public class FeedOwner : LinkOwner
    {
        public HashSet<string> keywords { get; set; }
        public Dictionary<string, int> userPageRank { get; set; }

        public FeedOwner(Uri domain, List<string> tags, HashSet<string> keywords, ObjectId user, ObjectId results)
            : base(domain, tags, user, results)
        {
            this.keywords = keywords;
            userPageRank = new Dictionary<string, int>();
        }
    }

    public class TextOwner : LinkOwner
    {
        public string previousText { get; set; }

        public TextOwner(Uri domain, List<string> tags, ObjectId user, ObjectId results, string text)
            : base(domain, tags, user, results)
        {
            previousText = text;
        }
    }

    /*public class LinkFeedResults : LinkFeed, Serializable
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
    }*/


    /*public class TextUpdateResults : TextUpdate, Serializable
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
    }*/
}