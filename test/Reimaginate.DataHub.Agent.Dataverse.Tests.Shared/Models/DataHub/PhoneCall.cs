using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class PhoneCall : DataHubEntity
{
    public PhoneCall()
    {
        entityType = nameof(PhoneCall);
    }

    public string? Subject { get; set; }

    public string? Description { get; set; }

    public ActivityDirection? Direction { get; set; }

    public string? PhoneNumber { get; set; }

    public DateTime? ScheduledStart { get; set; }

    public DateTime? ScheduledEnd { get; set; }

    public int? ScheduledDurationMinutes { get; set; }

    public DateTime? ActualStart { get; set; }

    public DateTime? ActualEnd { get; set; }

    public int? ActualDurationMinutes { get; set; }

    public EntityReference? Owner { get; set; }

    public EntityReference? Regarding { get; set; }

    public List<ActivityParty> From { get; set; } = [];

    public List<ActivityParty> To { get; set; } = [];
}
