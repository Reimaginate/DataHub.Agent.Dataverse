using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using EntityReference = Reimaginate.DataHub.SharedModels.Core.EntityReference;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.EnsureReferencedEntitiesAreSyncd;

public class EnsureReferencedEntitiesAreSyncdRequest<TDataHubEntity, TDataverseEntity> : IRequest<EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TDataverseEntity>> where TDataHubEntity : DataHubEntity, new() where TDataverseEntity : Microsoft.Xrm.Sdk.Entity, new()
{
    public List<TDataHubEntity> Entities { get; set; }
    public List<EntityReference> DependencyTree { get; set; }
    public List<ResolutionPromise> ResolutionPromises { get; set; }
}