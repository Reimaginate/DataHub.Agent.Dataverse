using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUpdatedEntities;

public class ProcessUpdatedEntitiesResponse
{
    public List<SyncEntityResult> SyncResults { get; set; }
}