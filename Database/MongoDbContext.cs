using MongoDB.Driver;
using V2SubsCombinator.Models;

namespace V2SubsCombinator.Database
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("MongoDB connection string is not configured.");
            var databaseName = configuration.GetConnectionString("Database")
                ?? throw new InvalidOperationException("MongoDB connection Database is not configured.");;

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);

            var indexOptions = new CreateIndexOptions { Unique = true };

            var exportSubIndexKeys = Builders<ExportSub>.IndexKeys.Ascending(x => x.Suffix);
            var exportSubIndexModel = new CreateIndexModel<ExportSub>(exportSubIndexKeys, indexOptions);
            ExportSubs.Indexes.CreateOne(exportSubIndexModel);

            var userIndexKeys = Builders<User>.IndexKeys.Ascending(x => x.Username);
            var userIndexModel = new CreateIndexModel<User>(userIndexKeys, indexOptions);
            Users.Indexes.CreateOne(userIndexModel);
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
        public IMongoCollection<ImportSub> ImportSubs => _database.GetCollection<ImportSub>("ImportSubs");
        public IMongoCollection<ExportSub> ExportSubs => _database.GetCollection<ExportSub>("ExportSubs");
        public IMongoCollection<ExportSubGroup> ExportSubGroups => _database.GetCollection<ExportSubGroup>("ExportSubGroups");
    }
}