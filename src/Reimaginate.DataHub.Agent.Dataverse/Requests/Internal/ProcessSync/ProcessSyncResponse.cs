using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;

public class ProcessSyncResponse
{
    public List<SyncEntityResult> Results { get; set; } = new();
    public List<ResolutionPromise> ResolutionPromises { get; set; } = new();
}