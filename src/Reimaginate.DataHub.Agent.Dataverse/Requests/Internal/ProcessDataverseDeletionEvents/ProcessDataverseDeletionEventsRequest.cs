using Reimaginate.DataHub.Agent.Dataverse.Models;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseCreateUpdateEvents;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseDeletionEvents;

public class ProcessDataverseDeletionEventsRequest : IRequest<ProcessDataverseDeletionEventsResponse>
{
    public List<DeletionEvent> Events { get; set; }
    public Type DataHubAssemblyMarker { get; set; }
}