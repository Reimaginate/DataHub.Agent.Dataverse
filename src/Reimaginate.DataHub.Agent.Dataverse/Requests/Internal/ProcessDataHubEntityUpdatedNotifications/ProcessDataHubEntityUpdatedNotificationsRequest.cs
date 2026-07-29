using Reimaginate.DataHub.SharedModels.Notifications;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataHubEntityUpdatedNotifications;

public class ProcessDataHubEntityUpdatedNotificationsRequest : IRequest<NullResponse>
{
    public List<DataHubEntityUpdatedNotification> Notifications { get; set; }
    public Type DataHubAssemblyMarker { get; set; }
}