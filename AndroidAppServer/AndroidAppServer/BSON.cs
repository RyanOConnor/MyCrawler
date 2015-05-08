using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace AndroidAppServer
{
    public interface Serializable { }

    public static class BSON
    {
        public static byte[] Serialize<T>(this T obj) where T : Serializable
        {
            try
            {
                return obj.ToBson<T>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                throw ex;
            }
        }

        public static T Deserialize<T>(byte[] bson) where T : Serializable
        {
            try
            {
                string test = Encoding.UTF8.GetString(bson);
                T obj = BsonSerializer.Deserialize<T>(bson);
                return obj;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                throw ex;
            }
        }
    }

    public static class JSON
    {
        public static string Serialize<T>(this T obj)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
                    ser.WriteObject(stream, obj);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public static T Deserialize<T>(string json)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
                    return (T)ser.ReadObject(stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }
    }
}
