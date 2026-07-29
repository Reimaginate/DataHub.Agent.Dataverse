using Reimaginate.Mapper;
using DataHubActivity = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Activity;
using DataverseActivity = DataverseModel.ActivityPointer;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseActivityPointerToDataHubActivity : ITypeMapper<DataverseActivity, DataHubActivity>
{
    public Task<DataHubActivity> MapAsync(
        DataverseActivity from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubActivity
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseActivity.EntityLogicalName, from.Id),
            ActivityType = from.ActivityTypeCode,
            Subject = from.Subject,
            Description = from.Description,
            ScheduledStart = from.ScheduledStart,
            ScheduledEnd = from.ScheduledEnd,
            ScheduledDurationMinutes = from.ScheduledDurationMinutes,
            ActualStart = from.ActualStart,
            ActualEnd = from.ActualEnd,
            ActualDurationMinutes = from.ActualDurationMinutes,
            State = from.StateCode?.ToString(),
            Status = from.StatusCode?.ToString(),
            Owner = MappingHelpers.ToOwnerReference(from.OwnerId),
            Regarding = MappingHelpers.ToActivityReference(from.RegardingObjectId),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseActivity.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseActivity.Fields.ModifiedOn)
        });
    }
}
