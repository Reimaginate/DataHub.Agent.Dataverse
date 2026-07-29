using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using Reimaginate.Mapper;
using DataHubAppointment = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Appointment;
using DataverseAppointment = DataverseModel.Appointment;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseAppointmentToDataHubAppointment : ITypeMapper<DataverseAppointment, DataHubAppointment>
{
    public Task<DataHubAppointment> MapAsync(DataverseAppointment from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubAppointment
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseAppointment.EntityLogicalName, from.Id),
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
            Owner = MappingHelpers.ToOwnerReference(from.OwnerId),
            Regarding = MappingHelpers.ToActivityReference(from.RegardingObjectId),
            RequiredAttendees = MappingHelpers.ToActivityParties(from.RequiredAttendees, ActivityPartyRole.RequiredAttendee),
            OptionalAttendees = MappingHelpers.ToActivityParties(from.OptionalAttendees, ActivityPartyRole.OptionalAttendee),
            Organizer = MappingHelpers.ToActivityParties(from.Organizer, ActivityPartyRole.Organizer),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseAppointment.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseAppointment.Fields.ModifiedOn)
        });
    }
}
