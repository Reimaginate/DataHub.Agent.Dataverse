using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk.Query;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.GetDataverseAgentMergeMarker;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendMergeFailuresToDataHub;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendMergeSuccessesToDataHub;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.UpdateDataverseAgentMergeMarker;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.ProcessingLockService;
using Reimaginate.ProcessingLockService.Abstractions;
using NullResponse = Reimaginate.Mediator.NullResponse;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeUpdatedDataverseEntities;

public class MergeUpdatedDataverseEntitiesRequestHandler<TDataverseEntity, TDataHubEntity>(IOptions<DataverseAgentOptions> dataverseAgentConfig, IDataverseDataService idataverseDataService, IMediator mediator, ITimeService timeService, IProcessingLockService processingLockService)
    : IHandler<MergeUpdatedDataverseEntitiesRequest<TDataverseEntity, TDataHubEntity>, NullResponse>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
    where TDataHubEntity : DataHubEntity
{
    #region Private Helpers

    private async Task ReportSuccessesAndFailures(List<MergeEntityResult> mergeEntityResults, CancellationToken cancellationToken)
    {
        var failures = mergeEntityResults.Where(w => MergeOutcomes.IsFailure(w.MergeOutcome)).ToList();
        if (failures.Any()) _ = (await mediator.TrySend<NullResponse>(new SendMergeFailuresToDataHubRequest(failures), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        var successes = mergeEntityResults.Where(w => MergeOutcomes.IsSuccess(w.MergeOutcome)).ToList();
        if (successes.Any()) _ = (await mediator.TrySend<NullResponse>(new SendMergeSuccessesToDataHubRequest(successes), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
    }

    #endregion

    public async Task<NullResponse> HandleAsync(MergeUpdatedDataverseEntitiesRequest<TDataverseEntity, TDataHubEntity> request, CancellationToken cancellationToken)
    {
        var getSyncLockResponse = await processingLockService.WaitForLockAsync($"{dataverseAgentConfig.Value.DataSource}/sync/{typeof(TDataHubEntity).Name}", request.CorrelationId, duration: TimeSpan.FromMinutes(5), waitTimeOut: TimeSpan.FromMinutes(5), cancellationToken: cancellationToken);
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

            var entityLogicalName = typeof(TDataverseEntity).GetField("EntityLogicalName")?.GetValue(typeof(TDataverseEntity))?.ToString();

            #region Get merge marker

            var mergeMarkerResponse = (await mediator.TrySend<GetDataverseAgentMergeMarkerResponse>(new GetDataverseAgentMergeMarkerRequest()
            {
                AgentId = dataverseAgentConfig.Value.AgentId,
                DataSourceId = dataverseAgentConfig.Value.DataSource,
                EntityType = entityLogicalName,
                DefaultValue = timeService.Today().ToUniversalTime().ToString(Constants.DateFormats.ISO8601)
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            var mergeMarkerVal = timeService.Parse(mergeMarkerResponse.MergeMarker.Value);

            if (request.FromDateTime != null)
            {
                mergeMarkerVal = timeService.Parse(request.FromDateTime.Value);
            }

            #endregion

            #region Get Source Entities To sync

            var filterExpression = new FilterExpression()
            {
                Conditions = { new ConditionExpression("modifiedon", ConditionOperator.GreaterEqual, mergeMarkerVal.UtcDateTime) }
            };

            var page = 1;
            var orders = new Dictionary<string, OrderType>() { { "modifiedon", OrderType.Ascending } };

            var getDataverseEntitiesResponse = await idataverseDataService.PagedWhereAsync<TDataverseEntity>(filterExpression, page, request.BatchSize, new ColumnSet("modifiedon"), orders: orders, cancellationToken: cancellationToken);
            var sourceEntitiesToProcess = getDataverseEntitiesResponse.Results.Select(s => new { s.Id, ModifiedOn = (DateTime)s.Attributes["modifiedon"] }).ToList();

            while (getDataverseEntitiesResponse.MoreResultsAvailable && (request.Max == -1 || sourceEntitiesToProcess.Count < request.Max))
            {
                foreach (var processLock in processLocks)
                {
                    var renewLockResponse = await processingLockService.RenewLockAsync(processLock, cancellationToken);
                    renewLockResponse.ThrowIfUnsuccessful("Failed to renew lock");
                }

                page++;
                getDataverseEntitiesResponse = await idataverseDataService.PagedWhereAsync<TDataverseEntity>(filterExpression, page, request.BatchSize, new ColumnSet("modifiedon"), orders: orders, continuationToken: getDataverseEntitiesResponse.ContinuationToken, cancellationToken: cancellationToken);
                sourceEntitiesToProcess.AddRange(getDataverseEntitiesResponse.Results.Select(s => new { s.Id, ModifiedOn = (DateTime)s.Attributes["modifiedon"] }));
            }

            #endregion

            if (!sourceEntitiesToProcess.Any())
            {
                _ = (await mediator.TrySend<UpdateDataverseAgentMergeMarkerResponse>(new UpdateDataverseAgentMergeMarkerRequest()
                {
                    Marker = mergeMarkerResponse.MergeMarker,
                    NewValue = mergeMarkerResponse.MergeMarker.Value,
                    RunTime = runTime
                }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

                return new NullResponse();
            }

            #region Process Merge

            sourceEntitiesToProcess = sourceEntitiesToProcess.OrderBy(o => o.ModifiedOn).ToList();
            while (sourceEntitiesToProcess.Any())
            {
                foreach (var processLock in processLocks)
                {
                    var renewLockResponse = await processingLockService.RenewLockAsync(processLock, cancellationToken);
                    renewLockResponse.ThrowIfUnsuccessful("Failed to renew lock");
                }

                var batch = sourceEntitiesToProcess.Take(request.BatchSize).ToList();

                var mergeDataverseEntitiesResponse = (await mediator.TrySend<ProcessMergeResponse>(new MergeEntitiesRequest<TDataverseEntity, TDataHubEntity>()
                {
                    CorrelationId = request.CorrelationId,
                    DataverseEntityIds = batch.Select(s => s.Id).ToList()
                }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

                await ReportSuccessesAndFailures(mergeDataverseEntitiesResponse.Results, cancellationToken);

                var lastModifiedOnDate = batch.MaxBy(s => s.ModifiedOn).ModifiedOn;

                _ = (await mediator.TrySend<UpdateDataverseAgentMergeMarkerResponse>(new UpdateDataverseAgentMergeMarkerRequest()
                {
                    Marker = mergeMarkerResponse.MergeMarker,
                    NewValue = timeService.Parse(lastModifiedOnDate).ToString("o"),
                    RunTime = runTime
                }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

                sourceEntitiesToProcess.RemoveRange(0, batch.Count);
                GC.Collect();
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
