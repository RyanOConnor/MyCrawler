using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace WebApplication
{
    public static class UserManager
    {
        public enum queueStatus { createUser, findUserById, findUserByUserName, addLinkTouser, removeLinkFromUser, modifyUserLink, deleteUser };
        public static Queue<KeyValuePair<User, queueStatus>> userWriteQueue { get; private set; }

        public static void createUser(ObjectId userid, string userName, string password)
        {
            // query database, hash password, create new User, save to collection
        }

        public static User findUserByID( ObjectId userid)
        {
            // query db, return user object
            return new User(new ObjectId(), "", "", new Dictionary<ObjectId,User.url>());
        }

         public static User findUserByUserName(string userName)
        {
            // query db, return user object
            return new User(new ObjectId(), "", "", new Dictionary<ObjectId,User.url>());
        }

         public static string retrieveUserHash(ObjectId userid)
        {
            // query db for userid, return their hash value
            return string.Empty;
        }

        public static void addLinkToUser(ObjectId userid, string userName, string url, string[] htmlTags, string[] keywords)
        {
            // query database for user
            // create new "link" struct with values
            // query Crawl data database for existing link
            // if it doesn't exist then add this value to Crawl database with DataManager,
            //      if it does then return it's ObjectId in crawl database
            // add ObjectId and new link struct to Users dictionary
            // save to database collection
        }

        public static void removeLinkFromUser(ObjectId userid, string userName, ObjectId urlid, string url)
        {
            // query database for user
            // search Dictionary for url stringid or inside struct for url string
            // delete that Dictionary<object, link> pair
            // save the database collection
        }

        public static void modifyUserLink(ObjectId userid, string userName, ObjectId urlid, string url, object changes)
        {
            // change users link struct
        }

        public static void deleteUser(ObjectId userid, string userName, string passwordHash)
        {
            // query for userid
            // remove user
            // save database collection
        }
    }

    public class User
    {
        public ObjectId userid { get; private set; }
        public string userName { get; private set; }
        public string passwordHash { get; private set; }
        public Dictionary<ObjectId, url> links { get; private set; }

        public struct url
        {
            public string urlValue { get; set; }
            public string[] htmlTags { get; set; }
            public string[] keywords { get; set; }
        }

        public User(ObjectId userid, string userName, string passwordHash, Dictionary<ObjectId, url> links)
        {
            this.userid = userid;
            this.userName = userName;
            this.passwordHash = passwordHash;
            foreach(KeyValuePair<ObjectId, url> link in links)
            {
                this.links.Add(link.Key, link.Value);
            }
        }
        
    }
}
