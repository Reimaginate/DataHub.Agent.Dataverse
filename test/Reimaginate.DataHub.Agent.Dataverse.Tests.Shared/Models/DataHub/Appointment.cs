using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class Appointment : DataHubEntity
{
    public Appointment()
    {
        entityType = nameof(Appointment);
    }

    public string? Subject { get; set; }

    public string? Description { get; set; }

    public string? Location { get; set; }

    public bool? IsAllDayEvent { get; set; }

    public DateTime? ScheduledStart { get; set; }

    public DateTime? ScheduledEnd { get; set; }

    public int? ScheduledDurationMinutes { get; set; }

    public DateTime? ActualStart { get; set; }

    public DateTime? ActualEnd { get; set; }

    public int? ActualDurationMinutes { get; set; }

    public EntityReference? Owner { get; set; }

    public EntityReference? Regarding { get; set; }

    public List<ActivityParty> RequiredAttendees { get; set; } = [];

    public List<ActivityParty> OptionalAttendees { get; set; } = [];

    public List<ActivityParty> Organizer { get; set; } = [];
}
