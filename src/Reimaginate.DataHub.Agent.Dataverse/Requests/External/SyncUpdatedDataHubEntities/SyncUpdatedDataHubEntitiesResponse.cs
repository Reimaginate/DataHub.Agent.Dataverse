using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncUpdatedDataHubEntities;

public class SyncUpdatedDataHubEntitiesResponse<TDataHubEntity, TDataverseEntity> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity where TDataHubEntity : DataHubEntity
{
    public Dictionary<string, string> ProcessedEntities { get; set; } = new();
}