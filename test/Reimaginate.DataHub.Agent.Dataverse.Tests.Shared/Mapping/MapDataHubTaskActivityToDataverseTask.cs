using Reimaginate.Mapper;
using DataHubTask = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.TaskActivity;
using DataverseTask = DataverseModel.Task;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataHubTaskActivityToDataverseTask : ITypeMapper<DataHubTask, DataverseTask>
{
    public Task<DataverseTask> MapAsync(DataHubTask from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        var mapped = new DataverseTask
        {
            Subject = from.Subject,
            Description = from.Description,
            ScheduledStart = from.ScheduledStart,
            ScheduledEnd = from.ScheduledEnd,
            ScheduledDurationMinutes = from.ScheduledDurationMinutes,
            ActualStart = from.ActualStart,
            ActualEnd = from.ActualEnd,
            ActualDurationMinutes = from.ActualDurationMinutes,
            OwnerId = MappingHelpers.ResolveOwner(from.Owner, cache),
            RegardingObjectId = MappingHelpers.ResolveActivityReference(from.Regarding, cache)
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseTask.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.ActivityId = dataverseId;
        }

        return Task.FromResult(mapped);
    }
}
