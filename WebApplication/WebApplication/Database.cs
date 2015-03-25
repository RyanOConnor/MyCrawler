using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace WebApplication
{
    public class Database
    {
        private MongoServer Server { get; set; }
        private MongoDatabase DB { get; set; }
        private MongoClient dbClient { get; set; }
        private static Database _instance;
        public static Database Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Database();
                return _instance;
            }
        }

        public void Start()
        {
            dbClient = new MongoClient();
            Server = dbClient.GetServer();
            DB = Server.GetDatabase("CloudCrawler");
        }

        public MongoCollection<T> GetCollection<T>(string name)
        {
            MongoCollection<T> collection = null;
            if(DB.CollectionExists(name))
            {
                collection = DB.GetCollection<T>(name);
            } 

            return collection;
        }
    }
}
