using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecords;

public class UpdateDataverseRecordsCommandHandler(IDataverseDataService idataverseDataService) : IHandler<UpdateDataverseRecordsCommand, UpdateDataverseRecordsResponse<Microsoft.Xrm.Sdk.Entity>>
{
    public async Task<UpdateDataverseRecordsResponse<Entity>> HandleAsync(UpdateDataverseRecordsCommand command, CancellationToken cancellationToken)
    {
        var updateResponses = await idataverseDataService.UpdateAsync(command.Records, cancellationToken: cancellationToken, disableRowVersionCheck: command.DisableRowVersionCheck.GetValueOrDefault(false));

        var ret = new UpdateDataverseRecordsResponse<Entity>()
        {
            HasErrors = updateResponses.Any(a => !a.Value.Success),
            Results = updateResponses.ToDictionary(k => k.Key, v => new UpdateResult<Entity>()
            {
                EntityId = v.Value.EntityId,
                Success = v.Value.Success,
                FailureReason = v.Value.Error
            })
        };

        if (command.AutoFetchUpdatedEntities)
        {
            var successfulUpdates = ret.Results.Where(w => w.Value.Success).ToDictionary(k => k.Value.EntityId!.Value, v => v);
            if (successfulUpdates.Any())
            {
                var entityLogicalName = command.Records.Values.First().LogicalName;
                var entityIds = successfulUpdates.Keys.ToList();
                
                var updatedRecords = await idataverseDataService.GetAsync(entityLogicalName, entityIds, columns: new ColumnSet(true), cancellationToken: cancellationToken);
               
                updatedRecords.ForEach(updatedRecord =>
                {
                    var update = successfulUpdates[updatedRecord.Id];
                    update.Value.ResultingEntity = updatedRecord;
                });
            }
        }

        return ret;
    }
}

public class UpdateDataverseRecordsCommandHandler<TDataverseEntity>(IDataverseDataService idataverseDataService, ITimeService timeService)
    : IHandler<UpdateDataverseRecordsCommand<TDataverseEntity>, UpdateDataverseRecordsResponse<TDataverseEntity>>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity, new()
{
    public async Task<UpdateDataverseRecordsResponse<TDataverseEntity>> HandleAsync(UpdateDataverseRecordsCommand<TDataverseEntity> command, CancellationToken cancellationToken)
    {
        var updateTimestamp = timeService.Now().UtcDateTime;
        updateTimestamp = updateTimestamp.AddMilliseconds(-updateTimestamp.Millisecond); // Align with Dataverse date precision

        var updateResponses = await idataverseDataService.UpdateAsync(command.Records, cancellationToken: cancellationToken);
        var ret = new UpdateDataverseRecordsResponse<TDataverseEntity>()
        {
            HasErrors = updateResponses.Any(a => !a.Value.Success),
            Results = updateResponses.ToDictionary(k => k.Key, v => new UpdateResult<TDataverseEntity>()
            {
                EntityId = v.Value.EntityId,
                Success = v.Value.Success,
                FailureReason = v.Value.Error
            })
        };

        if (command.AutoFetchUpdatedEntities)
        {
            var successfulUpdates = ret.Results.Where(w => w.Value.Success).ToDictionary(k => k.Value.EntityId!.Value, v => v);
            if (successfulUpdates.Any())
            {
                var entityLogicalName = command.Records.Values.First().LogicalName;
                var entityIds = successfulUpdates.Keys.ToList();

                var updatedRecords = await idataverseDataService.GetAsync(entityLogicalName, entityIds, columns: new ColumnSet(true), cancellationToken: cancellationToken);

                updatedRecords.ForEach(updatedRecord =>
                {
                    var update = successfulUpdates[updatedRecord.Id];
                    update.Value.ResultingEntity = (TDataverseEntity)updatedRecord;
                });
            }
        }
        else
        {
            foreach (var record in ret.Results)
            {
                var originalEntity = command.Records[record.Key];
                record.Value.ResultingEntity = new TDataverseEntity
                {
                    Id = originalEntity.Id,
                    Attributes =
                    {
                        ["modifiedon"] = updateTimestamp
                    }
                };
            }
        }
        return ret;
    }
}

