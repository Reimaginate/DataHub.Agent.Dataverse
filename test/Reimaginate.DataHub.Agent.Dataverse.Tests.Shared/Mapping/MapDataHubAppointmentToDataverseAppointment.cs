using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using Reimaginate.Mapper;
using DataHubAppointment = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Appointment;
using DataverseAppointment = DataverseModel.Appointment;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataHubAppointmentToDataverseAppointment : ITypeMapper<DataHubAppointment, DataverseAppointment>
{
    public Task<DataverseAppointment> MapAsync(DataHubAppointment from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        var mapped = new DataverseAppointment
        {
            Subject = from.Subject,
            Description = from.Description,
            Location = from.Location,
            IsAllDayEvent = from.IsAllDayEvent,
            ScheduledStart = from.ScheduledStart,
            ScheduledEnd = from.ScheduledEnd,
            ScheduledDurationMinutes = from.ScheduledDurationMinutes,
            ActualStart = from.ActualStart,
            ActualEnd = from.ActualEnd,
            ActualDurationMinutes = from.ActualDurationMinutes,
            OwnerId = MappingHelpers.ResolveOwner(from.Owner, cache),
            RegardingObjectId = MappingHelpers.ResolveActivityReference(from.Regarding, cache),
            RequiredAttendees = MappingHelpers.ToDataverseActivityParties(from.RequiredAttendees, ActivityPartyRole.RequiredAttendee, cache),
            OptionalAttendees = MappingHelpers.ToDataverseActivityParties(from.OptionalAttendees, ActivityPartyRole.OptionalAttendee, cache),
            Organizer = MappingHelpers.ToDataverseActivityParties(from.Organizer, ActivityPartyRole.Organizer, cache)
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseAppointment.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.ActivityId = dataverseId;
        }

        return Task.FromResult(mapped);
    }
}
