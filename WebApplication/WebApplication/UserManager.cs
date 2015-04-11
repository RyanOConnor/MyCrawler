using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Builders;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

namespace WebApplication
{
    public class UserManager : ClientListener
    {
        public enum QueueStatus { createUser, findUserById, findUserByUserName, addLinkTouser, removeLinkFromUser, modifyUserLink, deleteUser };
        private Dictionary<ObjectId, SocketHandle> clients = new Dictionary<ObjectId, SocketHandle>();
        private ConcurrentQueue<Message> clientMessageQueue = new ConcurrentQueue<Message>();
        private static UserManager _instance;
        public static UserManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new UserManager();
                return _instance;
            }
        }

        public void Start()
        {
            this.MessageReceived += new EventHandler<MessageEventArgs>(ClientMessageReceived);
            this.StartListener();
        }

        public void AddClient(ObjectId userId, SocketHandle clientHandle)
        {
            lock(clients)
            {
                clients[userId] = clientHandle;
            }
        }

        public SocketHandle GetClient(ObjectId userId)
        {
            lock(clients)
            {
                return clients[userId];
            }
        }

        public void ClientMessageReceived(object sender, MessageEventArgs args)
        {
            SocketHandle clientHandle = (SocketHandle)sender;
            UserMessage userMessage = args.message as UserMessage;
            ObjectId userId = userMessage.userid;

            AddClient(userId, clientHandle);

            Message response = null;
            Console.WriteLine("[Received {0}]  {1}", userMessage.username, args.message.GetType().ToString());

            if(args.message is SyncRequest)
            {
                SyncRequest syncRequest = args.message as SyncRequest;
                response = syncRequest.RetrieveData();
            }
            else if(args.message is CreateUser)
            {
                CreateUser newUserMessage = args.message as CreateUser;
                response = newUserMessage.Create();
            }
            else if(args.message is AddLinkFeed)
            {
                AddLinkFeed linkFeed = args.message as AddLinkFeed;
                response = linkFeed.AddLink();
            }
            else if(args.message is AddTextUpdate)
            {
                AddTextUpdate textUpdate = args.message as AddTextUpdate;
                response = textUpdate.AddLink();
            }
            else if(args.message is ChangePassword)
            {
                ChangePassword changePassword = args.message as ChangePassword;
                response = changePassword.Start();
            }
            else if(args.message is ModifyItem)
            {
                ModifyItem modifyItem = args.message as ModifyItem;
                response = modifyItem.Modify();
            }
            else if(args.message is DeleteUser)
            {
                DeleteUser deleteUser = args.message as DeleteUser;
                response = deleteUser.Delete();
            }

            Console.WriteLine("\t[Sent {0}]  {1}", response.GetType().ToString(), userMessage.username);
            byte[] bson = BSON.Serialize<Message>(response);
            Send(clientHandle, bson);
        }

        public bool ValidateLoginAttempt(ObjectId userid, string username, byte[] enteredPassword)
        {
            User user = FindUserByID(userid);
            byte[] saltedHash = Authorize.GenerateSaltedHash(enteredPassword, user.password.passwordSalt);
            return Authorize.IsValidHash(saltedHash, user.password.passwordHash);
        }

        public User FindUserByID(ObjectId userid)
        {
            try
            {
                IMongoQuery queryUser = Query.EQ("_id", userid);
                User user = Database.Instance.userCollection.FindOne(queryUser);
                return user;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public void SaveUser(User user)
        {
            Database.Instance.userCollection.Save(user, WriteConcern.Acknowledged);
        }

        public void UpdateUsersByRecord(HtmlRecord record)
        {
            foreach(HtmlResults results in record.results.Values)
            {
                IMongoQuery query = Query.EQ("Links.v._id", results.id);
                MongoCursor<User> users = Database.Instance.userCollection.FindAs<User>(query);

                foreach(User user in users)
                {
                    // Send SyncData notification to all users associated with HtmlRecord object

                }
            }
        }

        public void DeleteUser(ObjectId userid, string userName, string passwordHash)
        {
            IMongoQuery query = Query.EQ("_id", userid);
            // Remove users information from crawl data
            Database.Instance.userCollection.Remove(query);
        }
    }
}
