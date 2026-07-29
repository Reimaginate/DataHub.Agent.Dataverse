using Reimaginate.DataHub.Agent.Dataverse.Models;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseCreateUpdateEvents;

public class ProcessDataverseCreateUpdateEventsRequest : IRequest<ProcessDataverseCreateUpdateEventsResponse>
{
    public List<CreateUpdateEvent> Events { get; set; }
    public Type DataHubAssemblyMarker { get; set; }
}