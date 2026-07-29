using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;

public class ProcessMergeRequest<TDataverseEntity, TDataHubEntity>(List<TDataverseEntity> dataverseEntities = null, List<ExternalEntityReference> dependencyTree = null)
    : IRequest<ProcessMergeResponse>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
    where TDataHubEntity : DataHubEntity
{
    public Dictionary<string, object> Cache { get; set; } = new();

    public List<TDataverseEntity> DataverseEntities { get; set; } = dataverseEntities;

    public List<ExternalEntityReference> DependencyTree { get; set; } = dependencyTree;

    public string CorrelationId { get; set; }
}