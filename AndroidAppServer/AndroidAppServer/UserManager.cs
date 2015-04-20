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

        public bool ValidateLoginAttempt(string username, byte[] enteredPassword)
        {
            User user = Database.Instance.GetUserCollection().FindOneAs<User>(Query.EQ("username", username));
            byte[] saltedHash = Authorize.GenerateSaltedHash(enteredPassword, user.password.passwordSalt);
            return Authorize.IsValidHash(saltedHash, user.password.passwordHash);
        }

        public void SaveUser(User user)
        {
            Database.Instance.userCollection.Save(user, WriteConcern.Acknowledged);
        }

        public User FindUserByID(ObjectId userid)
        {
            try
            {
                IMongoQuery queryUser = Query.EQ("_id", userid);
                User user = Database.Instance.userCollection.FindOne(queryUser);
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }
    }
}
