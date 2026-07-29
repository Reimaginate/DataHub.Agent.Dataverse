using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUntrackedEntities;

public class ProcessUntrackedEntitiesResponse
{
    public List<SyncEntityResult> SyncResults { get; set; }
}