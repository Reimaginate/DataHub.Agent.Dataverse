using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.RetrieveUpdatedDataHubEntities;

public class RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity> where TDataHubEntity : DataHubEntity
{
    public List<TDataHubEntity> Results { get; set; }
    public string ContinuationToken { get; set; }
    public int ResultCount { get; set; }
    public bool MoreResultsAvailable { get; set; }
}
