using V2SubsCombinator.DTOs;

namespace V2SubsCombinator.IServices
{
    public interface ISubscription
    {
        public Task<ExportSubGroupResult> GetExportSubGroupsAsync(GetExportSubGroupRequest getExportSubGroupRequest);
        public Task<ExportSubGroupResult> GetExportSubGroupDetailAsync(GetExportSubGroupRequest getExportSubGroupRequest);
        public Task<ExportSubGroupResult> AddExportSubGroupAsync(AddExportSubGroupRequest addExportSubGroupRequest);
        public Task<ImportSubResult> AddImportSubToExportSubGroupAsync(AddImportSubRequest addImportSubRequest);
        public Task<ExportSubResult> AddExportSubToExportSubGroupAsync(AddExportSubRequest addExportSubRequest);
        public Task<ImportSubResult> RemoveImportSubFromExportSubGroupAsync(RemoveImportSubRequest removeImportSubRequest);
        public Task<ExportSubResult> RemoveExportSubFromExportSubGroupAsync(RemoveExportSubRequest removeExportSubRequest);
        public Task<ExportSubGroupResult> RemoveExportSubGroupAsync(RemoveExportSubGroupRequest removeExportSubGroupRequest);
        public Task<ExportSubGroupResult> UpdateExportSubGroupAsync(UpdateExportSubGroupRequest updateExportSubGroupRequest);
        public Task<ExportSubResult> UpdateExportSubAsync(UpdateExportSubRequest updateExportSubRequest);
        public Task<ImportSubResult> UpdateImportSubAsync(UpdateImportSubRequest updateImportSubRequest);
        public Task<string> GetExportSubContentAsync(GetExportSubContentRequest getExportSubContentRequest);
    }
}