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
        public ObjectId userId { get; set; }
    }

    public class SyncRequest : UserMessage, Serializable
    {
        ObjectId itemId { get; set; }

        public SyncData RetrieveData()
        {
            SyncData response = null;
            try
            {
                if (itemId != ObjectId.Empty)
                {
                    HtmlResults results = DataManager.Instance.ManualRequest(itemId);
                    response = new SyncData(userId, results);
                }
                else
                    response = new SyncData(userId, null);
            }
            catch(Exception ex)
            {
                response = new SyncData(userId, null);
                throw ex;
            }

            return response;
        }
    }

    public class SyncData : UserMessage, Serializable
    {
        public HtmlResults userData { get; set; }

        public SyncData(ObjectId id, HtmlResults results)
        {
            userId = id;
            userData = results;
        }
    }

    public class ServerResponse : Message, Serializable
    {
        public ObjectId userId { get; set; }
        public ObjectId jobId { get; set; }
        public Response serverResponse { get; set; }

        public ServerResponse(ObjectId user, Response response)
        {
            userId = user;
            serverResponse = response;
        }

        public void AddJobId(ObjectId jobId)
        {
            this.jobId = jobId;
        }
    }

    public class CreateUser : UserMessage, Serializable
    {
        public string username { get; set; }
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

                    User newUser = new User(username, password);
                    try
                    {
                        UserManager.Instance.SaveUser(newUser);
                        response = new ServerResponse(newUser.id, Response.UserCreated);
                    }
                    catch(MongoWriteConcernException)
                    {
                        response = new ServerResponse(userId, Response.UserAlreadyExists);
                    }
                }
                else
                {
                    response = new ServerResponse(userId, Response.InvalidPassword);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new ServerResponse(userId, Response.ServerError);
                throw ex;
            }

            return response;
        }
    }

    public class AddLinkFeed : UserMessage, Serializable
    {
        public string url { get; set; }
        public List<string> htmlTags { get; set; }
        public List<string> keywords { get; set; }

        public ServerResponse AddLink()
        {
            ServerResponse response = null;

            try
            {
                User user = UserManager.Instance.FindUserByID(userId);
                ObjectId itemId = ObjectId.Empty;
                try
                {
                    itemId = user.AddLinkFeed(url, htmlTags, keywords);
                }
                catch(Exception)
                {
                    response = new ServerResponse(userId, Response.LinkAlreadyExist);
                }

                if (itemId != null)
                {
                    UserManager.Instance.SaveUser(user);
                    response = new ServerResponse(user.id, Response.LinkAdded);
                    response.AddJobId(itemId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new ServerResponse(userId, Response.ServerError);
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
                User user = UserManager.Instance.FindUserByID(this.userId);

                ObjectId itemId = user.AddTextUpdate(url, htmlTags, innerText);

                if (itemId != ObjectId.Empty)
                {
                    UserManager.Instance.SaveUser(user);
                    response = new ServerResponse(user.id, Response.Success);
                    response.AddJobId(itemId);
                }
                else
                {
                    response = new ServerResponse(user.id, Response.RecordAlreadyExists);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new ServerResponse(userId, Response.ServerError);
            }

            return response;
        }
    }

    public class ModifyItem : UserMessage, Serializable
    {
        public ObjectId itemId { get; set; }
        public HtmlResults userResults { get; set; }

        public ServerResponse Modify()
        {
            ServerResponse response = null;
            try
            {
                User user = UserManager.Instance.FindUserByID(this.userId);
                ObjectId itemId = ObjectId.Empty;

                if(userResults.GetType() == typeof(LinkFeed))
                {
                    itemId = user.ModifyLinkFeed(itemId, userResults as LinkFeed);
                }
                else if(userResults.GetType() == typeof(TextUpdate))
                {
                    itemId = user.ModifyTextUpdate(itemId, userResults as TextUpdate);
                }

                if (itemId != ObjectId.Empty)
                {
                    response = new ServerResponse(user.id, Response.Success);
                    response.AddJobId(itemId);
                }
                else
                {
                    response = new ServerResponse(user.id, Response.ServerError);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new ServerResponse(userId, Response.ServerError);
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
                User user = UserManager.Instance.FindUserByID(this.userId);
                user.RemoveLink(itemId);
                UserManager.Instance.SaveUser(user);

                response = new ServerResponse(userId, Response.Success);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new ServerResponse(userId, Response.ServerError);
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
                    User user = UserManager.Instance.FindUserByID(userId);

                    byte[] salt = Authorize.GenerateSalt();
                    byte[] saltedHash = Authorize.GenerateSaltedHash(newPassword, salt);
                    Password password = new Password(saltedHash, salt);

                    bool operationSuccessful = user.ChangePassword(password);

                    if (operationSuccessful)
                    {
                        response = new ServerResponse(userId, Response.Success);
                        UserManager.Instance.SaveUser(user);
                    }
                    else
                    {
                        response = new ServerResponse(userId, Response.InvalidPassword);
                    }
                }
                else
                {
                    response = new ServerResponse(userId, Response.InvalidPassword);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                response = new ServerResponse(userId, Response.ServerError);
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
                response = new ServerResponse(userId, Response.ServerError);
            }

            return response;
        }
    }
}
