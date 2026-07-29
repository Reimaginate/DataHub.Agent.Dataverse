//using Newtonsoft.Json;
//using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessJobCreatedEventNotifications;
//using Reimaginate.DataHub.SharedModels.Notifications;
//using Reimaginate.Mediator;

//namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.ProcessJobNotifications;

//public class ProcessJobNotificationsRequestHandler : IHandler<ProcessJobNotificationsRequest, ProcessJobNotificationsResponse>
//{
//    private readonly IMediator _mediator;

//    public ProcessJobNotificationsRequestHandler(IMediator mediator)
//    {
//        _mediator = mediator;
//    }

//    public async Task<ProcessJobNotificationsResponse> HandleAsync(ProcessJobNotificationsRequest request, CancellationToken cancellationToken)
//    {
//        var groupedByEventType = request.EventGridEvents.GroupBy(g => g.EventType);

//        foreach (var eventType in groupedByEventType)
//        {
//            switch (eventType.Key)
//            {
//                case nameof(JobNotification):
//                    var jobCreatedNotifications = eventType.Select(s => JsonConvert.DeserializeObject<JobNotification>(s.Data.ToString())).ToList();
//                    await _mediator.SendAndHandleExceptions(new ProcessJobCreatedEventNotificationsRequest()
//                    {
//                        Notifications = jobCreatedNotifications,
//                        DataHubAssemblyMarker = request.DataHubAssemblyMarker
//                    }, cancellationToken);
//                    break;

//            }
//        }

//        return new ProcessJobNotificationsResponse(true);
//    }
//}