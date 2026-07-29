using Newtonsoft.Json;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataHubEntityCreatedNotifications;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataHubEntityUpdatedNotifications;
using Reimaginate.DataHub.SharedModels.Notifications;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.ProcessDataHubEventNotifications;

public class ProcessDataHubEventNotificationsRequestHandler(IMediator mediator) : IHandler<ProcessDataHubEventNotificationsRequest, ProcessDataHubEventNotificationsResponse>
{
    public async Task<ProcessDataHubEventNotificationsResponse> HandleAsync(ProcessDataHubEventNotificationsRequest request, CancellationToken cancellationToken)
    {
        var groupedByEventType = request.EventGridEvents.GroupBy(g => g.EventType);

        foreach (var eventType in groupedByEventType)
        {
            switch (eventType.Key)
            {
                case nameof(DataHubEntityCreatedNotification):
                    var entityCreatedNotifications = eventType.Select(s => JsonConvert.DeserializeObject<DataHubEntityCreatedNotification>(s.Data.ToString())).ToList();
                    _ = (await mediator.TrySend<NullResponse>(new ProcessDataHubEntityCreatedNotificationsRequest() { Notifications = entityCreatedNotifications, DataHubAssemblyMarker = request.DataHubAssemblyMarker }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
                    break;

                case nameof(DataHubEntityUpdatedNotification):
                    var entityUpdatedNotifications = eventType.Select(s => JsonConvert.DeserializeObject<DataHubEntityUpdatedNotification>(s.Data.ToString())).ToList();
                    _ = (await mediator.TrySend<NullResponse>(new ProcessDataHubEntityUpdatedNotificationsRequest() { Notifications = entityUpdatedNotifications, DataHubAssemblyMarker = request.DataHubAssemblyMarker }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
                    break;
            }
        }

        return new ProcessDataHubEventNotificationsResponse(true);
    }
}