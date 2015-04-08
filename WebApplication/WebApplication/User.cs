using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace WebApplication
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

    public class User : Serializable
    {
        [BsonId]
        public ObjectId id { get; private set; }
        public string username { get; private set; }
        public Password password { get; set; }
        public List<Password> previousPasswords { get; set; }
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ObjectId, HtmlResults> links { get; private set; }

        public User(string newUserName, Password newPassword)
        {
            username = newUserName;
            password = newPassword;
            links = new Dictionary<ObjectId, HtmlResults>();
        }

        public ObjectId AddLinkFeed(string url, List<string> htmlTags, List<string> keywords)
        {
            if (!links.Any(pair => pair.Value.domain.OriginalString == url &&
                                   pair.Value.htmlTags.SequenceEqual(htmlTags)))
            {
                LinkFeed linkFeed = new LinkFeed(url, htmlTags, keywords);
                LinkFeedResults feedResults = DataManager.Instance.CreateEntry(linkFeed, id) as LinkFeedResults;
                links.Add(feedResults.jobId, linkFeed);
                return feedResults.jobId;
            }
            else
            {
                throw new Exception();
            }
        }

        public ObjectId AddTextUpdate(string url, List<string> htmlTags, string innerText)
        {
            if (!links.Any(pair => pair.Value.domain.OriginalString == url &&
                                  pair.Value.htmlTags.SequenceEqual(htmlTags)))
            {
                TextUpdate textUpdate = new TextUpdate(url, htmlTags, innerText);
                TextUpdateResults textResults = DataManager.Instance.CreateEntry(textUpdate, id) as TextUpdateResults;
                links.Add(textResults.jobId, textUpdate);
                return textResults.jobId;
            }
            else
            {
                throw new Exception();
            }
        }

        public ObjectId ModifyLinkFeed(ObjectId itemId, LinkFeed linkFeed)
        {
            if (links.ContainsKey(itemId))
            {
                LinkFeedResults feedResults = DataManager.Instance.ModifyEntry(linkFeed, id) as LinkFeedResults;
                links.Add(feedResults.jobId, linkFeed);
                return feedResults.jobId;
            }
            else
            {
                throw new Exception();
            }
        }

        public ObjectId ModifyTextUpdate(ObjectId itemId, TextUpdate textUpdate)
        {
            if (links.ContainsKey(itemId))
            {
                TextUpdateResults textResults = DataManager.Instance.ModifyEntry(textUpdate, id) as TextUpdateResults;
                links.Add(textResults.jobId, textUpdate);
                return textResults.jobId;
            }
            else
            {
                throw new Exception();
            }
        }

        public void UpdateResults(ObjectId recordId, HtmlResults results)
        {
            if (links.ContainsKey(recordId))
            {
                links[recordId] = results;
            }
            else
                throw new Exception();
        }

        public void RemoveLink(ObjectId itemId)
        {
            if (links.ContainsKey(itemId))
            {
                links.Remove(itemId);
            }
            else
                throw new Exception();
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
