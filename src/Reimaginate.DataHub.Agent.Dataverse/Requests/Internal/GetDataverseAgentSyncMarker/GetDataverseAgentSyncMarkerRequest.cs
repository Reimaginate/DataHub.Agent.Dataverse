using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.GetDataverseAgentSyncMarker;

public class GetDataverseAgentSyncMarkerRequest : IRequest<GetDataverseAgentSyncMarkerResponse>
{
    public string AgentId { get; set; }
    public string DataSourceId { get; set; }
    public string EntityType { get; set; }
    public string DefaultValue { get; set; }
}