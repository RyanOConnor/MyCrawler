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
        public static Queue<HTMLRecord> dataWriteQueue { get; private set; }

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

        /*public HTMLPage findUrlById(ObjectId id)
        {
            return HTMLPage;
        }

        public HTMLPage findUrlByUrl(string url)
        {
            return HTMLPage;
        }

        public List<HTMLPage> findMultipleByDateTime(List<DateTime> timeStamps)
        {
            return List<HTMLPage>();
        }

        public HTMLPage findSingleByDateTime(DateTime timeStamp)
        {
            return HTMLPage;
        }*/

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
