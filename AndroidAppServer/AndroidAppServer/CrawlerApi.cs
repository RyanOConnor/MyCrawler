using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.IO.Compression;
using MongoDB.Bson;

namespace AndroidAppServer
{
    public interface ICrawlerApi
    {
        string Authenticate(string domain);
        ServerResponse ReturnFinishedJob(byte[] zippedBytes);
    }
    
    public class CrawlerApi : ICrawlerApi
    {
        private void WriteLog(Exception ex)
        {
            System.Diagnostics.Debug.Print(ex.ToString());
            System.Diagnostics.Debug.Print(ex.StackTrace);
        }

        public string Authenticate(string domain)
        {
            CrawlerManager.Instance.AddCrawlerNode(new Uri(domain));
            return "certificate";
        }

        public ServerResponse ReturnFinishedJob(byte[] zippedBytes)
        {
            ServerResponse response = ServerResponse.ServerError;
            try
            {
                byte[] recordBytes = RestAPI.Unzip(zippedBytes);
                HtmlRecord record = BSON.Deserialize<HtmlRecord>(recordBytes);
                CrawlerManager.Instance.RemoveJob(record.recordid);
                DataManager.Instance.UpdateEntry(record);
                response = ServerResponse.Success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                response = ServerResponse.ServerError;
            }
            return response;
        }
    }
}