using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.ProcessingLockService.Abstractions;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncUpdatedDataHubEntities;

public class SyncUpdatedDataHubEntitiesRequest<TDataHubEntity, TDataverseEntity> : IRequest<NullResponse> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity where TDataHubEntity : DataHubEntity
{
    public DateTime? FromDateTime { get; set; }
    public string CorrelationId { get; set; }
    public int? BatchSize { get; set; }
    public int Max { get; set; } = -1;
    public ProcessingLock JobLock { get; set; }
}