using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ResolveResolutionPromises;

public class ResolveResolutionPromisesRequest<TDataHubEntity, TDataverseSibling> : IRequest<ResolveResolutionPromisesResponse<TDataHubEntity, TDataverseSibling>> where TDataHubEntity : DataHubEntity where TDataverseSibling : Microsoft.Xrm.Sdk.Entity
{
    public List<ResolutionPromise> ResolutionPromises { get; set; }
    public List<TDataHubEntity> EntitiesToResolve { get; set; }
}