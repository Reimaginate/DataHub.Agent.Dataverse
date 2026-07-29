using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.ProcessingLockService.Abstractions;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeUpdatedDataverseEntities;

public class MergeUpdatedDataverseEntitiesRequest<TDataverseEntity, TDataHubEntity> : IRequest<NullResponse> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity where TDataHubEntity : DataHubEntity
{
    public string CorrelationId { get; set; }
    public DateTime? FromDateTime { get; set; }
    public int BatchSize { get; set; } = 500;
    public int Max { get; set; } = -1;
    public ProcessingLock JobLock { get; set; }
}