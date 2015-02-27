using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using MongoDB.Driver;
using MongoDB.Bson;

namespace WebApplication
{
    class Authorize
    {
        public void generateSaltValue()
        {

        }

        public void hashPassword(string clearData, string saltValue, HashAlgorithm hash)
        {

        }

        public void validateHash(ObjectId userid, string hash)
        {

        }
    }
}
