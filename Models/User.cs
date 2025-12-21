using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace V2SubsCombinator.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("username")]
        [BsonRequired]
        public required string Username { get; set; }

        [BsonElement("passwordHash")]
        [BsonRequired]
        public required string PasswordHash { get; set; }

        [BsonElement("exportSubGroupIds")]
        public List<string> ExportSubGroupIds { get; set; } = [];
    }
}