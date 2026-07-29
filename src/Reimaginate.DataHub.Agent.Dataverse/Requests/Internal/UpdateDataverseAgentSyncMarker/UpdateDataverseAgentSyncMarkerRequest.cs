using Reimaginate.DataHub.SharedModels.Markers;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.UpdateDataverseAgentSyncMarker;

public class UpdateDataverseAgentSyncMarkerRequest : IRequest<UpdateDataverseAgentSyncMarkerResponse>
{
    public SyncMarker Marker { get; set; }
    public string NewValue { get; set; }
    public DateTimeOffset? RunTime { get; set; }
}