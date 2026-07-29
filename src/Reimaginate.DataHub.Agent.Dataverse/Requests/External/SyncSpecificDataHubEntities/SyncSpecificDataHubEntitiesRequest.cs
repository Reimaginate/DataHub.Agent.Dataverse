using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.ProcessingLockService.Abstractions;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncSpecificDataHubEntities;

public class SyncSpecificDataHubEntitiesRequest<TDataHubEntity, TDataverseEntity> : IRequest<ProcessSyncResponse> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity where TDataHubEntity : DataHubEntity
{
    public SyncSpecificDataHubEntitiesRequest()
    {}

    public SyncSpecificDataHubEntitiesRequest(List<string> entityIds, string correlationId = null) : this()
    {
        EntityIds = entityIds;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
    }

    public List<string> EntityIds { get; set; }
    public string CorrelationId { get; set; }
}