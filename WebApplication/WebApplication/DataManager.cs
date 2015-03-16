using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Runtime.Serialization;

namespace WebApplication
{
    public class DataManager
    {
        private static MongoServer Server { get; set; }
        private static MongoDatabase Database { get; set; }
        private static MongoCollection Collection { get; set; }
        private static MongoClient dbClient = new MongoClient();
        private static Queue<HTMLRecord> writeQueue = new Queue<HTMLRecord>();

        public DataManager()
        {
           
        }

        public static void Start()
        {
            Server = dbClient.GetServer();
            Database = Server.GetDatabase("AppDatabase");
            Collection = Database.GetCollection<HTMLRecord>("CrawlData");
        }

        public static void createEntry(ObjectId urlid, string url)
        {

        }

        public static void modifyEntry(ObjectId urlid, object changes)
        {

        }

        public static void updateEntry(HTMLRecord page)
        {
            Collection.Save(page);
        }

        public static HTMLRecord findUrlById(ObjectId id)
        {
            return new HTMLRecord(new ObjectId(), "", DateTime.Now, new List<string>(),new List<string>());
        }

        public static HTMLRecord findUrlByUrl(string url)
        {
            return new HTMLRecord(new ObjectId(), "", DateTime.Now, new List<string>(), new List<string>());
        }

        public static List<HTMLRecord> findMultipleByDateTime(List<DateTime> timeStamps)
        {
            return new List<HTMLRecord>();
        }

        public static HTMLRecord findSingleByDateTime(DateTime timeStamp)
        {
            return new HTMLRecord(new ObjectId(), "", DateTime.Now, new List<string>(), new List<string>());
        }

    }
    [DataContract]
    public class HTMLRecord
    {
        [DataMember]
        public ObjectId ID { get; private set; }
        [DataMember]
        public string URL { get; private set; }
        [DataMember]
        public DateTime TimeStamp { get; private set; }
        [DataMember]
        public List<string> HtmlTags { get; private set; }
        [DataMember]
        public List<string> Keywords { get; private set; }
        [DataMember]
        public List<string> RankedResults { get; private set; }

        public HTMLRecord(ObjectId id, string url, DateTime timeStamp, List<string> tags, List<string> keywords)
        {
            this.ID = id;
            this.URL = url;
            this.TimeStamp = timeStamp;
            this.HtmlTags = tags;
            this.Keywords = keywords;
        }
    }
}
