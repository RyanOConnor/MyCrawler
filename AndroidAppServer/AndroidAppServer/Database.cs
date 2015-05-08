using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace AndroidAppServer
{
    class Database
    {
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

        private MongoServer server { get; set; }
        private MongoDatabase db { get; set; }
        private MongoClient dbClient { get; set; }
        private MongoCollection<HtmlRecord> _htmlCollection;
        public MongoCollection<HtmlRecord> htmlCollection { get { return _htmlCollection; } }
        private MongoCollection<User> _userCollection;
        public MongoCollection<User> userCollection { get { return _userCollection; } }

        public void Start()
        {
            dbClient = new MongoClient();
            server = dbClient.GetServer();
            db = server.GetDatabase("CloudCrawler");
            _htmlCollection = db.GetCollection<HtmlRecord>("CrawlData");
            _userCollection = db.GetCollection<User>("UserData");

            if (!_htmlCollection.IndexExists(IndexKeys<HtmlRecord>.Ascending(val => val.url)))
                _htmlCollection.CreateIndex(IndexKeys<HtmlRecord>.Ascending(val => val.url), IndexOptions.SetUnique(true));
            if (!_userCollection.IndexExists(IndexKeys<User>.Ascending(val => val.username)))
                _userCollection.CreateIndex(IndexKeys<User>.Ascending(val => val.username), IndexOptions.SetUnique(true));
        }
    }
}
