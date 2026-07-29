using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class ActivityParty
{
    public ActivityPartyRole? Role { get; set; }

    public string? AddressUsed { get; set; }

    public string? UnresolvedPartyName { get; set; }

    public EntityReference? Party { get; set; }
}

public enum ActivityPartyRole
{
    Sender,
    ToRecipient,
    CcRecipient,
    BccRecipient,
    RequiredAttendee,
    OptionalAttendee,
    Organizer,
    Regarding,
    Owner,
    Resource,
    Customer,
    Related,
    ChatParticipant
}
