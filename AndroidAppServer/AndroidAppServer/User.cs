using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace AndroidAppServer
{
    public class Password : Serializable
    {
        public byte[] passwordHash;
        public byte[] passwordSalt;

        public Password(byte[] hash, byte[] salt)
        {
            passwordHash = hash;
            passwordSalt = salt;
        }
    }

    public class LinkFeedParams
    {
        public ObjectId recordid { get; private set; }
        public ObjectId resultsid { get; private set; }
        public string url { get; private set; }
        public string htmlTags { get; private set; }
        public HashSet<string> keywords { get; set; }

        public LinkFeedParams(ObjectId recordid, ObjectId resultsid, string url,
                                string htmlTags, HashSet<string> keywords)
        {
            this.recordid = recordid;
            this.resultsid = resultsid;
            this.url = url;
            this.htmlTags = htmlTags;
            this.keywords = keywords;
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
                linkFeeds.Add(new Tuple<LinkFeedParams, List<UserLink>>(pair.Value,
                                        results.GetLinkFeedResults(pair.Value.keywords)));
            }
            return linkFeeds;
        }

        public LinkFeedParams ModifySubscription(ObjectId recordid, ObjectId resultsid, string htmlTags, HashSet<string> keywords)
        {
            LinkFeedParams parameters = null;
            if (links.ContainsKey(resultsid))
            {
                if (links[resultsid].htmlTags == htmlTags)
                {
                    links[resultsid].keywords = keywords;
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

        public bool ChangePassword(Password newPassword)
        {
            if (!previousPasswords.Any(pass => pass.passwordHash == newPassword.passwordHash))
            {
                previousPasswords.Add(this.password);
                password = newPassword;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
