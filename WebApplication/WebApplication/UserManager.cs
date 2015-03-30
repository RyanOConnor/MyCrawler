using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Builders;
using System.Diagnostics;
using System.Net;

namespace WebApplication
{
    public class UserManager
    {
        public enum QueueStatus { createUser, findUserById, findUserByUserName, addLinkTouser, removeLinkFromUser, modifyUserLink, deleteUser };
        public SocketServer UserListener { get; set; }
        public Queue<UserMessage> UserMessageQueue { get; set; }
        private static UserManager _instance;
        public static UserManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new UserManager();
                return _instance;
            }
        }

        public void Start()
        {
            UserListener = new SocketServer();
            UserListener.StartListener(IPAddress.Any);
        }

        public MongoCollection<User> GetCollection()
        {
            return Database.Instance.GetCollection<User>("UserData");
        }

        public void CreateUser(string userName, byte[] inputPassword)
        {
            try
            {
                if (Authorize.PassesGuidelines(inputPassword))
                {
                    long count = GetCollection().FindAs<User>(Query.EQ("UserName", userName)).Count();
                    if (count == 0)
                    {
                        byte[] salt = Authorize.GenerateSalt();
                        byte[] saltedHash = Authorize.GenerateSaltedHash(inputPassword, salt);

                        User newUser = new User(userName, saltedHash, salt);
                        GetCollection().Save(newUser);
                    }
                    else
                    {
                        // Nofity user that username is taken
                    }
                }
                else
                {
                    // Notify user that password doesn't follow guidelines
                    Console.WriteLine();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public bool ValidateLoginAttempt(ObjectId userid, string username, byte[] password)
        {
            User user = FindUserByID(userid);
            byte[] saltedHash = Authorize.GenerateSaltedHash(password, user.Salt);
            return Authorize.IsValidHash(saltedHash, user.Password);
        }

        public User FindUserByID(ObjectId userid)
        {
            try
            {
                IMongoQuery queryUser = Query.EQ("_id", userid);
                User user = GetCollection().FindOne(queryUser);
                return user;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public void AddLinkToUser(ObjectId userid, string userName, NewRecord newRecord)
        {
            try
            {
                User user = FindUserByID(userid);
                bool addSuccessful = user.AddHTMLRecord(newRecord);

                if (addSuccessful)
                {
                    GetCollection().Save(user);
                }
                else
                {
                    // Notify user that record already exists
                    throw new Exception("[sent user error message]");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public void UpdateUsersByRecord(HtmlRecord record)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach(HtmlResults results in record.Results.Values)
            {
                IMongoQuery query = Query.EQ("Links.v._id", results.JobId);
                MongoCursor<User> users = GetCollection().FindAs<User>(query);

                foreach(User user in users)
                {
                    user.UpdateResults(record.Id, results);
                    GetCollection().Save(user);             // Instead of writing, just notify users
                }
                Console.WriteLine("\n\tUpdated {0} user records with {1}", users.Count(), record.URL);
            }
            sw.Stop();
            Console.WriteLine("{0}ms", sw.ElapsedMilliseconds);
        }

        public void removeLinkFromUser(ObjectId userid, HtmlResults results)
        {
            User user = FindUserByID(userid);
            user.RemoveLink(results);
            GetCollection().Save(user);
        }

        public void DeleteUser(ObjectId userid, string userName, string passwordHash)
        {
            IMongoQuery query = Query.EQ("_id", userid);
            // Remove users information from crawl data
            GetCollection().Remove(query);
        }
    }

    public class User : Serializable
    {
        [BsonId]
        public ObjectId Id { get; private set; }
        public string UserName { get; private set; }
        public byte[] Password { get; private set; }
        public byte[] Salt { get; private set; }
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ObjectId, HtmlResults> Links { get; private set; }

        public User(string userName, byte[] passwordHash, byte[] salt)
        {
            this.UserName = userName;
            this.Password = passwordHash;
            this.Salt = salt;
            Links = new Dictionary<ObjectId, HtmlResults>();
        }

        public bool AddHTMLRecord(NewRecord newRecord)
        {
            if(!Links.Any(pair => pair.Value.Domain.OriginalString == newRecord.URL &&
                                  pair.Value.HtmlTags.SequenceEqual(newRecord.HtmlTags)))
            {
                KeyValuePair<ObjectId, HtmlResults> newResults = DataManager.Instance.CreateEntry(newRecord);
                Links.Add(newResults.Key, newResults.Value);
                return true;
            }
            else
            {
                Console.WriteLine("User: {0}\nLink Already Exists: {1}", UserName, newRecord.URL);
                return false;
            }
        }

        public void UpdateResults(ObjectId recordId, HtmlResults results)
        {
            if(Links.ContainsKey(recordId))
            {
                Links[recordId] = results;
            }
            else
            {
                throw new Exception();
            }
        }

        public void RemoveLink(HtmlResults results)
        {
            if(Links.ContainsKey(results.JobId))
            {
                Links.Remove(results.JobId);
            }
            else
            {
                throw new Exception();
            }
        }

    }

    public class NewRecord : Serializable
    {
        [BsonId]
        public ObjectId UserId { get; set; }
        public Type RecordType { get; set; }
        public string URL { get; set; }
        public List<string> HtmlTags { get; set; }
        public List<string> Keywords { get; set; }
        public string EmbeddedText { get; set; }

        public NewRecord(Type type, string url, List<string> tags, List<string> keywords, string innerText)
        {
            RecordType = type;
            URL = url;
            HtmlTags = tags;
            Keywords = keywords;
            EmbeddedText = innerText;
        }

        public void AddUserId(ObjectId id)
        {
            UserId = id;
        }
    }

    public class UserMessage : EventArgs
    {

    }
}
