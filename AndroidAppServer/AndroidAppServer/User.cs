using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using System.Diagnostics;

namespace AndroidAppServer
{
    public class Password : Serializable
    {
        public byte[] hash;
        public byte[] salt;

        public Password(byte[] hash, byte[] salt)
        {
            this.hash = hash;
            this.salt = salt;
        }
    }

    public class LinkFeedParams : Serializable
    {
        public ObjectId recordid { get; private set; }
        public ObjectId resultsid { get; private set; }
        public string url { get; private set; }
        public string htmlTags { get; private set; }
        public HashSet<string> keywords { get; private set; }
        private DateTime lastUpdated { get; set; }

        public LinkFeedParams(ObjectId recordid, ObjectId resultsid, string url,
                                string htmlTags, HashSet<string> keywords)
        {
            this.recordid = recordid;
            this.resultsid = resultsid;
            this.url = url;
            this.htmlTags = htmlTags;
            this.keywords = keywords;
        }

        public void ReplaceKeywords(HashSet<string> newKeywords)
        {
            keywords = newKeywords;
        }

        public void UpdateTimeStamp()
        {
            lastUpdated = DateTime.UtcNow;
        }
    }

    public class User : Serializable
    {
        [BsonId]
        public ObjectId id { get; private set; }
        public string username { get; private set; }
        public Password password { get; set; }
        public List<Password> previousPasswords { get; set; }
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ObjectId, LinkFeedParams> links { get; private set; }

        public User(string newUserName, Password newPassword)
        {
            username = newUserName;
            password = newPassword;
            links = new Dictionary<ObjectId, LinkFeedParams>();
            previousPasswords = new List<Password>();
        }

        public LinkFeedParams AddLinkFeed(string url, string htmlTags, HashSet<string> keywords)
        {
            if (!links.Any(pair => pair.Value.url == url &&
                                   pair.Value.htmlTags == htmlTags))
            {
                IHtmlRecord record = DataManager.Instance.CreateHtmlRecord(new Uri(url));
                IHtmlResults results = record.AddResults(htmlTags, id);
                LinkFeedParams parameters = new LinkFeedParams(record.recordid, results.resultsid,
                                                               record.domain.AbsoluteUri, results.htmlTags, keywords);
                DataManager.Instance.SaveHtmlRecord(record);
                links.Add(results.resultsid, parameters);

                return parameters;
            }
            else
            {
                throw new Exception();
            }
        }

        public List<Tuple<LinkFeedParams, List<UserLink>>> GetAllLinkFeeds()
        {
            List<Tuple<LinkFeedParams, List<UserLink>>> linkFeeds = new List<Tuple<LinkFeedParams, List<UserLink>>>();

            foreach (KeyValuePair<ObjectId, LinkFeedParams> pair in links)
            {
                IHtmlRecord record = DataManager.Instance.GetHtmlRecord(pair.Value.recordid);
                IHtmlResults results = record.GetHtmlResults(pair.Value.resultsid);
                List<UserLink> userLinks;
                if (pair.Value.keywords != null)
                    userLinks = results.GetLinkFeedResults(pair.Value.keywords);
                else
                    userLinks = results.GetLinkFeedResults(new HashSet<string>());

                linkFeeds.Add(new Tuple<LinkFeedParams, List<UserLink>>(pair.Value, userLinks));
                pair.Value.UpdateTimeStamp();
            }
            return linkFeeds;
        }

        public LinkFeedParams ModifySubscription(ObjectId recordid, ObjectId resultsid, 
                                                 string htmlTags, HashSet<string> keywords)
        {
            LinkFeedParams parameters = null;
            if (links.ContainsKey(resultsid))
            {
                if (links[resultsid].htmlTags == htmlTags)
                {
                    links[resultsid].ReplaceKeywords(keywords);
                    parameters = links[resultsid];
                }
                else
                {
                    IHtmlRecord record = DataManager.Instance.GetHtmlRecord(recordid);
                    record.RemoveResults(resultsid, id);
                    IHtmlResults results = record.AddResults(htmlTags, id);
                    parameters = new LinkFeedParams(record.recordid, results.resultsid, 
                                                    record.domain.AbsoluteUri, results.htmlTags, keywords);
                    DataManager.Instance.SaveHtmlRecord(record);
                    links.Add(results.resultsid, parameters);
                }
            }
            return parameters;
        }

        public void RemoveLink(ObjectId resultsid)
        {
            if (links.ContainsKey(resultsid))
            {
                ObjectId recordid = links[resultsid].recordid;
                IHtmlRecord record = DataManager.Instance.GetHtmlRecord(recordid);
                record.RemoveResults(resultsid, id);
                links.Remove(resultsid);
                DataManager.Instance.SaveHtmlRecord(record);
            }
        }

        public void RemoveAllLinks()
        {
            foreach (LinkFeedParams linkParams in links.Values)
            {
                IHtmlRecord record = DataManager.Instance.GetHtmlRecord(linkParams.recordid);
                record.RemoveResults(linkParams.resultsid, id);
            }
        }

        public bool ChangePassword(byte[] newPassword)
        {
            byte[] saltedHash = Authorize.GenerateSaltedHash(newPassword, password.salt);
            if(Authorize.IsValidHash(saltedHash, password.hash))
            {
                return false;
            }
            foreach (Password prevPass in previousPasswords)
            {
                saltedHash = Authorize.GenerateSaltedHash(newPassword, prevPass.salt);
                if (Authorize.IsValidHash(saltedHash, prevPass.hash))
                {
                    return false;
                }
            }
            byte[] salt = Authorize.GenerateSalt();
            saltedHash = Authorize.GenerateSaltedHash(newPassword, salt);
            Password newPass = new Password(saltedHash, salt);
            previousPasswords.Add(this.password);
            this.password = newPass;

            return true;
        }
    }
}
