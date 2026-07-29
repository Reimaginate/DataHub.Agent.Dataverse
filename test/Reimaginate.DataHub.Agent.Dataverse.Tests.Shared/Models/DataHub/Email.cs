using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class Email : DataHubEntity
{
    public Email()
    {
        entityType = nameof(Email);
    }

    public string? Subject { get; set; }

    public string? Description { get; set; }

    public EmailDirection? Direction { get; set; }

    public DateTime? ScheduledStart { get; set; }

    public DateTime? ScheduledEnd { get; set; }

    public int? ScheduledDurationMinutes { get; set; }

    public DateTime? ActualStart { get; set; }

    public DateTime? ActualEnd { get; set; }

    public int? ActualDurationMinutes { get; set; }

    public string? TrackingToken { get; set; }

    public string? InReplyTo { get; set; }

    public EntityReference? Owner { get; set; }

    public EntityReference? Regarding { get; set; }

    public EntityReference? ParentEmail { get; set; }

    public EntityReference? CorrelatedEmail { get; set; }

    public List<ActivityParty> From { get; set; } = [];

    public List<ActivityParty> To { get; set; } = [];

    public List<ActivityParty> Cc { get; set; } = [];

    public List<ActivityParty> Bcc { get; set; } = [];
}

public enum EmailDirection
{
    Incoming,
    Outgoing
}
