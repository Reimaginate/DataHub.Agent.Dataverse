using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using Reimaginate.Mapper;
using DataHubPhoneCall = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PhoneCall;
using DataversePhoneCall = DataverseModel.PhoneCall;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataversePhoneCallToDataHubPhoneCall : ITypeMapper<DataversePhoneCall, DataHubPhoneCall>
{
    public Task<DataHubPhoneCall> MapAsync(DataversePhoneCall from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubPhoneCall
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataversePhoneCall.EntityLogicalName, from.Id),
            Subject = from.Subject,
            Description = from.Description,
            Direction = from.DirectionCode switch { true => ActivityDirection.Outgoing, false => ActivityDirection.Incoming, _ => null },
            PhoneNumber = from.PhoneNumber,
            ScheduledStart = from.ScheduledStart,
            ScheduledEnd = from.ScheduledEnd,
            ScheduledDurationMinutes = from.ScheduledDurationMinutes,
            ActualStart = from.ActualStart,
            ActualEnd = from.ActualEnd,
            ActualDurationMinutes = from.ActualDurationMinutes,
            Owner = MappingHelpers.ToOwnerReference(from.OwnerId),
            Regarding = MappingHelpers.ToActivityReference(from.RegardingObjectId),
            From = MappingHelpers.ToActivityParties(from.From, ActivityPartyRole.Sender),
            To = MappingHelpers.ToActivityParties(from.To, ActivityPartyRole.ToRecipient),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataversePhoneCall.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataversePhoneCall.Fields.ModifiedOn)
        });
    }
}
