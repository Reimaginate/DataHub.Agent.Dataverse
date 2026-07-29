using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using Reimaginate.Mapper;
using DataHubPhoneCall = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PhoneCall;
using DataversePhoneCall = DataverseModel.PhoneCall;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataHubPhoneCallToDataversePhoneCall : ITypeMapper<DataHubPhoneCall, DataversePhoneCall>
{
    public Task<DataversePhoneCall> MapAsync(DataHubPhoneCall from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        var mapped = new DataversePhoneCall
        {
            Subject = from.Subject,
            Description = from.Description,
            DirectionCode = from.Direction switch { ActivityDirection.Outgoing => true, ActivityDirection.Incoming => false, _ => null },
            PhoneNumber = from.PhoneNumber,
            ScheduledStart = from.ScheduledStart,
            ScheduledEnd = from.ScheduledEnd,
            ScheduledDurationMinutes = from.ScheduledDurationMinutes,
            ActualStart = from.ActualStart,
            ActualEnd = from.ActualEnd,
            ActualDurationMinutes = from.ActualDurationMinutes,
            OwnerId = MappingHelpers.ResolveOwner(from.Owner, cache),
            RegardingObjectId = MappingHelpers.ResolveActivityReference(from.Regarding, cache),
            From = MappingHelpers.ToDataverseActivityParties(from.From, ActivityPartyRole.Sender, cache),
            To = MappingHelpers.ToDataverseActivityParties(from.To, ActivityPartyRole.ToRecipient, cache)
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataversePhoneCall.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.ActivityId = dataverseId;
        }

        return Task.FromResult(mapped);
    }
}
