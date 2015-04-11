using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApplication
{
    public enum Response { UserCreated, UserAlreadyExists, InvalidUser, LinkAdded, LinkAlreadyExist, Success, InvalidPassword, RecordAlreadyExists, ServerError };

    [BsonKnownTypes(typeof(CreateUser), typeof(SyncData), typeof(AddLinkFeed), typeof(AddTextUpdate), typeof(SyncRequest),
                    typeof(ModifyItem), typeof(RemoveItem), typeof(ChangePassword), typeof(DeleteUser))]
    public class UserMessage : Message, Serializable
    {
        public ObjectId userid { get; set; }
        public string username { get; set; }
    }

    public class SyncRequest : UserMessage, Serializable
    {
        public LinkOwner linkOwner { get; set; }
        public SyncData RetrieveData()
        {
            SyncData response = null;
            try
            {
                LinkOwner results = DataManager.Instance.ManualRequest(linkOwner);
                response = new SyncData(userid, results);
            }
            catch(Exception ex)
            {
                response = new SyncData(userid, null);
                throw ex;
            }

            return response;
        }
    }

    public class SyncData : UserMessage, Serializable
    {
        public LinkOwner userData { get; set; }

        public SyncData(ObjectId id, LinkOwner results)
        {
            userid = id;
            userData = results;
        }
    }

    public class ServerResponse : Message, Serializable
    {
        public ObjectId userId { get; set; }
        public LinkOwner owner { get; set; }
        public Response serverResponse { get; set; }

        public ServerResponse(ObjectId user, Response response)
        {
            userId = user;
            serverResponse = response;
        }

        public void AddOwnerToResponse(LinkOwner owner)
        {
            this.owner = owner;
        }
    }

    public class CreateUser : UserMessage, Serializable
    {
        public string enteredUserName { get; set; }
        public byte[] enteredPassword { get; set; }

        public ServerResponse Create()
        {
            ServerResponse response = null;
            try
            {
                if (Authorize.PassesGuidelines(enteredPassword))
                {
                    byte[] salt = Authorize.GenerateSalt();
                    byte[] saltedHash = Authorize.GenerateSaltedHash(enteredPassword, salt);
                    Password password = new Password(saltedHash, salt);

                    User newUser = new User(enteredUserName, password);
                    try
                    {
                        UserManager.Instance.SaveUser(newUser);
                        response = new ServerResponse(newUser.id, Response.UserCreated);
                    }
                    catch(MongoWriteConcernException)
                    {
                        response = new ServerResponse(userid, Response.UserAlreadyExists);
                    }
                }
                else
                {
                    response = new ServerResponse(userid, Response.InvalidPassword);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new ServerResponse(userid, Response.ServerError);
                throw ex;
            }

            return response;
        }
    }

    public class AddLinkFeed : UserMessage, Serializable
    {
        public string url { get; set; }
        public List<string> htmlTags { get; set; }
        public HashSet<string> keywords { get; set; }

        public ServerResponse AddLink()
        {
            ServerResponse response = null;

            try
            {
                User user = UserManager.Instance.FindUserByID(userid);
                LinkOwner results = null;
                try
                {
                    results = user.AddLinkFeed(url, htmlTags, keywords);

                    if (results != null)
                    {
                        UserManager.Instance.SaveUser(user);
                        response = new ServerResponse(user.id, Response.LinkAdded);
                        response.AddOwnerToResponse(results);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    response = new ServerResponse(userid, Response.LinkAlreadyExist);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new ServerResponse(userid, Response.ServerError);
                throw ex;
            }

            return response;
        }
    }

    public class AddTextUpdate : UserMessage, Serializable
    {
        public string url { get; set; }
        public List<string> htmlTags { get; set; }
        public string innerText { get; set; }

        public ServerResponse AddLink()
        {
            ServerResponse response = null;

            try
            {
                User user = UserManager.Instance.FindUserByID(userid);
                LinkOwner results = null;
                try
                {
                    results = user.AddTextUpdate(url, htmlTags, innerText);

                    if(results != null)
                    {
                        UserManager.Instance.SaveUser(user);
                        response = new ServerResponse(user.id, Response.LinkAdded);
                        response.AddOwnerToResponse(results);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    response = new ServerResponse(userid, Response.LinkAlreadyExist);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new ServerResponse(userid, Response.ServerError);
            }

            return response;
        }
    }

    public class ModifyItem : UserMessage, Serializable
    {
        public LinkOwner modifiedKeywords { get; set; }

        public ServerResponse Modify()
        {
            ServerResponse response = null;
            try
            {
                User user = UserManager.Instance.FindUserByID(this.userid);
                LinkOwner results = null;
                try
                {
                    results = user.ModifySubscription(modifiedKeywords);
                    if (results != null)
                    {
                        response = new ServerResponse(user.id, Response.Success);
                        response.AddOwnerToResponse(results);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    throw ex;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new ServerResponse(userid, Response.ServerError);
            }

            return response;
        }
    }

    public class RemoveItem : UserMessage, Serializable
    {
        public ObjectId itemId { get; set; }

        public ServerResponse RemoveFromUser()
        {
            ServerResponse response = null;
            try
            {
                User user = UserManager.Instance.FindUserByID(this.userid);
                user.RemoveLink(itemId);
                UserManager.Instance.SaveUser(user);

                response = new ServerResponse(userid, Response.Success);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new ServerResponse(userid, Response.ServerError);
            }

            return response;
        }
    }

    public class ChangePassword : UserMessage, Serializable
    {
        public byte[] newPassword { get; set; }

        public ServerResponse Start()
        {
            ServerResponse response = null;
            try
            {
                if (Authorize.PassesGuidelines(newPassword))
                {
                    User user = UserManager.Instance.FindUserByID(userid);

                    byte[] salt = Authorize.GenerateSalt();
                    byte[] saltedHash = Authorize.GenerateSaltedHash(newPassword, salt);
                    Password password = new Password(saltedHash, salt);

                    bool operationSuccessful = user.ChangePassword(password);

                    if (operationSuccessful)
                    {
                        response = new ServerResponse(userid, Response.Success);
                        UserManager.Instance.SaveUser(user);
                    }
                    else
                    {
                        response = new ServerResponse(userid, Response.InvalidPassword);
                    }
                }
                else
                {
                    response = new ServerResponse(userid, Response.InvalidPassword);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new ServerResponse(userid, Response.ServerError);
            }

            return response;
        }
    }


    public class DeleteUser : UserMessage, Serializable
    {
        public ServerResponse Delete()
        {
            ServerResponse response = null;
            try
            {

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new ServerResponse(userid, Response.ServerError);
            }

            return response;
        }
    }
}
