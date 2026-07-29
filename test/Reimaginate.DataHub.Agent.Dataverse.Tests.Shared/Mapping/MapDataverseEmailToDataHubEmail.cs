using Reimaginate.Mapper;
using DataHubEmail = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Email;
using DataverseEmail = DataverseModel.Email;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseEmailToDataHubEmail : ITypeMapper<DataverseEmail, DataHubEmail>
{
    public Task<DataHubEmail> MapAsync(
        DataverseEmail from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubEmail
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseEmail.EntityLogicalName, from.Id),
            Subject = from.Subject,
            Description = from.Description,
            Direction = from.DirectionCode switch
            {
                true => Models.DataHub.EmailDirection.Outgoing,
                false => Models.DataHub.EmailDirection.Incoming,
                _ => null
            },
            ScheduledStart = from.ScheduledStart,
            ScheduledEnd = from.ScheduledEnd,
            ScheduledDurationMinutes = from.ScheduledDurationMinutes,
            ActualStart = from.ActualStart,
            ActualEnd = from.ActualEnd,
            ActualDurationMinutes = from.ActualDurationMinutes,
            TrackingToken = from.TrackingToken,
            InReplyTo = from.InReplyTo,
            Owner = MappingHelpers.ToOwnerReference(from.OwnerId),
            Regarding = MappingHelpers.ToActivityReference(from.RegardingObjectId),
            ParentEmail = MappingHelpers.ToActivityReference(from.ParentActivityId),
            CorrelatedEmail = MappingHelpers.ToActivityReference(from.CorrelatedActivityId),
            From = MappingHelpers.ToActivityParties(from.From, Models.DataHub.ActivityPartyRole.Sender),
            To = MappingHelpers.ToActivityParties(from.To, Models.DataHub.ActivityPartyRole.ToRecipient),
            Cc = MappingHelpers.ToActivityParties(from.Cc, Models.DataHub.ActivityPartyRole.CcRecipient),
            Bcc = MappingHelpers.ToActivityParties(from.Bcc, Models.DataHub.ActivityPartyRole.BccRecipient),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseEmail.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseEmail.Fields.ModifiedOn)
        });
    }
}
