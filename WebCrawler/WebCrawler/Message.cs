using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebCrawler
{
    [BsonKnownTypes(typeof(ResendMessage), typeof(ReadyMessage), typeof(RecordMessage),
                typeof(StatusReport), typeof(StatusRequest))]
    public class Message : Serializable
    {
        [BsonId]
        public ObjectId id = ObjectId.GenerateNewId();
    }

    public class DestroyedBuffer : Message
    { }

    public class ResendMessage : Message, Serializable
    { }

    public class ReadyMessage : Message, Serializable
    {
        public ObjectId idReceived { get; set; }

        public ReadyMessage(ObjectId received)
        {
            idReceived = received;
        }
    }

    public class RecordMessage : Message, Serializable
    {
        public HtmlRecord htmlRecord { get; set; }

        public RecordMessage(HtmlRecord record)
        {
            htmlRecord = record;
        }
    }

    public class StatusRequest : Message, Serializable
    {
        public ObjectId requestedId { get; set; }

        public StatusRequest(ObjectId id)
        {
            requestedId = id;
        }
    }

    public class StatusReport : Message, Serializable
    {
        public KeyValuePair<ObjectId, JobStatus> statusReport { get; set; }

        public StatusReport(KeyValuePair<ObjectId, JobStatus> report)
        {
            statusReport = report;
        }
    }
}
