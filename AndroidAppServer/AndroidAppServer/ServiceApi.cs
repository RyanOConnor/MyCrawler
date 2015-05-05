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
        ObjectId UserAuthorization(string username, string password);

        Tuple<ServerResponse, ObjectId> CreateNewAccount(string username, string password);
        Tuple<ServerResponse, LinkFeedParams> AddLinkFeed(string userid, string url, string htmlTags, 
                                                          HashSet<string> keywords);
        Tuple<ServerResponse, LinkFeedParams> ModifyFeed(string userid, string recordid, string resultsid,
                                                         string htmlTags, HashSet<string> keywords);
        ServerResponse RemoveFeed(string userid, string resultsid);
        ServerResponse ChangePassword(string newPassword);
        ServerResponse DeleteUser(string userid);

        List<Tuple<LinkFeedParams, List<UserLink>>> RetrieveUpdates(string userid);
    }

    public class ServiceApi : IServiceApi
    {
        private void WriteLog(Exception ex)
        {
            using (StreamWriter file = new StreamWriter("ExceptionLog.txt", true))
            {
                file.Write(ex.ToString());
            }
        }

        private void WriteString(string s)
        {
            using (StreamWriter file = new StreamWriter("TestLog.txt", true))
            {
                file.Write(s);
            }
        }

        public ObjectId UserAuthorization(string username, string password)
        {
            return UserManager.Instance.ValidateLoginAttempt(username, Encoding.UTF8.GetBytes(password));
        }

        public Tuple<ServerResponse, ObjectId> CreateNewAccount(string username, string password)
        {
            Tuple<ServerResponse, ObjectId> response;
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
                        response = new Tuple<ServerResponse, ObjectId>(ServerResponse.Success, newUser.id);
                    }
                    catch (MongoWriteConcernException ex)
                    {
                        WriteLog(ex);
                        response = new Tuple<ServerResponse, ObjectId>(ServerResponse.UsernameAlreadyExists, ObjectId.Empty);
                    }
                }
                else
                {
                    response = new Tuple<ServerResponse, ObjectId>(ServerResponse.UsernameAlreadyExists, ObjectId.Empty);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                response = new Tuple<ServerResponse, ObjectId>(ServerResponse.ServerError, ObjectId.Empty);
            }
            return response;
        }

        public Tuple<ServerResponse, LinkFeedParams> AddLinkFeed(string userid, string url, 
                                                                 string htmlTags, HashSet<string> keywords)
        {
            Tuple<ServerResponse, LinkFeedParams> response = null;
            ObjectId userObjId = ObjectId.Parse(userid);
            try
            {
                User user = UserManager.Instance.FindUserByID(userObjId);
                try
                {
                    LinkFeedParams results = user.AddLinkFeed(url, htmlTags, keywords);
                    UserManager.Instance.SaveUser(user);
                    response = new Tuple<ServerResponse, LinkFeedParams>(ServerResponse.Success, results);
                }
                catch (Exception ex)
                {
                    WriteLog(ex);
                    response = new Tuple<ServerResponse, LinkFeedParams>(ServerResponse.LinkAlreadyExists, null);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                response = new Tuple<ServerResponse, LinkFeedParams>(ServerResponse.ServerError, null);
            }
            return response;
        }

        public Tuple<ServerResponse, LinkFeedParams> ModifyFeed(string userid, string recordid, string resultsid, 
                                                                string htmlTags, HashSet<string> keywords)
        {
            Tuple<ServerResponse, LinkFeedParams> response = null;
            ObjectId userObjId = ObjectId.Parse(userid);
            try
            {
                User user = UserManager.Instance.FindUserByID(userObjId);
                ObjectId recordObjId = ObjectId.Parse(recordid);
                ObjectId resultsObjId = ObjectId.Parse(resultsid);
                LinkFeedParams parameters = user.ModifySubscription(recordObjId, resultsObjId, htmlTags, keywords);
                UserManager.Instance.SaveUser(user);
                response = new Tuple<ServerResponse, LinkFeedParams>(ServerResponse.Success, parameters);
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                response = new Tuple<ServerResponse, LinkFeedParams>(ServerResponse.ServerError, null);
            }

            return response;
        }

        public ServerResponse RemoveFeed(string userid, string resultsid)
        {
            ServerResponse response = ServerResponse.ServerError;
            try
            {
                ObjectId userObjId = ObjectId.Parse(userid);
                ObjectId resultsObjId = ObjectId.Parse(resultsid);
                User user = UserManager.Instance.FindUserByID(userObjId);
                user.RemoveLink(resultsObjId);
                UserManager.Instance.SaveUser(user);
                response = ServerResponse.Success;
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                response = ServerResponse.ServerError;
            }
            return response;
        }

        public ServerResponse ChangePassword(string newPassword)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("RestApiTest.txt", true))
            {
                file.Write("[{0}] ChangePassword({1})", DateTime.Now, newPassword.ToJson());
            }

            return ServerResponse.DummyResponse;
        }

        public ServerResponse DeleteUser(string userid)
        {
            try
            {
                ObjectId userObjId = ObjectId.Parse(userid);
                bool successful = UserManager.Instance.DeleteUser(userObjId);

                if (successful)
                    return ServerResponse.Success;
                else
                    return ServerResponse.ServerError;
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                return ServerResponse.ServerError;
            }
        }

        public List<Tuple<LinkFeedParams, List<UserLink>>> RetrieveUpdates(string userid)
        {
            try
            {
                ObjectId userObjId = ObjectId.Parse(userid);
                User user = UserManager.Instance.FindUserByID(userObjId);
                return user.GetAllLinkFeeds();
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                return null;
            }
        }
    }
}