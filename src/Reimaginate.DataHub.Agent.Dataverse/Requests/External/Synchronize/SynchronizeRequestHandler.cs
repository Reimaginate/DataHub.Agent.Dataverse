using Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeUpdatedDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncUpdatedDataHubEntities;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.ProcessingLockService;
using NullResponse = Reimaginate.Mediator.NullResponse;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.Synchronize;

public class SynchronizeRequestHandler<TDataverseEntity, TDataHubEntity>(IMediator mediator, IProcessingLockService processingLockService)
    : IHandler<SynchronizeRequest<TDataverseEntity, TDataHubEntity>, NullResponse>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
    where TDataHubEntity : DataHubEntity
{
    public async Task<NullResponse> HandleAsync(SynchronizeRequest<TDataverseEntity, TDataHubEntity> request, CancellationToken cancellationToken)
    {
        var correlationId = request.CorrelationId ??= Guid.NewGuid().ToString();
        
        _ = (await mediator.TrySend<NullResponse>(new MergeUpdatedDataverseEntitiesRequest<TDataverseEntity, TDataHubEntity>()
        {
            BatchSize = request.BatchSize,
            CorrelationId = correlationId,
            FromDateTime = request.FromDateTime,
            Max = request.Max,
            JobLock = request.JobLock
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        _ = (await mediator.TrySend<NullResponse>(new SyncUpdatedDataHubEntitiesRequest<TDataHubEntity, TDataverseEntity>()
        {
            CorrelationId = correlationId,
            FromDateTime = request.FromDateTime,
            BatchSize = request.BatchSize,
            Max = request.Max,
            JobLock = request.JobLock
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        return new NullResponse();
    }
}