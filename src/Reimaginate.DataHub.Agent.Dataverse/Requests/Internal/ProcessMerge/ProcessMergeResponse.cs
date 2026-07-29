using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;

public class ProcessMergeResponse
{
    public List<MergeEntityResult> Results { get; set; } = new();
}