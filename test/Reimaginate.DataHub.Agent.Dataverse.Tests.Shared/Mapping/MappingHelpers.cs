using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Constants;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using Reimaginate.DataHub.SharedModels.Core;
using System.Globalization;
using DataHubAccount = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account;
using DataHubActivity = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Activity;
using DataHubAppointment = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Appointment;
using DataHubCase = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Case;
using DataHubContact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;
using DataHubEmail = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Email;
using DataHubEntityReference = Reimaginate.DataHub.SharedModels.Core.EntityReference;
using DataHubLead = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Lead;
using DataHubNote = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Note;
using DataHubOpportunity = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Opportunity;
using DataHubPhoneCall = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PhoneCall;
using DataHubPriceList = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PriceList;
using DataHubPriceListItem = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PriceListItem;
using DataHubProduct = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Product;
using DataHubQuote = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Quote;
using DataHubQuoteLine = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.QuoteLine;
using DataHubSystemUser = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.SystemUser;
using DataHubTaskActivity = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.TaskActivity;
using DataHubTeam = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Team;
using DataverseActivityParty = DataverseModel.ActivityParty;
using XrmEntityReference = Microsoft.Xrm.Sdk.EntityReference;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

internal static class MappingHelpers
{
    public static List<AlternateKey> DataverseAlternateKeys(string logicalName, Guid id)
    {
        return id == Guid.Empty
            ? []
            : [new AlternateKey { Key = $"dataverse.{logicalName}", Value = id.ToString() }];
    }

    public static DateTimeOffset? ToDateTimeOffset(DateTime? value)
    {
        return value is null
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }

    public static DateTimeOffset? ToDateTimeOffset(Entity entity, string attributeLogicalName)
    {
        if (!entity.Attributes.TryGetValue(attributeLogicalName, out var value) ||
            value is null)
        {
            return null;
        }

        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            string text when DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTimeOffset) => dateTimeOffset,
            _ => null
        };
    }

    public static decimal? ToDecimal(Money? value)
    {
        return value?.Value;
    }

    public static Money? ToMoney(decimal? value)
    {
        return value is null ? null : new Money(value.Value);
    }

    public static ExternalEntityReference? ToExternalReference<TDataHubEntity>(
        XrmEntityReference? reference,
        string sourceEntityType)
    {
        return reference is null
            ? null
            : new ExternalEntityReference
            {
                DataSource = TestDataSources.Dataverse,
                SourceEntityType = sourceEntityType,
                EntityType = typeof(TDataHubEntity).Name,
                EntityId = reference.Id.ToString()
            };
    }

    public static ExternalEntityReference? ToExternalReference(
        XrmEntityReference? reference,
        string sourceEntityType,
        string entityType)
    {
        return reference is null
            ? null
            : new ExternalEntityReference
            {
                DataSource = TestDataSources.Dataverse,
                SourceEntityType = sourceEntityType,
                EntityType = entityType,
                EntityId = reference.Id.ToString()
            };
    }

    public static ExternalEntityReference? ToOwnerReference(XrmEntityReference? reference)
    {
        var dataHubEntityType = reference?.LogicalName switch
        {
            DataverseModel.SystemUser.EntityLogicalName => typeof(DataHubSystemUser).Name,
            DataverseModel.Team.EntityLogicalName => typeof(DataHubTeam).Name,
            _ => reference?.LogicalName
        };

        return reference is null
            ? null
            : new ExternalEntityReference
            {
                DataSource = TestDataSources.Dataverse,
                SourceEntityType = reference.LogicalName,
                EntityType = dataHubEntityType,
                EntityId = reference.Id.ToString()
            };
    }

    public static Guid? GetDataverseId(DataHubEntity entity, string logicalName)
    {
        var value = entity.alternateKeys?
            .FirstOrDefault(key => key.Key == $"dataverse.{logicalName}")?
            .Value;

        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static XrmEntityReference? ResolveReference<TDataHubEntity>(
        DataHubEntityReference? reference,
        string logicalName,
        Dictionary<string, object>? cache)
    {
        if (reference is null)
        {
            return null;
        }

        if (reference is ExternalEntityReference externalReference &&
            externalReference.DataSource == TestDataSources.Dataverse &&
            externalReference.SourceEntityType == logicalName &&
            Guid.TryParse(externalReference.EntityId, out var externalId))
        {
            return new XrmEntityReference(logicalName, externalId);
        }

        if (reference.EntityType == logicalName &&
            Guid.TryParse(reference.EntityId, out var dataverseReferenceId))
        {
            return new XrmEntityReference(logicalName, dataverseReferenceId);
        }

        if (cache is null ||
            !cache.TryGetValue(typeof(TDataHubEntity).Name, out var cachedEntities) ||
            cachedEntities is not IEnumerable<JObject> cachedObjects)
        {
            return null;
        }

        var matchedEntity = cachedObjects.FirstOrDefault(entity =>
            entity.Value<string>(nameof(DataHubEntity.id)) == reference.EntityId);

        var dataverseId = matchedEntity?
            .Value<JArray>(nameof(DataHubEntity.alternateKeys))?
            .ToObject<List<AlternateKey>>()?
            .FirstOrDefault(key => key.Key == $"dataverse.{logicalName}")?
            .Value;

        return Guid.TryParse(dataverseId, out var resolvedId)
            ? new XrmEntityReference(logicalName, resolvedId)
            : null;
    }

    public static XrmEntityReference? ResolveExternalReference(
        DataHubEntityReference? reference,
        string logicalName)
    {
        if (reference is null)
        {
            return null;
        }

        if (reference is ExternalEntityReference externalReference &&
            externalReference.DataSource == TestDataSources.Dataverse &&
            externalReference.SourceEntityType == logicalName &&
            Guid.TryParse(externalReference.EntityId, out var externalId))
        {
            return new XrmEntityReference(logicalName, externalId);
        }

        if (reference.EntityType == logicalName &&
            Guid.TryParse(reference.EntityId, out var dataverseReferenceId))
        {
            return new XrmEntityReference(logicalName, dataverseReferenceId);
        }

        return null;
    }

    public static XrmEntityReference? ResolveOwner(
        DataHubEntityReference? reference,
        Dictionary<string, object>? cache)
    {
        if (reference is null ||
            !Guid.TryParse(reference.EntityId, out var id))
        {
            return null;
        }

        if (reference is ExternalEntityReference externalReference &&
            externalReference.DataSource == TestDataSources.Dataverse &&
            !string.IsNullOrWhiteSpace(externalReference.SourceEntityType))
        {
            return new XrmEntityReference(externalReference.SourceEntityType, id);
        }

        return reference.EntityType is DataverseModel.SystemUser.EntityLogicalName or DataverseModel.Team.EntityLogicalName
            ? new XrmEntityReference(reference.EntityType, id)
            : ResolveDataHubOwnerReference(reference, cache);
    }

    private static XrmEntityReference? ResolveDataHubOwnerReference(
        DataHubEntityReference reference,
        Dictionary<string, object>? cache)
    {
        if (reference.EntityType == typeof(DataHubSystemUser).Name)
        {
            return ResolveReference<DataHubSystemUser>(reference, DataverseModel.SystemUser.EntityLogicalName, cache);
        }

        if (reference.EntityType == typeof(DataHubTeam).Name)
        {
            return ResolveReference<DataHubTeam>(reference, DataverseModel.Team.EntityLogicalName, cache);
        }

        return null;
    }

    public static DataHubEntityReference? ToActivityReference(XrmEntityReference? reference)
    {
        return reference?.LogicalName switch
        {
            DataverseModel.Account.EntityLogicalName => ToExternalReference<DataHubAccount>(reference, DataverseModel.Account.EntityLogicalName),
            DataverseModel.Annotation.EntityLogicalName => ToExternalReference<DataHubNote>(reference, DataverseModel.Annotation.EntityLogicalName),
            DataverseModel.Appointment.EntityLogicalName => ToExternalReference<DataHubAppointment>(reference, DataverseModel.Appointment.EntityLogicalName),
            DataverseModel.Contact.EntityLogicalName => ToExternalReference<DataHubContact>(reference, DataverseModel.Contact.EntityLogicalName),
            DataverseModel.Email.EntityLogicalName => ToExternalReference<DataHubEmail>(reference, DataverseModel.Email.EntityLogicalName),
            DataverseModel.Incident.EntityLogicalName => ToExternalReference<DataHubCase>(reference, DataverseModel.Incident.EntityLogicalName),
            DataverseModel.Lead.EntityLogicalName => ToExternalReference<DataHubLead>(reference, DataverseModel.Lead.EntityLogicalName),
            DataverseModel.Opportunity.EntityLogicalName => ToExternalReference<DataHubOpportunity>(reference, DataverseModel.Opportunity.EntityLogicalName),
            DataverseModel.PhoneCall.EntityLogicalName => ToExternalReference<DataHubPhoneCall>(reference, DataverseModel.PhoneCall.EntityLogicalName),
            DataverseModel.PriceLevel.EntityLogicalName => ToExternalReference<DataHubPriceList>(reference, DataverseModel.PriceLevel.EntityLogicalName),
            DataverseModel.Product.EntityLogicalName => ToExternalReference<DataHubProduct>(reference, DataverseModel.Product.EntityLogicalName),
            DataverseModel.ProductPriceLevel.EntityLogicalName => ToExternalReference<DataHubPriceListItem>(reference, DataverseModel.ProductPriceLevel.EntityLogicalName),
            DataverseModel.Quote.EntityLogicalName => ToExternalReference<DataHubQuote>(reference, DataverseModel.Quote.EntityLogicalName),
            DataverseModel.QuoteDetail.EntityLogicalName => ToExternalReference<DataHubQuoteLine>(reference, DataverseModel.QuoteDetail.EntityLogicalName),
            DataverseModel.Task.EntityLogicalName => ToExternalReference<DataHubTaskActivity>(reference, DataverseModel.Task.EntityLogicalName),
            DataverseModel.ActivityPointer.EntityLogicalName => ToExternalReference<DataHubActivity>(reference, DataverseModel.ActivityPointer.EntityLogicalName),
            DataverseModel.SystemUser.EntityLogicalName => ToOwnerReference(reference),
            DataverseModel.Team.EntityLogicalName => ToOwnerReference(reference),
            null => null,
            _ => ToExternalReference(reference, reference.LogicalName, reference.LogicalName)
        };
    }

    public static XrmEntityReference? ResolveActivityReference(
        DataHubEntityReference? reference,
        Dictionary<string, object>? cache)
    {
        if (reference is null)
        {
            return null;
        }

        if (reference is ExternalEntityReference externalReference &&
            externalReference.DataSource == TestDataSources.Dataverse &&
            !string.IsNullOrWhiteSpace(externalReference.SourceEntityType) &&
            Guid.TryParse(externalReference.EntityId, out var externalId))
        {
            return new XrmEntityReference(externalReference.SourceEntityType, externalId);
        }

        return reference.EntityType switch
        {
            var entityType when entityType == typeof(DataHubAccount).Name => ResolveReference<DataHubAccount>(reference, DataverseModel.Account.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubActivity).Name => ResolveReference<DataHubActivity>(reference, DataverseModel.ActivityPointer.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubAppointment).Name => ResolveReference<DataHubAppointment>(reference, DataverseModel.Appointment.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubCase).Name => ResolveReference<DataHubCase>(reference, DataverseModel.Incident.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubContact).Name => ResolveReference<DataHubContact>(reference, DataverseModel.Contact.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubEmail).Name => ResolveReference<DataHubEmail>(reference, DataverseModel.Email.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubLead).Name => ResolveReference<DataHubLead>(reference, DataverseModel.Lead.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubNote).Name => ResolveReference<DataHubNote>(reference, DataverseModel.Annotation.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubOpportunity).Name => ResolveReference<DataHubOpportunity>(reference, DataverseModel.Opportunity.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubPhoneCall).Name => ResolveReference<DataHubPhoneCall>(reference, DataverseModel.PhoneCall.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubPriceList).Name => ResolveReference<DataHubPriceList>(reference, DataverseModel.PriceLevel.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubPriceListItem).Name => ResolveReference<DataHubPriceListItem>(reference, DataverseModel.ProductPriceLevel.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubProduct).Name => ResolveReference<DataHubProduct>(reference, DataverseModel.Product.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubQuote).Name => ResolveReference<DataHubQuote>(reference, DataverseModel.Quote.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubQuoteLine).Name => ResolveReference<DataHubQuoteLine>(reference, DataverseModel.QuoteDetail.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubSystemUser).Name => ResolveReference<DataHubSystemUser>(reference, DataverseModel.SystemUser.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubTaskActivity).Name => ResolveReference<DataHubTaskActivity>(reference, DataverseModel.Task.EntityLogicalName, cache),
            var entityType when entityType == typeof(DataHubTeam).Name => ResolveReference<DataHubTeam>(reference, DataverseModel.Team.EntityLogicalName, cache),
            DataverseModel.Account.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.Account.EntityLogicalName),
            DataverseModel.Annotation.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.Annotation.EntityLogicalName),
            DataverseModel.Appointment.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.Appointment.EntityLogicalName),
            DataverseModel.ActivityPointer.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.ActivityPointer.EntityLogicalName),
            DataverseModel.Contact.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.Contact.EntityLogicalName),
            DataverseModel.Email.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.Email.EntityLogicalName),
            DataverseModel.Incident.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.Incident.EntityLogicalName),
            DataverseModel.Lead.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.Lead.EntityLogicalName),
            DataverseModel.Opportunity.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.Opportunity.EntityLogicalName),
            DataverseModel.PhoneCall.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.PhoneCall.EntityLogicalName),
            DataverseModel.PriceLevel.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.PriceLevel.EntityLogicalName),
            DataverseModel.Product.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.Product.EntityLogicalName),
            DataverseModel.ProductPriceLevel.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.ProductPriceLevel.EntityLogicalName),
            DataverseModel.Quote.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.Quote.EntityLogicalName),
            DataverseModel.QuoteDetail.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.QuoteDetail.EntityLogicalName),
            DataverseModel.SystemUser.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.SystemUser.EntityLogicalName),
            DataverseModel.Task.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.Task.EntityLogicalName),
            DataverseModel.Team.EntityLogicalName => ResolveExternalReference(reference, DataverseModel.Team.EntityLogicalName),
            _ => null
        };
    }

    public static List<ActivityParty> ToActivityParties(
        IEnumerable<DataverseActivityParty>? parties,
        ActivityPartyRole defaultRole)
    {
        return parties?
            .Select(party => new ActivityParty
            {
                Role = ToActivityPartyRole(party.ParticipationTypeMask) ?? defaultRole,
                AddressUsed = party.AddressUsed,
                UnresolvedPartyName = party.UnresolvedPartyName,
                Party = ToActivityReference(party.PartyId)
            })
            .ToList() ?? [];
    }

    public static List<DataverseActivityParty>? ToDataverseActivityParties(
        IEnumerable<ActivityParty>? parties,
        ActivityPartyRole defaultRole,
        Dictionary<string, object>? cache)
    {
        var mapped = parties?
            .Select(party => new DataverseActivityParty
            {
                AddressUsed = party.AddressUsed,
                UnresolvedPartyName = party.UnresolvedPartyName,
                ParticipationTypeMask = ToDataverseActivityPartyRole(party.Role ?? defaultRole),
                PartyId = ResolveActivityReference(party.Party, cache)
            })
            .ToList();

        return mapped is null || mapped.Count == 0 ? null : mapped;
    }

    private static ActivityPartyRole? ToActivityPartyRole(DataverseModel.ActivityParty_ParticipationTypeMask? value)
    {
        return value switch
        {
            DataverseModel.ActivityParty_ParticipationTypeMask.Sender => ActivityPartyRole.Sender,
            DataverseModel.ActivityParty_ParticipationTypeMask.ToRecipient => ActivityPartyRole.ToRecipient,
            DataverseModel.ActivityParty_ParticipationTypeMask.CcRecipient => ActivityPartyRole.CcRecipient,
            DataverseModel.ActivityParty_ParticipationTypeMask.BccRecipient => ActivityPartyRole.BccRecipient,
            DataverseModel.ActivityParty_ParticipationTypeMask.RequiredAttendee => ActivityPartyRole.RequiredAttendee,
            DataverseModel.ActivityParty_ParticipationTypeMask.OptionalAttendee => ActivityPartyRole.OptionalAttendee,
            DataverseModel.ActivityParty_ParticipationTypeMask.Organizer => ActivityPartyRole.Organizer,
            DataverseModel.ActivityParty_ParticipationTypeMask.Regarding => ActivityPartyRole.Regarding,
            DataverseModel.ActivityParty_ParticipationTypeMask.Owner => ActivityPartyRole.Owner,
            DataverseModel.ActivityParty_ParticipationTypeMask.Resource => ActivityPartyRole.Resource,
            DataverseModel.ActivityParty_ParticipationTypeMask.Customer => ActivityPartyRole.Customer,
            DataverseModel.ActivityParty_ParticipationTypeMask.Related => ActivityPartyRole.Related,
            DataverseModel.ActivityParty_ParticipationTypeMask.ChatParticipant => ActivityPartyRole.ChatParticipant,
            _ => null
        };
    }

    private static DataverseModel.ActivityParty_ParticipationTypeMask? ToDataverseActivityPartyRole(ActivityPartyRole? value)
    {
        return value switch
        {
            ActivityPartyRole.Sender => DataverseModel.ActivityParty_ParticipationTypeMask.Sender,
            ActivityPartyRole.ToRecipient => DataverseModel.ActivityParty_ParticipationTypeMask.ToRecipient,
            ActivityPartyRole.CcRecipient => DataverseModel.ActivityParty_ParticipationTypeMask.CcRecipient,
            ActivityPartyRole.BccRecipient => DataverseModel.ActivityParty_ParticipationTypeMask.BccRecipient,
            ActivityPartyRole.RequiredAttendee => DataverseModel.ActivityParty_ParticipationTypeMask.RequiredAttendee,
            ActivityPartyRole.OptionalAttendee => DataverseModel.ActivityParty_ParticipationTypeMask.OptionalAttendee,
            ActivityPartyRole.Organizer => DataverseModel.ActivityParty_ParticipationTypeMask.Organizer,
            ActivityPartyRole.Regarding => DataverseModel.ActivityParty_ParticipationTypeMask.Regarding,
            ActivityPartyRole.Owner => DataverseModel.ActivityParty_ParticipationTypeMask.Owner,
            ActivityPartyRole.Resource => DataverseModel.ActivityParty_ParticipationTypeMask.Resource,
            ActivityPartyRole.Customer => DataverseModel.ActivityParty_ParticipationTypeMask.Customer,
            ActivityPartyRole.Related => DataverseModel.ActivityParty_ParticipationTypeMask.Related,
            ActivityPartyRole.ChatParticipant => DataverseModel.ActivityParty_ParticipationTypeMask.ChatParticipant,
            _ => null
        };
    }
}
