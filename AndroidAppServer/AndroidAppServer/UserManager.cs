using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace AndroidAppServer
{
    public class UserManager
    {
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

        public ObjectId ValidateLoginAttempt(string username, byte[] enteredPassword)
        {
            User user = Database.Instance.userCollection.FindOneAs<User>(Query.EQ("username", username));
            if (user != null)
            {
                byte[] saltedHash = Authorize.GenerateSaltedHash(enteredPassword, user.password.salt);

                if (Authorize.IsValidHash(saltedHash, user.password.hash))
                    return user.id;
                else
                    return ObjectId.Empty;
            }
            else
            {
                return ObjectId.Empty;
            }
        }

        public void SaveUser(User user)
        {
            Database.Instance.userCollection.Save(user, WriteConcern.Acknowledged);
        }

        public bool DeleteUser(ObjectId userid)
        {
            User user = FindUserByID(userid);
            user.RemoveAllLinks();

            IMongoQuery query = Query.EQ("_id", userid);
            WriteConcernResult concern = Database.Instance.userCollection.Remove(query, WriteConcern.Acknowledged);
            if (concern.Ok)
                return true;
            else
                return false;
        }

        public User FindUserByID(ObjectId userid)
        {
            IMongoQuery query = Query.EQ("_id", userid);
            User user = Database.Instance.userCollection.FindOne(query);
            return user;
        }
    }
}
