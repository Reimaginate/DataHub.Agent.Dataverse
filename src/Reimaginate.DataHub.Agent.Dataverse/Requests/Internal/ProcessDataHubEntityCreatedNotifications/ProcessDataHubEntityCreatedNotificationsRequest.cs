using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseDeletionEvents;
using Reimaginate.DataHub.SharedModels.Notifications;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataHubEntityCreatedNotifications;

public class ProcessDataHubEntityCreatedNotificationsRequest : IRequest<NullResponse>
{
    public List<DataHubEntityCreatedNotification> Notifications { get; set; }
    public Type DataHubAssemblyMarker { get; set; }
}