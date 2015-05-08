using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.IO.Compression;
using MongoDB.Bson;

namespace WebCrawlerNode
{

    public interface IServiceApi
    {
        ServerResponse EnqueueJob(byte[] zippedBytes);
        Tuple<ServerResponse, JobStatus> StatusRequest(string recordid);
    }

    public class ServiceApi : IServiceApi
    {
        public ServerResponse EnqueueJob(byte[] zippedBytes)
        {
            ServerResponse response = ServerResponse.ServerError;
            try
            {
                byte[] recordBytes = RestAPI.Unzip(zippedBytes);
                HtmlRecord record = BSON.Deserialize<HtmlRecord>(recordBytes);
                record.Initialize();
                WebCrawler.Instance.EnqueueWork(record);
                response = ServerResponse.Success;
                System.Diagnostics.Debug.Print("[" + DateTime.Now.ToString() + "]" + " Received: " + 
                                                record.domain.AbsoluteUri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                response = ServerResponse.ServerError;
            }
            return response;
        }

        public Tuple<ServerResponse, JobStatus> StatusRequest(string recordid)
        {
            Tuple<ServerResponse, JobStatus> response = null;
            try
            {
                ObjectId recordObjId = ObjectId.Parse(recordid);
                JobStatus status = WebCrawler.Instance.GetJobStatus(recordObjId);
                response = new Tuple<ServerResponse, JobStatus>(ServerResponse.Success, status);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                response = new Tuple<ServerResponse, JobStatus>(ServerResponse.ServerError, JobStatus.None);
            }
            return response;
        }
    }
}