namespace V2SubsCombinator.DTOs
{
    public class GetExportSubGroupRequest : SubscriptionRequestBase
    {
        public string? Id { get; set; }
    }

    public class RemoveExportSubGroupRequest : SubscriptionRequestBase
    {
        public required string Id { get; set; }
    }

    public class UpdateExportSubGroupRequest : SubscriptionRequestBase
    {
        public required string Id { get; set; }
        public string? Name { get; set; }
        public bool? IsActive { get; set; }
    }

    public class AddExportSubGroupRequest : SubscriptionRequestBase
    {
        public required string Name { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ExportSubGroupBasicData
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required bool IsActive { get; set; }
        public required int ImportSubCount { get; set; }
        public required int ExportSubCount { get; set; }
        public List<ExportSubData>? ExportSubDataList { get; set; }
        public List<ImportSubData>? ImportSubData { get; set; }
    }

    public class ExportSubsDetailData
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required bool IsActive { get; set; }
    }

    public class ExportSubGroupResult
    {
        public required bool Success { get; set; }
        public List<ExportSubGroupBasicData>? ExportSubGroups { get; set; }
    }
}