using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class Activity : DataHubEntity
{
    public Activity()
    {
        entityType = nameof(Activity);
    }

    public string? ActivityType { get; set; }

    public string? Subject { get; set; }

    public string? Description { get; set; }

    public DateTime? ScheduledStart { get; set; }

    public DateTime? ScheduledEnd { get; set; }

    public int? ScheduledDurationMinutes { get; set; }

    public DateTime? ActualStart { get; set; }

    public DateTime? ActualEnd { get; set; }

    public int? ActualDurationMinutes { get; set; }

    public string? State { get; set; }

    public string? Status { get; set; }

    public EntityReference? Owner { get; set; }

    public EntityReference? Regarding { get; set; }
}
