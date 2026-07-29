using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.EnsureReferencedEntitiesAreSyncd;

public class EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TDataverseEntity> where TDataHubEntity : DataHubEntity, new() where TDataverseEntity : Microsoft.Xrm.Sdk.Entity, new()
{
    public List<JObject> CachedEntities { get; set; }
    public List<ResolutionPromise> ResolutionPromises { get; set; } = new();
    public List<ReferenceEntitySyncFailure> Failures { get; set; } = new();
}