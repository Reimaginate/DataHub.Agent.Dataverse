using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk.Query;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Helpers;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Core.Interfaces;
using Reimaginate.Mediator;
using Reimaginate.ProcessingLockService;
using Reimaginate.ProcessingLockService.Abstractions;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeEntities;

public class MergeEntitiesRequestHandler<TDataverseEntity, TDataHubEntity>(IOptions<DataverseAgentOptions> dataverseAgentConfig, IMediator mediator, IProcessingLockService processingLockService, IServiceProvider serviceProvider, ITimeService timeService)
    : IHandler<MergeEntitiesRequest<TDataverseEntity, TDataHubEntity>, ProcessMergeResponse>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
    where TDataHubEntity : DataHubEntity
{
    public async Task<ProcessMergeResponse> HandleAsync(MergeEntitiesRequest<TDataverseEntity, TDataHubEntity> request, CancellationToken cancellationToken)
    {
        if (!request.DataverseEntityIds.Any()) return new ProcessMergeResponse();

        List<ProcessingLock> dataverseEntityLocks = null;

        try
        {
            var entityIds = request.DataverseEntityIds.Distinct().ToList();
            if (entityIds.Count > 5000) throw new Exception("Merge Entities Request supports a maximum of 5000 entities per call");
            var lockIds = entityIds.Select(entityId => $"entities/DataHub/{DataSources.DataHub}/{entityId}").ToList();
            var getLocksResponse = await processingLockService.WaitForLocksAsync(lockIds, request.CorrelationId, duration:TimeSpan.FromMinutes(5), waitTimeOut: TimeSpan.FromMinutes(5), cancellationToken: cancellationToken);
            getLocksResponse.ThrowIfUnsuccessful();
            dataverseEntityLocks = getLocksResponse.Result;

            var relatedEntitiesAttr = typeof(TDataHubEntity).GetRelatedEntityTypeAttribute(dataverseAgentConfig.Value.DataSource);
            var columnSet = relatedEntitiesAttr?.GetColumnSet();
            
            var mergeResults = new List<MergeEntityResult>();

            var response = (await mediator.TrySend<GetSpecificDataverseEntitiesResponse<TDataverseEntity>>(new GetSpecificDataverseEntitiesRequest<TDataverseEntity>() { EntityIds = entityIds, ColumnSet = columnSet, ThrowOnNotFound = false }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
            if (response.NotFound?.Any() ?? false)
            {
                var sourceEntityType = typeof(TDataverseEntity).Name;
                mergeResults.AddRange(response.NotFound.Select(id => new MergeEntityResult()
                {
                    SourceEntityType = sourceEntityType,
                    SourceEntityId = id.ToString(),
                    MergeOutcome = MergeOutcomes.SourceEntityNotFound,
                    FailureReason = $"{sourceEntityType} '{id}' was not found in Dataverse."
                }).ToList());
            }
            
            var dataverseEntities = response!.Results;
            if (request.ForceUpdate)
            {
                dataverseEntities.ForEach(e =>
                {
                    e.Attributes["modifiedon"] = timeService.Now();
                });
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            var mergeRequest = serviceProvider.GetService<ICustomProcessorRequest<ProcessMergeRequest<TDataverseEntity, TDataHubEntity>>>() as ProcessMergeRequest<TDataverseEntity, TDataHubEntity>;
            mergeRequest ??= new ProcessMergeRequest<TDataverseEntity, TDataHubEntity>();

            mergeRequest.CorrelationId = request.CorrelationId;
            mergeRequest.DataverseEntities = dataverseEntities;
            mergeRequest.DependencyTree = request.DependencyTree;

            var mergeResponse = (await mediator.TrySend<ProcessMergeResponse>(mergeRequest, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
            mergeResponse.Results.AddRange(mergeResults);
            return mergeResponse;
        }
        finally
        {
            if (dataverseEntityLocks != null && dataverseEntityLocks.Any())
            {
                await processingLockService.ReleaseLocksAsync(dataverseEntityLocks, cancellationToken);
            }
        }
    }
}
