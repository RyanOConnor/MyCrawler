using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;

namespace AndroidAppServer
{
    public enum ServerResponse 
    { 
        Success, ServerError, InvalidPasswordType, InvalidPassword, UsernameAlreadyExists, LinkAlreadyExists
    }

    public interface IServiceApi
    {
        ObjectId UserAuthorization(string username, string password);

        Tuple<ServerResponse, ObjectId> CreateNewAccount(string username, string password);
        Tuple<ServerResponse, LinkFeedParams> AddLinkFeed(string userid, string url, string htmlTags, 
                                                          HashSet<string> keywords);
        Tuple<ServerResponse, LinkFeedParams> ModifyFeed(string userid, string recordid, string resultsid,
                                                         string htmlTags, HashSet<string> keywords);
        ServerResponse RemoveFeed(string userid, string resultsid);
        ServerResponse ChangePassword(string userid, string previousPassword, string newPassword);
        ServerResponse DeleteUser(string userid);

        string RetrieveUpdates(string userid);
    }

    public class ServiceApi : IServiceApi
    {
        public ObjectId UserAuthorization(string username, string password)
        {
            byte[] passBytes = Encoding.UTF8.GetBytes(password);
            return UserManager.Instance.ValidateLoginAttempt(username.ToLower(), passBytes);
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
                    User newUser = new User(username.ToLower(), pass);
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

        public ServerResponse ChangePassword(string userid, string previousPassword, string newPassword)
        {
            ServerResponse response = ServerResponse.ServerError;
            try
            {
                ObjectId userObjId = ObjectId.Parse(userid);
                User user = UserManager.Instance.FindUserByID(userObjId);
                if (Authorize.PassesGuidelines(newPassword))
                {
                    byte[] passBytes = Encoding.UTF8.GetBytes(newPassword);

                    bool passwordNotUsedBefore = user.ChangePassword(passBytes);
                    if (passwordNotUsedBefore)
                    {
                        UserManager.Instance.SaveUser(user);
                        response = ServerResponse.Success;
                    }
                    else
                    {
                        response = ServerResponse.InvalidPassword;
                    }
                }
                else
                {
                    response = ServerResponse.InvalidPasswordType;
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                response = ServerResponse.ServerError;
            }
            return response;
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

        public string RetrieveUpdates(string userid)
        {
            System.Diagnostics.Debug.Print("RetrieveUpdates: " + userid);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                ObjectId userObjId = ObjectId.Parse(userid);
                User user = UserManager.Instance.FindUserByID(userObjId);

                List<Tuple<LinkFeedParams, List<UserLink>>> tupleList = user.GetAllLinkFeeds();
                long first = sw.ElapsedMilliseconds;

                JArray jArray = new JArray();
                foreach (Tuple<LinkFeedParams, List<UserLink>> tuple in tupleList)
                {
                    JObject jTuple = new JObject();
                    JObject jParams = new JObject();
                    JArray jKeywords = new JArray();
                    jParams.Add("recordid", tuple.Item1.recordid.ToString());
                    jParams.Add("resultsid", tuple.Item1.resultsid.ToString());
                    jParams.Add("url", tuple.Item1.url);
                    jParams.Add("htmlTags", tuple.Item1.htmlTags);
                    foreach (string keyword in tuple.Item1.keywords)
                    {
                        jKeywords.Add(keyword);
                    }
                    jParams.Add("keywords", jKeywords);

                    JArray jLinks = new JArray();
                    foreach (UserLink link in tuple.Item2)
                    {
                        JObject jLink = new JObject();
                        jLink.Add("url", link.url);
                        jLink.Add("innerText", link.innerText);
                        jLink.Add("pageRank", link.pageRank);
                        jLinks.Add(jLink);
                    }

                    jTuple.Add("Item1", jParams);
                    jTuple.Add("Item2", jLinks);
                    jArray.Add(jTuple);
                }

                long second = sw.ElapsedMilliseconds - first;

                sw.Stop();
                System.Diagnostics.Debug.Print("\r\n[RetrieveUpdates]: \r\n\t GetLinks: " + 
                                                first + "ms\r\n\t JSON: " + second + 
                                                "ms\r\n\t Total: " + sw.ElapsedMilliseconds + 
                                                "ms\r\n");
                return jArray.ToString(Formatting.None);
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                return null;
            }
        }

        private void WriteLog(Exception ex)
        {
            System.Diagnostics.Debug.Print(ex.ToString());
            System.Diagnostics.Debug.Print(ex.StackTrace);
        }
    }
}