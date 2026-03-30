using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace V2SubsCombinator.Models
{
public class ImportSub
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; } = null!;

            [BsonElement("url")]
            [BsonRequired]
            public required string Url { get; set; }

            [BsonElement("prefix")]
            public string Prefix { get; set; } = string.Empty;

            [BsonElement("isActive")]
            public bool IsActive { get; set; } = true;

            [BsonElement("exportSubGroupId")]
            [BsonRequired]
            public required string ExportSubGroupId { get; set; }

            [BsonElement("userId")]
            [BsonRequired]
            public required string UserId { get; set; }

            [BsonElement("cacheDurationMinutes")]
            public int CacheDurationMinutes { get; set; } = 60;

            [BsonElement("cachedNodes")]
            public List<SupportedNode>? CachedNodes { get; set; }

            [BsonElement("cacheExpiresAt")]
            public DateTime? CacheExpiresAt { get; set; }

            [BsonIgnore]
            public bool IsCacheEnabled => CacheDurationMinutes > 0;
        }
}