using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SyncEntities;

public class SyncEntitiesRequest<TDataHubEntity, TDataverseEntity> : IRequest<ProcessSyncResponse> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity where TDataHubEntity : DataHubEntity
{
    public List<string> EntityIds { get; set; }
    public string CorrelationId { get; set; }
    public List<EntityReference> DependencyTree { get; set; } = new();
    public List<ResolutionPromise> ResolutionPromises { get; set; } = new();
}