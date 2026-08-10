using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Core.Interfaces;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using Reimaginate.ProcessingLockService;
using Reimaginate.ProcessingLockService.Abstractions;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SyncEntities;

public class SyncEntitiesRequestHandler<TDataHubEntity, TDataverseEntity>(IOptions<DataverseAgentOptions> dataverseAgentConfig, IDataHubClient dataHubClient, IMediator mediator, IMapper mapper, IProcessingLockService processingLockService, IServiceProvider serviceProvider)
    : IHandler<SyncEntitiesRequest<TDataHubEntity, TDataverseEntity>, ProcessSyncResponse>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
    where TDataHubEntity : DataHubEntity
{
    private readonly IMapper _mapper = mapper;

    public async Task<ProcessSyncResponse> HandleAsync(SyncEntitiesRequest<TDataHubEntity, TDataverseEntity> request, CancellationToken cancellationToken)
    {
        if (!request.EntityIds.Any()) return new ProcessSyncResponse();

        List<ProcessingLock> dataHubEntityLocks = null;

        try
        {
            var entityIds = request.EntityIds.Distinct().ToList();
            var lockIds = entityIds.Select(entityId => $"entities/{dataverseAgentConfig.Value.DataSource}/{typeof(TDataHubEntity).Name}/{entityId}").ToList();
            var getLocksResponse = await processingLockService.WaitForLocksAsync(lockIds, request.CorrelationId, duration: TimeSpan.FromMinutes(5), waitTimeOut: TimeSpan.FromMinutes(5), cancellationToken: cancellationToken);
            getLocksResponse.ThrowIfUnsuccessful();
            dataHubEntityLocks = getLocksResponse.Result;

            var getDataHubEntitiesByIdResponse = await dataHubClient.PostRequestAsync<GetDataHubEntitiesByIdRequest, GetDataHubEntitiesByIdResponse>(new GetDataHubEntitiesByIdRequest()
            {
                EntityType = typeof(TDataHubEntity).Name,
                EntityIds = entityIds
            }, cancellationToken);

            var entitiesToSync = getDataHubEntitiesByIdResponse.Results;

            // ReSharper disable once SuspiciousTypeConversion.Global
            var syncRequest = serviceProvider.GetService<ICustomProcessorRequest<ProcessSyncRequest<TDataHubEntity, TDataverseEntity>>>() as ProcessSyncRequest<TDataHubEntity, TDataverseEntity>;
            if (syncRequest == null)
            {
                //Prefilter the entities to sync to exclude nosync. If there is a custom processor, let the custom processor handle them.
                var sourceSystemPrefix = dataverseAgentConfig.Value.DataSource.Trim().ToLowerInvariant();
                entitiesToSync = entitiesToSync.Where(w =>
                {
                    var noSync = w.Value<bool>(nameof(DataHubEntity.noSync));
                    var whiteList = w.Value<JArray>(nameof(DataHubEntity.syncWhitelist)) ?? [];
                    var blackList = w.Value<JArray>(nameof(DataHubEntity.syncBlacklist)) ?? [];

                    var doNotSync = (noSync && whiteList.All(a => a.Value<string>() != sourceSystemPrefix)) || blackList.Any(a => a.Value<string>() == sourceSystemPrefix);
                    return !doNotSync;
                }).ToList();

            }

            syncRequest ??= new ProcessSyncRequest<TDataHubEntity, TDataverseEntity>();

            var ser = new JsonSerializer();
            ser.Error += (_, args) =>
            {
                if (args.ErrorContext.Error.Message.StartsWith("Error reading") || args.ErrorContext.Error.Message.StartsWith("Error converting value"))
                {
                    args.ErrorContext.Handled = true;
                }
            };

            syncRequest.CorrelationId = request.CorrelationId;
            syncRequest.DependencyTree = request.DependencyTree;
            syncRequest.DataHubEntities = entitiesToSync.Select(s => s.ToObject<TDataHubEntity>(ser)).ToList();
            syncRequest.ResolutionPromises = request.ResolutionPromises;

            var response = (await mediator.TrySend<ProcessSyncResponse>(syncRequest, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
            return response;
        }
        finally
        {
            if (dataHubEntityLocks != null && dataHubEntityLocks.Any())
            {
                await processingLockService.ReleaseLocksAsync(dataHubEntityLocks, cancellationToken);
            }
        }
    }
}
