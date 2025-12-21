using MongoDB.Driver;
using V2SubsCombinator.Models;

namespace V2SubsCombinator.Database
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration configuration)
        {
            var connectionString = configuration["CosmosDb:ConnectionString"];
            var databaseName = configuration["CosmosDb:DatabaseName"];

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);

            var indexKeys = Builders<ExportSub>.IndexKeys.Ascending(x => x.Suffix);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<ExportSub>(indexKeys, indexOptions);
            ExportSubs.Indexes.CreateOne(indexModel);
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
        public IMongoCollection<ImportSub> ImportSubs => _database.GetCollection<ImportSub>("ImportSubs");
        public IMongoCollection<ExportSub> ExportSubs => _database.GetCollection<ExportSub>("ExportSubs");
        public IMongoCollection<ExportSubGroup> ExportSubGroups => _database.GetCollection<ExportSubGroup>("ExportSubGroups");
    }
}