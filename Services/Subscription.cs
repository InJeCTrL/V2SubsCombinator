using MongoDB.Driver;
using V2SubsCombinator.Database;
using V2SubsCombinator.DTOs;
using V2SubsCombinator.IServices;
using V2SubsCombinator.Models;
using V2SubsCombinator.Utils;

namespace V2SubsCombinator.Services
{
    public class Subscription(MongoDbContext dbContext) : ISubscription
    {
        private readonly MongoDbContext _dbContext = dbContext;

        public async Task<ExportSubGroupResult> GetExportSubGroupsAsync(GetExportSubGroupRequest request)
        {
            var groups = await _dbContext.ExportSubGroups
                .Find(g => g.UserId == request.UserId && (string.IsNullOrEmpty(request.Id) || g.Id == request.Id))
                .ToListAsync();

            var result = groups.Select(g => new ExportSubGroupBasicData
            {
                Id = g.Id,
                Name = g.Name,
                IsActive = g.IsActive,
                ImportSubCount = g.ImportSubIds.Count,
                ExportSubCount = g.ExportSubIds.Count
            }).ToList();

            return new ExportSubGroupResult { Success = true, ExportSubGroups = result };
        }

        public async Task<ExportSubGroupResult> GetExportSubGroupDetailAsync(GetExportSubGroupRequest request)
        {
            if (string.IsNullOrEmpty(request.Id))
            {
                return new ExportSubGroupResult { Success = false };
            }

            var group = await _dbContext.ExportSubGroups
                .Find(g => g.Id == request.Id && g.UserId == request.UserId)
                .FirstOrDefaultAsync();

            if (group == null)
            {
                return new ExportSubGroupResult { Success = false };
            }

            var importSubs = await _dbContext.ImportSubs
                .Find(s => group.ImportSubIds.Contains(s.Id))
                .ToListAsync();

            var exportSubs = await _dbContext.ExportSubs
                .Find(s => group.ExportSubIds.Contains(s.Id))
                .ToListAsync();

            var data = new ExportSubGroupBasicData
            {
                Id = group.Id,
                Name = group.Name,
                IsActive = group.IsActive,
                ImportSubCount = group.ImportSubIds.Count,
                ExportSubCount = group.ExportSubIds.Count,
                ImportSubData = [.. importSubs.Select(s => new ImportSubData
                {
                    Id = s.Id,
                    Url = s.Url,
                    Prefix = s.Prefix,
                    IsActive = s.IsActive
                })],
                ExportSubDataList = [.. exportSubs.Select(s => new ExportSubData
                {
                    Id = s.Id,
                    Suffix = s.Suffix,
                    Remark = s.Remark,
                    IsActive = s.IsActive
                })]
            };

            return new ExportSubGroupResult { Success = true, ExportSubGroups = [data] };
        }

        public async Task<ExportSubGroupResult> AddExportSubGroupAsync(AddExportSubGroupRequest request)
        {
            var newGroup = new ExportSubGroup
            {
                Name = request.Name,
                IsActive = request.IsActive,
                UserId = request.UserId
            };

            await _dbContext.ExportSubGroups.InsertOneAsync(newGroup);

            return new ExportSubGroupResult
            {
                Success = true,
                ExportSubGroups = [new ExportSubGroupBasicData
                {
                    Id = newGroup.Id,
                    Name = newGroup.Name,
                    IsActive = newGroup.IsActive,
                    ImportSubCount = 0,
                    ExportSubCount = 0
                }]
            };
        }

        public async Task<ImportSubResult> AddImportSubToExportSubGroupAsync(AddImportSubRequest request)
        {
            var group = await _dbContext.ExportSubGroups
                .Find(g => g.Id == request.ExportSubGroupId && g.UserId == request.UserId)
                .FirstOrDefaultAsync();

            if (group == null)
            {
                return new ImportSubResult { Success = false };
            }

            var newImportSub = new ImportSub
            {
                Url = request.Url,
                Prefix = request.Prefix,
                IsActive = request.IsActive,
                ExportSubGroupId = request.ExportSubGroupId,
                UserId = request.UserId
            };

            await _dbContext.ImportSubs.InsertOneAsync(newImportSub);

            var update = Builders<ExportSubGroup>.Update.Push(g => g.ImportSubIds, newImportSub.Id);
            await _dbContext.ExportSubGroups.UpdateOneAsync(g => g.Id == request.ExportSubGroupId, update);

            return new ImportSubResult { Success = true };
        }

        public async Task<ExportSubResult> AddExportSubToExportSubGroupAsync(AddExportSubRequest request)
        {
            var group = await _dbContext.ExportSubGroups
                .Find(g => g.Id == request.ExportSubGroupId && g.UserId == request.UserId)
                .FirstOrDefaultAsync();

            if (group == null)
            {
                return new ExportSubResult { Success = false };
            }

            var newExportSub = new ExportSub
            {
                Suffix = request.Suffix,
                Remark = request.Remark,
                IsActive = request.Isactive,
                ExportSubGroupId = request.ExportSubGroupId,
                UserId = request.UserId
            };

            try
            {
                await _dbContext.ExportSubs.InsertOneAsync(newExportSub);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return new ExportSubResult { Success = false };
            }

            var update = Builders<ExportSubGroup>.Update.Push(g => g.ExportSubIds, newExportSub.Id);
            await _dbContext.ExportSubGroups.UpdateOneAsync(g => g.Id == request.ExportSubGroupId, update);

            return new ExportSubResult { Success = true };
        }

        public async Task<ImportSubResult> RemoveImportSubFromExportSubGroupAsync(RemoveImportSubRequest request)
        {
            var importSub = await _dbContext.ImportSubs
                .Find(s => s.Id == request.Id && s.UserId == request.UserId)
                .FirstOrDefaultAsync();

            if (importSub == null)
            {
                return new ImportSubResult { Success = false };
            }

            var update = Builders<ExportSubGroup>.Update.Pull(g => g.ImportSubIds, request.Id);
            await _dbContext.ExportSubGroups.UpdateOneAsync(g => g.Id == importSub.ExportSubGroupId, update);

            await _dbContext.ImportSubs.DeleteOneAsync(s => s.Id == request.Id);

            return new ImportSubResult { Success = true };
        }

        public async Task<ExportSubResult> RemoveExportSubFromExportSubGroupAsync(RemoveExportSubRequest request)
        {
            var exportSub = await _dbContext.ExportSubs
                .Find(s => s.Id == request.Id && s.UserId == request.UserId)
                .FirstOrDefaultAsync();

            if (exportSub == null)
                return new ExportSubResult { Success = false };

            var update = Builders<ExportSubGroup>.Update.Pull(g => g.ExportSubIds, request.Id);
            await _dbContext.ExportSubGroups.UpdateOneAsync(g => g.Id == exportSub.ExportSubGroupId, update);

            await _dbContext.ExportSubs.DeleteOneAsync(s => s.Id == request.Id);

            return new ExportSubResult { Success = true };
        }

        public async Task<ExportSubGroupResult> RemoveExportSubGroupAsync(RemoveExportSubGroupRequest request)
        {
            var group = await _dbContext.ExportSubGroups
                .Find(g => g.Id == request.Id && g.UserId == request.UserId)
                .FirstOrDefaultAsync();

            if (group == null)
            {
                return new ExportSubGroupResult { Success = false };
            }

            await _dbContext.ImportSubs.DeleteManyAsync(s => group.ImportSubIds.Contains(s.Id));
            await _dbContext.ExportSubs.DeleteManyAsync(s => group.ExportSubIds.Contains(s.Id));
            await _dbContext.ExportSubGroups.DeleteOneAsync(g => g.Id == request.Id);

            return new ExportSubGroupResult { Success = true };
        }

        public async Task<ExportSubGroupResult> UpdateExportSubGroupAsync(UpdateExportSubGroupRequest request)
        {
            var updateDef = Builders<ExportSubGroup>.Update;
            var updates = new List<UpdateDefinition<ExportSubGroup>>();

            if (request.Name != null)
                updates.Add(updateDef.Set(g => g.Name, request.Name));
            if (request.IsActive.HasValue)
                updates.Add(updateDef.Set(g => g.IsActive, request.IsActive.Value));

            if (updates.Count == 0)
                return new ExportSubGroupResult { Success = false };

            var result = await _dbContext.ExportSubGroups.UpdateOneAsync(
                g => g.Id == request.Id && g.UserId == request.UserId,
                updateDef.Combine(updates));

            return new ExportSubGroupResult { Success = result.ModifiedCount > 0 };
        }

        public async Task<ExportSubResult> UpdateExportSubAsync(UpdateExportSubRequest request)
        {
            var updateDef = Builders<ExportSub>.Update;
            var updates = new List<UpdateDefinition<ExportSub>>();

            if (request.Suffix != null)
                updates.Add(updateDef.Set(s => s.Suffix, request.Suffix));
            if (request.Remark != null)
                updates.Add(updateDef.Set(s => s.Remark, request.Remark));
            if (request.Isactive.HasValue)
                updates.Add(updateDef.Set(s => s.IsActive, request.Isactive.Value));

            if (updates.Count == 0)
                return new ExportSubResult { Success = false };

            try
            {
                var result = await _dbContext.ExportSubs.UpdateOneAsync(
                    s => s.Id == request.Id && s.UserId == request.UserId,
                    updateDef.Combine(updates));

                return new ExportSubResult { Success = result.ModifiedCount > 0 };
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return new ExportSubResult { Success = false };
            }
        }

        public async Task<ImportSubResult> UpdateImportSubAsync(UpdateImportSubRequest request)
        {
            var updateDef = Builders<ImportSub>.Update;
            var updates = new List<UpdateDefinition<ImportSub>>();

            if (request.Url != null)
                updates.Add(updateDef.Set(s => s.Url, request.Url));
            if (request.Prefix != null)
                updates.Add(updateDef.Set(s => s.Prefix, request.Prefix));
            if (request.IsActive.HasValue)
                updates.Add(updateDef.Set(s => s.IsActive, request.IsActive.Value));

            if (updates.Count == 0)
                return new ImportSubResult { Success = false };

            var result = await _dbContext.ImportSubs.UpdateOneAsync(
                s => s.Id == request.Id && s.UserId == request.UserId,
                updateDef.Combine(updates));

            return new ImportSubResult { Success = result.ModifiedCount > 0 };
        }

        public async Task<string> GetExportSubContentAsync(GetExportSubContentRequest request)
        {
            var exportSub = await _dbContext.ExportSubs
                .Find(s => s.Suffix == request.Suffix && s.IsActive)
                .FirstOrDefaultAsync();

            if (exportSub == null)
                return string.Empty;

            var group = await _dbContext.ExportSubGroups
                .Find(g => g.Id == exportSub.ExportSubGroupId && g.IsActive)
                .FirstOrDefaultAsync();

            if (group == null)
                return string.Empty;

            var importSubs = await _dbContext.ImportSubs
                .Find(s => group.ImportSubIds.Contains(s.Id) && s.IsActive)
                .ToListAsync();

            var subscriptions = importSubs.Select(s => (s.Url, s.Prefix));

            return await V2SubsHelper.FetchAndCombineSubscriptionsAsync(subscriptions, request.IsClash);
        }
    }
}
