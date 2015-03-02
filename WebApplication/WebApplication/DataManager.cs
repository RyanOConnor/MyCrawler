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
    public static class DataManager
    {
        private static Queue<HTMLRecord> writeQueue { get; private set; }

        public static void createEntry(ObjectId urlid, string url)
        {

        }

        public static void modifyEntry(ObjectId urlid, object changes)
        {

        }

        public static List<HTMLRecord> mockDatabase = new List<HTMLRecord>();
        public static void updateEntry(HTMLRecord page)
        {
            // add page to writeQueue
            mockDatabase.Add(page);
        }

        public HTMLRecord findUrlById(ObjectId id)
        {
            return new HTMLRecord("", "", DateTime.Now, new List<string>());
        }

        public HTMLRecord findUrlByUrl(string url)
        {
            return new HTMLRecord("", "", DateTime.Now, new List<string>());
        }

        public List<HTMLRecord> findMultipleByDateTime(List<DateTime> timeStamps)
        {
            return new List<HTMLRecord>();
        }

        public HTMLRecord findSingleByDateTime(DateTime timeStamp)
        {
            return new HTMLRecord("", "", DateTime.Now, new List<string>());
        }

    }
    [DataContract]
    public class HTMLRecord
    {
        //public ObjectId id { get; private set; }
        [DataMember]
        public string id { get; private set; }
        [DataMember]
        public string url { get; private set; }
        [DataMember]
        public DateTime timeStamp { get; private set; }
        [DataMember]
        public List<string> htmlHashes { get; private set; }

        public HTMLRecord(string id, string url, DateTime timeStamp, List<string> contentHashes)
        {
            this.id = id;
            this.url = url;
            this.timeStamp = timeStamp;
            this.htmlHashes = contentHashes;
        }
    }
}
