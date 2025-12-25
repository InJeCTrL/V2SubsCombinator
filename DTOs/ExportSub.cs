namespace V2SubsCombinator.DTOs
{
    public class GetExportSubContentRequest
    {
        public required string Suffix { get; set; }
        public bool IsClash { get; set; } = false;
    }

    public class RemoveExportSubRequest : SubscriptionRequestBase
    {
        public required string Id { get; set; }
    }

    public class UpdateExportSubRequest : SubscriptionRequestBase
    {
        public required string Id { get; set; }
        public string? Suffix { get; set; }
        public string? Remark { get; set; }
        public bool? Isactive { get; set; }
    }

    public class AddExportSubRequest : SubscriptionRequestBase
    {
        public required string ExportSubGroupId { get; set; }
        public required string Suffix { get; set; }
        public string Remark { get; set; } = string.Empty;
        public bool Isactive { get; set; } = true;
    }

    public class ExportSubData
    {
        public required string Id { get; set; }
        public required string Suffix { get; set; }
        public required bool IsActive { get; set; }
        public string Remark { get; set; } = string.Empty;
    }

    public class ExportSubResult
    {
        public required bool Success { get; set; }
    }
}