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
        public Dictionary<ObjectId, LinkOwner> links { get; private set; }

        public User(string newUserName, Password newPassword)
        {
            username = newUserName;
            password = newPassword;
            links = new Dictionary<ObjectId, LinkOwner>();
            previousPasswords = new List<Password>();
        }

        public LinkOwner AddLinkFeed(string url, List<string> htmlTags, HashSet<string> keywords)
        {
            if (!links.Any(pair => pair.Value.domain.OriginalString == url &&
                                   pair.Value.htmlTags.SequenceEqual(htmlTags)))
            {
                ILinkFeed linkFeed = new LinkFeed(url, htmlTags, keywords);
                FeedOwner feedResults = DataManager.Instance.CreateEntry(linkFeed, id) as FeedOwner;
                links.Add(feedResults.resultsid, feedResults);
                return feedResults;
            }
            else
            {
                throw new Exception();
            }
        }

        public LinkOwner AddTextUpdate(string url, List<string> htmlTags, string innerText)
        {
            if (!links.Any(pair => pair.Value.domain.OriginalString == url &&
                                  pair.Value.htmlTags.SequenceEqual(htmlTags)))
            {
                ITextUpdate textUpdate = new TextUpdate(url, htmlTags, innerText);
                TextOwner textResults = DataManager.Instance.CreateEntry(textUpdate, id) as TextOwner;
                links.Add(textResults.resultsid, textResults);
                return textResults;
            }
            else
            {
                throw new Exception();
            }
        }

        public LinkOwner ModifySubscription(LinkOwner modifiedResults)
        {
            if(links.ContainsKey(modifiedResults.resultsid))
            {
                LinkOwner linkOwner = DataManager.Instance.ModifyOwnership(modifiedResults);
                links[modifiedResults.resultsid] = linkOwner;
                return linkOwner;
            }
            else
            {
                throw new Exception();
            }
        }

        public void UpdateResults(LinkOwner userResults, ObjectId itemid)
        {
            if (links.ContainsKey(itemid))
            {
                links[itemid] = userResults;
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
