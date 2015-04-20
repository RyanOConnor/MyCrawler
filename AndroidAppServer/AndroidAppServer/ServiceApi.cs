using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using System.IO;

namespace AndroidAppServer
{
    public enum ServerResponse 
    { 
        Success, ServerError, InvalidPasswordType, InvalidPassword, UsernameAlreadyExists, LinkAlreadyExists,
        DummyResponse
    };

    public interface IServiceApi
    {
        string UserAuthorization(string username, string password);

        ServerResponse CreateNewAccount(string username, string password);
        KeyValuePair<ServerResponse, LinkOwner> AddLinkFeed(string url, List<string> htmlTags, HashSet<string> keywords);
        KeyValuePair<ServerResponse, LinkOwner> AddTextUpdate(string url, List<string> htmlTags, string innerText);
        ServerResponse ModifyFeed( LinkOwner modifiedEntry);
        ServerResponse ModifyUpdate(LinkOwner modifiedEntry);
        ServerResponse RemoveItem(ObjectId itemid);
        ServerResponse ChangePassword(string newPassword);
        ServerResponse DeleteUser(ObjectId userid);

        List<LinkOwner> RetrieveUpdates(List<ObjectId> updates);
    }

    public class ServiceApi : IServiceApi
    {
        private void WriteLog(Exception ex)
        {
            using (StreamWriter file = new StreamWriter("MyAppLog.txt", true))
            {
                file.Write(ex.ToString());
            }
        }

        private void WriteString(string s)
        {
            using (StreamWriter file = new StreamWriter("RestApiTest.txt", true))
            {
                file.Write(s);
            }
        }

        public string UserAuthorization(string username, string password)
        {
            if (UserManager.Instance.ValidateLoginAttempt(username, Encoding.UTF8.GetBytes(password)))
            {
                return Authorize.GetSessionToken();
            }
            else
            {
                return string.Empty;
            }
        }

        public ServerResponse CreateNewAccount(string username, string password)
        {
            try
            {
                if (Authorize.PassesGuidelines(password))
                {
                    byte[] passBytes = Encoding.UTF8.GetBytes(password);
                    byte[] salt = Authorize.GenerateSalt();
                    byte[] saltedHash = Authorize.GenerateSaltedHash(passBytes, salt);
                    Password pass = new Password(saltedHash, salt);
                    User newUser = new User(username, pass);
                    try
                    {
                        UserManager.Instance.SaveUser(newUser);
                        return ServerResponse.Success;
                    }
                    catch (MongoWriteConcernException ex)
                    {
                        WriteLog(ex);
                        return ServerResponse.UsernameAlreadyExists;
                    }
                }
                else
                {
                    return ServerResponse.InvalidPasswordType;
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                return ServerResponse.ServerError;
            }
        }

        public KeyValuePair<ServerResponse, LinkOwner> AddLinkFeed(ObjectId userid, string url, List<string> htmlTags, HashSet<string> keywords)
        {
            try
            {
                User user = UserManager.Instance.FindUserByID(userid);
                try
                {
                    LinkOwner results = user.AddLinkFeed(url, htmlTags, keywords);
                    UserManager.Instance.SaveUser(user);
                    return new KeyValuePair<ServerResponse, LinkOwner>(ServerResponse.Success, results);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return new KeyValuePair<ServerResponse, LinkOwner>(ServerResponse.LinkAlreadyExists, null);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new KeyValuePair<ServerResponse, LinkOwner>(ServerResponse.ServerError, null);
            }
        }

        public KeyValuePair<ServerResponse, LinkOwner> AddTextUpdate(ObjectId userid, string url, List<string> htmlTags, string innerText)
        {
            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter("RestApiTest.txt", true))
                {
                    file.Write("[{0}] AddTextUpdate({1}, {2}, {3})", DateTime.Now, userid, url, htmlTags.ToJson(), innerText);
                }
            }
            catch (Exception) { }

            try
            {
                User user = UserManager.Instance.FindUserByID(userid);
                try
                {
                    LinkOwner results = user.AddTextUpdate(url, htmlTags, innerText);
                    UserManager.Instance.SaveUser(user);
                    return new KeyValuePair<ServerResponse, LinkOwner>(ServerResponse.Success, results);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return new KeyValuePair<ServerResponse, LinkOwner>(ServerResponse.LinkAlreadyExists, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new KeyValuePair<ServerResponse, LinkOwner>(ServerResponse.ServerError, null);
            }
        }

        public ServerResponse ModifyFeed(LinkOwner modifiedEntry)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("RestApiTest.txt", true))
            {
                file.Write("[{0}] ModifyLinkFeed({1})", DateTime.Now, modifiedEntry.ToJson());
            }

            return ServerResponse.DummyResponse;
        }
        public ServerResponse ModifyUpdate(LinkOwner modifiedEntry)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("RestApiTest.txt", true))
            {
                file.Write("[{0}] ModifyUpdate({1})", DateTime.Now, modifiedEntry.ToJson());
            }

            return ServerResponse.DummyResponse;
        }
        public ServerResponse RemoveItem(ObjectId itemid)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("RestApiTest.txt", true))
            {
                file.Write("[{0}] RemoveItem({1})", DateTime.Now, itemid.ToJson());
            }

            return ServerResponse.DummyResponse;
        }
        public ServerResponse ChangePassword(string newPassword)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("RestApiTest.txt", true))
            {
                file.Write("[{0}] ChangePassword({1})", DateTime.Now, newPassword.ToJson());
            }

            return ServerResponse.DummyResponse;
        }
        public ServerResponse DeleteUser(ObjectId userid)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("RestApiTest.txt", true))
            {
                file.Write("[{0}] AddLinkFeed({1})", DateTime.Now, userid.ToJson());
            }

            return ServerResponse.DummyResponse;
        }

        public List<LinkOwner> RetrieveUpdates(List<ObjectId> updates)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("RestApiTest.txt", true))
            {
                file.Write("[{0}] AddLinkFeed({1})", DateTime.Now, updates.ToJson());
            }
            return new List<LinkOwner>();
        }
    }
}