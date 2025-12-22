using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace V2SubsCombinator.Models
{
    public class ExportSub
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("suffix")]
        [BsonRequired]
        public required string Suffix { get; set; }

        [BsonElement("remark")]
        public string Remark { get; set; } = string.Empty;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("exportSubGroupId")]
        [BsonRequired]
        public required string ExportSubGroupId { get; set; }

        [BsonElement("userId")]
        [BsonRequired]
        public required string UserId { get; set; }
    }

    public class ExportSubGroup
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("name")]
        [BsonRequired]
        public required string Name { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("exportSubIds")]
        public List<string> ExportSubIds { get; set; } = [];

        [BsonElement("importSubIds")]
        public List<string> ImportSubIds { get; set; } = [];

        [BsonElement("userId")]
        [BsonRequired]
        public required string UserId { get; set; }
    }
}