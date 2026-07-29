using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessAgentTask;

public class ProcessAgentTaskRequest<TDataHubAssemblyMarker> : IRequest<JObject>  where TDataHubAssemblyMarker: DataHubEntity
{
    public string CorrelationId { get; set; }
    public JObject AgentRequest { get; set; }
}