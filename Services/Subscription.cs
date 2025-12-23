using V2SubsCombinator.DTOs;
using V2SubsCombinator.IServices;

namespace V2SubsCombinator.Services
{
    public class Subscription : ISubscription
    {
        public Task<ExportSubGroupResult> AddExportSubGroupAsync(AddExportSubGroupRequest addExportSubGroupRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ExportSubResult> AddExportSubToExportSubGroupAsync(AddExportSubRequest addExportSubRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ImportSubResult> AddImportSubToExportSubGroupAsync(AddImportSubRequest addImportSubRequest)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetExportSubContentAsync(GetExportSubContentRequest getExportSubContentRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ExportSubGroupResult> GetExportSubGroupDetailAsync(GetExportSubGroupRequest getExportSubGroupRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ExportSubGroupResult> GetExportSubGroupsAsync(GetExportSubGroupRequest getExportSubGroupRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ExportSubResult> RemoveExportSubFromExportSubGroupAsync(RemoveExportSubRequest removeExportSubRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ExportSubGroupResult> RemoveExportSubGroupAsync(RemoveExportSubGroupRequest removeExportSubGroupRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ImportSubResult> RemoveImportSubFromExportSubGroupAsync(RemoveImportSubRequest removeImportSubRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ExportSubResult> UpdateExportSubAsync(UpdateExportSubRequest updateExportSubRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ExportSubGroupResult> UpdateExportSubGroupAsync(UpdateExportSubGroupRequest updateExportSubGroupRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ImportSubResult> UpdateImportSubAsync(UpdateImportSubRequest updateImportSubRequest)
        {
            throw new NotImplementedException();
        }
    }
}