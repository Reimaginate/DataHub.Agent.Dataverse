using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;

public class ProcessSyncRequest<TDataHubEntity, TDataverseEntity> : IRequest<ProcessSyncResponse> where TDataHubEntity : DataHubEntity where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public ProcessSyncRequest(List<TDataHubEntity> dataHubEntities = null, List<EntityReference> dependencyTree = null)
    {
        DataHubEntities = dataHubEntities;
        DependencyTree = dependencyTree;
    }

    public Dictionary<string, object> Cache { get; set; } = new();

    public List<TDataHubEntity> DataHubEntities { get; set; }

    public List<EntityReference> DependencyTree { get; set; } = new();

    public string CorrelationId { get; set; }

    public List<ResolutionPromise> ResolutionPromises { get; set; } = new();

}