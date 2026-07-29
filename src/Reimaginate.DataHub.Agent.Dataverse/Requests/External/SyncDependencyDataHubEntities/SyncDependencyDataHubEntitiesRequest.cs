using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncDependencyDataHubEntities;

public class SyncDependencyDataHubEntitiesRequest<TDataHubEntity, TDataverseEntity> : IRequest<ProcessSyncResponse> where TDataHubEntity : DataHubEntity where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public SyncDependencyDataHubEntitiesRequest()
    { }

    public SyncDependencyDataHubEntitiesRequest(List<string> entityIds)
    {
        EntityIds = entityIds;
    }
    public SyncDependencyDataHubEntitiesRequest(List<string> entityIds, List<EntityReference> dependencyTree, List<ResolutionPromise> resolutionPromises)
    {
        EntityIds = entityIds;
        DependencyTree = dependencyTree;
        ResolutionPromises = resolutionPromises;
    }


    public List<string> EntityIds { get; set; }

    public List<EntityReference> DependencyTree { get; set; } = new();

    public string CorrelationId { get; set; }

    public List<ResolutionPromise> ResolutionPromises { get; set; } = new();
}