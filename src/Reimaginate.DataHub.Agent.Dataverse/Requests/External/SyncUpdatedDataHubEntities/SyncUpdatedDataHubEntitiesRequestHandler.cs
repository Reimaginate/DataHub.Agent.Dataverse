using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.GetDataverseAgentSyncMarker;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.RetrieveUpdatedDataHubEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendSyncFailuresToDataHub;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.UpdateDataverseAgentSyncMarker;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendSyncSuccessesToDataHub;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SyncEntities;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.ProcessingLockService;
using Reimaginate.Mediator;
using NullResponse = Reimaginate.Mediator.NullResponse;
using Reimaginate.ProcessingLockService.Abstractions;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncUpdatedDataHubEntities;

public class SyncUpdatedDataHubEntitiesRequestHandler<TDataHubEntity, TDataverseEntity>(IOptions<DataverseAgentOptions> config, IMediator mediator, ITimeService timeService, IProcessingLockService processingLockService)
    : IHandler<SyncUpdatedDataHubEntitiesRequest<TDataHubEntity, TDataverseEntity>, NullResponse>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
    where TDataHubEntity : DataHubEntity
{
    #region Private Helpers

    private async Task ReportSuccessesAndFailures(List<SyncEntityResult> syncEntityResults, CancellationToken cancellationToken)
    {
        var failures = syncEntityResults.Where(w => SyncOutcomes.IsFailure(w.SyncOutcome)).ToList();
        if (failures.Any()) _ = (await mediator.TrySend<NullResponse>(new SendSyncFailuresToDataHubRequest(failures), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        var successes = syncEntityResults.Where(w => w.SyncOutcome != SyncOutcomes.SyncFailed).ToList();
        if (successes.Any()) _ = (await mediator.TrySend<NullResponse>(new SendSyncSuccessesToDataHubRequest(successes), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
    }

    #endregion  

    public async Task<NullResponse> HandleAsync(SyncUpdatedDataHubEntitiesRequest<TDataHubEntity, TDataverseEntity> request, CancellationToken cancellationToken)
    {
        var getSyncLockResponse = await processingLockService.WaitForLockAsync($"dataverse/sync/{typeof(TDataHubEntity).Name}", request.CorrelationId, duration: TimeSpan.FromMinutes(5), waitTimeOut: TimeSpan.FromMinutes(5), cancellationToken: cancellationToken);
        getSyncLockResponse.ThrowIfUnsuccessful();
        var syncLock = getSyncLockResponse.Result;

        var processLocks = new List<ProcessingLock>() { syncLock };
        if (request.JobLock != null)
        {
            processLocks.Add(request.JobLock);
        }

        try
        {
            foreach (var processLock in processLocks)
            {
                var renewLockResponse = await processingLockService.RenewLockAsync(processLock, cancellationToken);
                renewLockResponse.ThrowIfUnsuccessful("Failed to renew lock");
            }

            request.CorrelationId ??= Guid.NewGuid().ToString();
            var runTime = timeService.Now();

            #region Get Sync Markers

            var syncMarkerResponse = (await mediator.TrySend<GetDataverseAgentSyncMarkerResponse>(new GetDataverseAgentSyncMarkerRequest()
            {
                AgentId = config.Value.AgentId,
                DataSourceId = config.Value.DataSource,
                EntityType = typeof(TDataHubEntity).Name,
                DefaultValue = timeService.Today().ToString("o")
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            var syncMarkerVal = timeService.Parse(syncMarkerResponse.SyncMarker.Value);
            if (request.FromDateTime != null)
            {
                syncMarkerVal = timeService.Parse(request.FromDateTime.Value);
            }

            #endregion Get Sync Markers

            #region Get DataHub Entities To sync

            var retrieveUpdatedDataHubEntitiesResponse = (await mediator.TrySend<RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity>>(new RetrieveUpdatedDataHubEntitiesRequest<TDataHubEntity>()
            {
                FromDateTime = syncMarkerVal,
                BatchSize = -1

            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
            var dataHubEntitiesToProcess = retrieveUpdatedDataHubEntitiesResponse.Results.Select(s => (s.id, s.lastUpdated)).Distinct().ToList();

            while (retrieveUpdatedDataHubEntitiesResponse.MoreResultsAvailable && (request.Max == -1 || dataHubEntitiesToProcess.Count < request.Max))
            {
                foreach (var processLock in processLocks)
                {
                    var renewLockResponse = await processingLockService.RenewLockAsync(processLock, cancellationToken);
                    renewLockResponse.ThrowIfUnsuccessful("Failed to renew lock");
                }

                retrieveUpdatedDataHubEntitiesResponse = (await mediator.TrySend<RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity>>(new RetrieveUpdatedDataHubEntitiesRequest<TDataHubEntity>()
                {
                    FromDateTime = syncMarkerVal,
                    BatchSize = -1,
                    ContinuationToken = retrieveUpdatedDataHubEntitiesResponse.ContinuationToken

                }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

                dataHubEntitiesToProcess.AddRange(retrieveUpdatedDataHubEntitiesResponse.Results.Select(s => (s.id, s.lastUpdated)).ToList());
            }

            #endregion

            if (!dataHubEntitiesToProcess.Any() && request.FromDateTime == null)
            {
                _ = (await mediator.TrySend<UpdateDataverseAgentSyncMarkerResponse>(new UpdateDataverseAgentSyncMarkerRequest()
                {
                    Marker = syncMarkerResponse.SyncMarker,
                    NewValue = syncMarkerResponse.SyncMarker.Value,
                    RunTime = runTime
                }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

                return new NullResponse();
            }

            #region Process Sync

            dataHubEntitiesToProcess = dataHubEntitiesToProcess.OrderBy(o => o.lastUpdated).ToList();
            while (dataHubEntitiesToProcess.Any())
            {
                foreach (var processLock in processLocks)
                {
                    var renewLockResponse = await processingLockService.RenewLockAsync(processLock, cancellationToken);
                    renewLockResponse.ThrowIfUnsuccessful("Failed to renew lock");
                }

                var batch = dataHubEntitiesToProcess.Take(request.BatchSize ?? 500).ToList();

                var syncEntitiesWithDataHubResponse = (await mediator.TrySend<ProcessSyncResponse>(new SyncEntitiesRequest<TDataHubEntity, TDataverseEntity>()
                {
                    CorrelationId = request.CorrelationId,
                    EntityIds = batch.Select(s => s.id).ToList()
                }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

                await ReportSuccessesAndFailures(syncEntitiesWithDataHubResponse.Results, cancellationToken);
                var lastModifiedOnDate = timeService.ToDataHubTimeZone(batch.Where(o => o.lastUpdated != null).MaxBy(o => o.lastUpdated).lastUpdated);

                if (lastModifiedOnDate != null && request.FromDateTime == null)
                {
                    _ = (await mediator.TrySend<UpdateDataverseAgentSyncMarkerResponse>(new UpdateDataverseAgentSyncMarkerRequest()
                    {
                        Marker = syncMarkerResponse.SyncMarker,
                        NewValue = lastModifiedOnDate.Value.ToString("o"),
                        RunTime = runTime
                    }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
                }

                dataHubEntitiesToProcess.RemoveRange(0, batch.Count);
            }

            #endregion

            return new NullResponse();
        }
        finally
        {
            if (syncLock != null)
            {
                await processingLockService.ReleaseLockAsync(syncLock, cancellationToken);
            }
        }
    }
}
