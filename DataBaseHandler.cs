using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace DrakesBasketballCourtServer
{
    public class DataBaseHandler
    {
        IGridFSBucket gridFS;
        IMongoCollection<DBAccountData> Players;

        public DataBaseHandler()
        {
            // строка подключения
            string connectionString = "mongodb://127.0.0.1:27017/thesanctuary";

            var connection = new MongoUrlBuilder(connectionString);
            // получаем клиента для взаимодействия с базой данных
            MongoClient client = new MongoClient(connectionString);
            // получаем доступ к самой базе данных
            IMongoDatabase database = client.GetDatabase(connection.DatabaseName);
            // получаем доступ к файловому хранилищу
            gridFS = new GridFSBucket(database);
            // обращаемся к коллекции Products
            Players = database.GetCollection<DBAccountData>("players");
        }

        public async Task Create(DBAccountData playerData)
        {
            await Players.InsertOneAsync(playerData);
        }

        public bool Check(string Login)
        {
            var builder = new FilterDefinitionBuilder<DBAccountData>();
            var filter = builder.Empty;

            if (!String.IsNullOrWhiteSpace(Login))
            {
                filter = filter & builder.Regex("login", new BsonRegularExpression(Login));
            }

            var Data = Players.Find(filter).FirstOrDefault();

            if (Data == null) return false;
            else return true;
        }

        public DBAccountData Get(string Login)
        {
            var builder = new FilterDefinitionBuilder<DBAccountData>();
            var filter = builder.Empty;

            if (!string.IsNullOrWhiteSpace(Login))
            {
                filter = filter & builder.Regex("login", new BsonRegularExpression(Login));
            }

            var Data = Players.Find(filter).FirstOrDefault();

            if (Data == null)
            {
                return null;
            }

            else
            {
                return Data;
            }
        }

        public DBAccountData Get(string Login, string Password)
        {
            var builder = new FilterDefinitionBuilder<DBAccountData>();
            var filter = builder.Empty;

            if(!String.IsNullOrWhiteSpace(Login) && !String.IsNullOrWhiteSpace(Password))
            {
                filter = filter & builder.Regex("login", new BsonRegularExpression(Login)) & builder.Regex("password", new BsonRegularExpression(Password));
            }

            var Data = Players.Find(filter).FirstOrDefault();

            if(Data == null)
            {
                return null;
            }

            else
            {
                return Data;
            }
        }

        public async Task Update(DBAccountData data)
        {
            await Players.ReplaceOneAsync(new BsonDocument("_id", new ObjectId(data.Id)), data);
        }
    }
}
