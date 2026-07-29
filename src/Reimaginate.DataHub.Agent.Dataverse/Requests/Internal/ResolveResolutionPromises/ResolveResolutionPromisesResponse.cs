using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ResolveResolutionPromises;

public class ResolveResolutionPromisesResponse<TDataHubEntity, TDataverseSibling> where TDataHubEntity : DataHubEntity where TDataverseSibling : Microsoft.Xrm.Sdk.Entity
{
    public List<ResolvedResolutionPromise> UpdatedEntities { get; set; }
    public List<ResolutionPromise> ResolvedPromises { get; set; }
}