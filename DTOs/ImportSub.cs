namespace V2SubsCombinator.DTOs
{
    public class RemoveImportSubRequest : SubscriptionRequestBase
    {
        public required string Id { get; set; }
    }

    public class RefreshImportSubCacheRequest : SubscriptionRequestBase
    {
        public required string Id { get; set; }
    }

public class UpdateImportSubRequest : SubscriptionRequestBase
    {
        public required string Id { get; set; }
        public string? Url { get; set; }
        public string? Prefix { get; set; }
        public bool? IsActive { get; set; }
        public string? FixedContent { get; set; }
        public bool? UseCache { get; set; }
    }

public class AddImportSubRequest : SubscriptionRequestBase
    {
        public required string ExportSubGroupId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string? FixedContent { get; set; }
        public bool UseCache { get; set; }
    }

public class ImportSubData
    {
        public required string Id { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public required bool IsActive { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? FixedContent { get; set; }
        public bool UseCache { get; set; }
        public DateTime? CachedAt { get; set; }
    }

    public class ImportSubResult
    {
        public required bool Success { get; set; }
        public string? Message { get; set; }
        public DateTime? CachedAt { get; set; }
    }
}
