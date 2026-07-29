using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeEntities;

public class MergeEntitiesRequest<TDataverseEntity, TDataHubEntity> : IRequest<ProcessMergeResponse> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity where TDataHubEntity : DataHubEntity
{
    public MergeEntitiesRequest(){
        
    }

    public MergeEntitiesRequest(List<Guid> dataverseEntityIds, List<ExternalEntityReference> dependencyTree, string correlationId)
    {
        DataverseEntityIds = dataverseEntityIds;
        DependencyTree = dependencyTree;
        CorrelationId = correlationId;
    }

    public List<Guid> DataverseEntityIds { get; set; }

    public List<ExternalEntityReference> DependencyTree { get; set; } = new();

    public string CorrelationId { get; set; }

    public bool ForceUpdate { get; set; }
}