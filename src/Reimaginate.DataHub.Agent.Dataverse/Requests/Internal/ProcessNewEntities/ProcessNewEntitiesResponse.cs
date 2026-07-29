using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessNewEntities;

public class ProcessNewEntitiesResponse
{
    public List<SyncEntityResult> SyncResults { get; set; }
    public List<ResolutionPromise> ResolutionPromises { get; set; }
}