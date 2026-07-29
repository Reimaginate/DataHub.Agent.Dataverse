using Reimaginate.Mapper;
using DataHubEmail = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Email;
using DataverseEmail = DataverseModel.Email;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataHubEmailToDataverseEmail : ITypeMapper<DataHubEmail, DataverseEmail>
{
    public Task<DataverseEmail> MapAsync(
        DataHubEmail from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var mapped = new DataverseEmail
        {
            Subject = from.Subject,
            Description = from.Description,
            DirectionCode = from.Direction switch
            {
                Models.DataHub.EmailDirection.Outgoing => true,
                Models.DataHub.EmailDirection.Incoming => false,
                _ => null
            },
            ScheduledStart = from.ScheduledStart,
            ScheduledEnd = from.ScheduledEnd,
            ActualStart = from.ActualStart,
            ActualEnd = from.ActualEnd,
            ActualDurationMinutes = from.ActualDurationMinutes,
            TrackingToken = from.TrackingToken,
            OwnerId = MappingHelpers.ResolveOwner(from.Owner, cache),
            RegardingObjectId = MappingHelpers.ResolveActivityReference(from.Regarding, cache),
            ParentActivityId = MappingHelpers.ResolveActivityReference(from.ParentEmail, cache),
            CorrelatedActivityId = MappingHelpers.ResolveActivityReference(from.CorrelatedEmail, cache),
            From = MappingHelpers.ToDataverseActivityParties(from.From, Models.DataHub.ActivityPartyRole.Sender, cache),
            To = MappingHelpers.ToDataverseActivityParties(from.To, Models.DataHub.ActivityPartyRole.ToRecipient, cache),
            Cc = MappingHelpers.ToDataverseActivityParties(from.Cc, Models.DataHub.ActivityPartyRole.CcRecipient, cache),
            Bcc = MappingHelpers.ToDataverseActivityParties(from.Bcc, Models.DataHub.ActivityPartyRole.BccRecipient, cache)
        };

        if (!string.IsNullOrWhiteSpace(from.InReplyTo))
        {
            mapped[DataverseEmail.Fields.InReplyTo] = from.InReplyTo;
        }

        if (from.ScheduledDurationMinutes is not null)
        {
            mapped[DataverseEmail.Fields.ScheduledDurationMinutes] = from.ScheduledDurationMinutes;
        }

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseEmail.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.ActivityId = dataverseId;
        }

        return Task.FromResult(mapped);
    }
}
