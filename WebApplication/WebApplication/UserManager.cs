using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Builders;

namespace WebApplication
{
    public class UserManager
    {
        public enum queueStatus { createUser, findUserById, findUserByUserName, addLinkTouser, removeLinkFromUser, modifyUserLink, deleteUser };
        public static MongoCollection<User> UserCollection { get; set; }
        public Queue<KeyValuePair<User, queueStatus>> userWriteQueue { get; private set; }

        public static void Start()
        {
            UserCollection = Database.Instance.GetCollection<User>("UserData");
        }

        public static void CreateUser(string userName, byte[] inputPassword)
        {
            try
            {
                if (Authorize.PassesGuidelines(inputPassword))
                {
                    long count = UserCollection.FindAs<User>(Query.EQ("UserName", userName)).Count();
                    if (count == 0)
                    {
                        byte[] salt = Authorize.GenerateSalt();
                        byte[] saltedHash = Authorize.GenerateSaltedHash(inputPassword, salt);

                        User newUser = new User(userName, saltedHash, salt);
                        UserCollection.Save(newUser);
                    }
                    else
                    {
                        // Nofity user that username is taken
                        Console.WriteLine();
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

        public static User FindUserByID(ObjectId userid)
        {
            try
            {
                IMongoQuery queryUser = Query.EQ("_id", userid);
                User user = UserCollection.FindOne(queryUser);
                return user;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public static User findUserByUserName(string userName)
        {
            // query db, return user object
            return new User("", new byte[0], new byte[0]);
        }

         public string retrieveUserHash(ObjectId userid)
        {
            // query db for userid, return their hash value
            return string.Empty;
        }

        public static void AddLinkToUser(ObjectId userid, string userName, NewRecord newRecord)
        {
            // query database for user
            // create new "link" struct with values
            // query Crawl data database for existing link
            // if it doesn't exist then add this value to Crawl database with DataManager,
            //      if it does then return it's ObjectId in crawl database
            // add ObjectId and new link struct to Users dictionary
            // save to database collection
            try
            {
                User user = FindUserByID(userid);
                HTMLRecord newHtmlRecord = user.AddHTMLRecord(newRecord);

                if (newHtmlRecord != null)
                {
                    UserCollection.Save(user);
                    CrawlerManager.Instance.DistributeWork(newHtmlRecord);
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

        public static void UpdateUserLink(ObjectId userId, HTMLRecord record)
        {
            try
            {
                User user = FindUserByID(userId);
                user.UpdateHtmlRecord(record);
                UserCollection.Save(user);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public void modifyUserLink(ObjectId userid, string userName, ObjectId urlid, string url, object changes)
        {
            // change users link struct
        }

        public void removeLinkFromUser(ObjectId userid, string userName, ObjectId urlid, string url)
        {
            // query database for user
            // search Dictionary for url stringid or inside struct for url string
            // delete that Dictionary<object, link> pair
            // save the database collection
        }

        public void deleteUser(ObjectId userid, string userName, string passwordHash)
        {
            // query for userid
            // remove user
            // save database collection
        }
    }

    [DataContract]
    public class User : Serializable
    {
        [DataMember][BsonId]
        public ObjectId Id { get; private set; }
        [DataMember]
        public string UserName { get; private set; }
        [DataMember]
        public byte[] Password { get; private set; }
        [DataMember]
        public byte[] Salt { get; private set; }
        [DataMember][BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ObjectId, HTMLRecord> Links { get; private set; }

        public User(string userName, byte[] passwordHash, byte[] salt)
        {
            this.UserName = userName;
            this.Password = passwordHash;
            this.Salt = salt;
            Links = new Dictionary<ObjectId, HTMLRecord>();
        }   

        public HTMLRecord AddHTMLRecord(NewRecord record)
        {
            HTMLRecord htmlRecord = null;
            if(!Links.Any(pair => pair.Value.URL == record.URL))
            {
                htmlRecord = DataManager.Instance.CreateEntry(this.Id, record);
                Links.Add(htmlRecord.Id, htmlRecord);
            }
            else
            {
                Console.WriteLine("User: {0}\nLink Already Exists: {1}", UserName, record.URL);
                throw new Exception();
            }

            return htmlRecord;
        }

        public void UpdateHtmlRecord(HTMLRecord record)
        {
            if(Links.ContainsKey(record.Id))
            {
                Links[record.Id] = record;
            }
            else
            {
                throw new Exception();
            }
        }
    }
}
