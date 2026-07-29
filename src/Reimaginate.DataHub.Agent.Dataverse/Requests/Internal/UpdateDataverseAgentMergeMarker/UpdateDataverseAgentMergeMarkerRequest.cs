using Reimaginate.DataHub.SharedModels.Markers;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.UpdateDataverseAgentMergeMarker;

public class UpdateDataverseAgentMergeMarkerRequest : IRequest<UpdateDataverseAgentMergeMarkerResponse>
{
    public MergeMarker Marker { get; set; }
    public string NewValue { get; set; }
    public DateTimeOffset? RunTime { get; set; }
}