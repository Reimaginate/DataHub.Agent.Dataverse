using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.GetDataverseAgentMergeMarker;

public class GetDataverseAgentMergeMarkerRequest : IRequest<GetDataverseAgentMergeMarkerResponse>
{
    public string AgentId { get; set; }
    public string DataSourceId { get; set; }
    public string EntityType { get; set; }
    public string DefaultValue { get; set; }
}