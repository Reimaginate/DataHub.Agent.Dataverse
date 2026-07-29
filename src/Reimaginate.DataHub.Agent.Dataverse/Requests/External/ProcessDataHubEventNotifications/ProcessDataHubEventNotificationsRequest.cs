using Azure.Messaging.EventGrid;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.ProcessDataHubEventNotifications;

public class ProcessDataHubEventNotificationsRequest : IRequest<ProcessDataHubEventNotificationsResponse>
{
    public Type DataHubAssemblyMarker { get; set; }
    public List<EventGridEvent> EventGridEvents { get; set; }
}