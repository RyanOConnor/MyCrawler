using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.ServiceModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AndroidAppServer
{
    class RestAPI
    {
        private const string urlString = "http://localhost:51748/Handler.ashx";
        //private const string urlString = "http://67.175.150.75/CrawlerApi/Handler.ashx";
        //private const string urlString = "http://localhost:83/CrawlerApi/Handler.ashx";
        public const int bufferSize = 4096;
        public const int gzipBufferSize = 64 * 1024;

        private string Load(string contents)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(urlString);
            req.AllowWriteStreamBuffering = true;
            req.Method = "POST";
            req.Timeout = 60000;
            Stream outStream = req.GetRequestStream();
            StreamWriter outStreamWriter = new StreamWriter(outStream);
            outStreamWriter.Write(contents);
            outStreamWriter.Flush();
            outStream.Close();
            WebResponse res = req.GetResponse();
            Stream httpStream = res.GetResponseStream();
            MemoryStream memoryStream = new MemoryStream();
            try
            {
                byte[] buff = new byte[bufferSize];
                int readedBytes = httpStream.Read(buff, 0, buff.Length);
                while (readedBytes > 0)
                {
                    memoryStream.Write(buff, 0, readedBytes);
                    readedBytes = httpStream.Read(buff, 0, buff.Length);
                }
            }
            finally
            {
                if (httpStream != null)
                {
                    httpStream.Close();
                }

                if (memoryStream != null)
                {
                    memoryStream.Close();
                }
            }
            byte[] data = memoryStream.ToArray();
            string result = Encoding.UTF8.GetString(data, 0, data.Length);
            return result;
        }

        public JObject EnqueueJob(byte[] recordBytes)
        {
            JObject result = null;
            JObject o = new JObject();
            JObject p = new JObject();
            o["interface"] = "Crawler_Node_RestAPI";
            o["method"] = "EnqueueJob";
            byte[] zippedBytes = Zip(recordBytes);
            p["zippedBytes"] = JToken.FromObject(zippedBytes);
            o["parameters"] = p;
            string s = JsonConvert.SerializeObject(o);
            string r = Load(s);
            result = JObject.Parse(r);
            return result;
        }

        public JObject StatusRequest(string recordid)
        {
            JObject result = null;
            JObject o = new JObject();
            JObject p = new JObject();
            o["interface"] = "Crawler_Node_RestAPI";
            o["method"] = "StatusRequest";
            p["recordid"] = JToken.FromObject(recordid);
            o["parameters"] = p;
            string s = JsonConvert.SerializeObject(o);
            string r = Load(s);
            result = JObject.Parse(r);
            return result;
        }

        public static byte[] Zip(byte[] bytes)
        {
            using (var mstream = new MemoryStream())
            {
                using (var gzip = new BufferedStream(new GZipStream(mstream, 
                                    CompressionMode.Compress), gzipBufferSize))
                {
                    gzip.Write(bytes, 0, bytes.Length);
                }
                return mstream.ToArray();
            }
        }

        public static byte[] Unzip(byte[] bytes)
        {
            using (var input = new MemoryStream(bytes))
            {
                using (var output = new MemoryStream())
                {
                    using (var gzip = new BufferedStream(new GZipStream(input,
                                        CompressionMode.Decompress), gzipBufferSize))
                    {
                        gzip.CopyTo(output);
                    }
                    return output.ToArray();
                }
            }
        }

        public ServerResponse ParseResponse(JObject obj)
        {
            int responseInt = obj.GetValue("Value").Value<int>();
            ServerResponse response = (ServerResponse)responseInt;
            return response;
        }

        public Tuple<ServerResponse, JobStatus> ParseStatusResponse(JObject obj)
        {
            JObject value = obj.GetValue("Value").Value<JObject>();
            int responseInt = value.GetValue("Item1").Value<int>();
            ServerResponse response = (ServerResponse)responseInt;
            int statusInt = value.GetValue("Item2").Value<int>();
            JobStatus status = (JobStatus)statusInt;
            return new Tuple<ServerResponse, JobStatus>(response, status);
        }
    }
}
