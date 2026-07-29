using Reimaginate.Mapper;
using DataHubTask = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.TaskActivity;
using DataverseTask = DataverseModel.Task;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseTaskToDataHubTaskActivity : ITypeMapper<DataverseTask, DataHubTask>
{
    public Task<DataHubTask> MapAsync(DataverseTask from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubTask
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseTask.EntityLogicalName, from.Id),
            Subject = from.Subject,
            Description = from.Description,
            ScheduledStart = from.ScheduledStart,
            ScheduledEnd = from.ScheduledEnd,
            ScheduledDurationMinutes = from.ScheduledDurationMinutes,
            ActualStart = from.ActualStart,
            ActualEnd = from.ActualEnd,
            ActualDurationMinutes = from.ActualDurationMinutes,
            Owner = MappingHelpers.ToOwnerReference(from.OwnerId),
            Regarding = MappingHelpers.ToActivityReference(from.RegardingObjectId),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseTask.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseTask.Fields.ModifiedOn)
        });
    }
}
