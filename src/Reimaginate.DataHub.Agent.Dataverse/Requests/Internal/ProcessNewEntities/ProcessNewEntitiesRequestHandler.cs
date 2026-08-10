using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.CreateDataverseRecords;
using Reimaginate.DataHub.Agent.Dataverse.Helpers;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.EnsureReferencedEntitiesAreSyncd;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ResolveResolutionPromises;
using Reimaginate.DataHub.Agent.Dataverse.Services.DataHubEntityCache;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mapper;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessNewEntities;

public class ProcessNewEntitiesRequestHandler<TDataHubEntity, TDataverseEntity>(IOptions<DataverseAgentOptions> dataverseAgentConfig, IDataHubClient dataHubClient, IDataHubEntityCache dataHubEntityCache, IMapper mapper, IMediator mediator)
    : IHandler<ProcessNewEntitiesRequest<TDataHubEntity, TDataverseEntity>, ProcessNewEntitiesResponse>
    where TDataHubEntity : DataHubEntity, new()
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity, new()
{
    public async Task<ProcessNewEntitiesResponse> HandleAsync(ProcessNewEntitiesRequest<TDataHubEntity, TDataverseEntity> request, CancellationToken cancellationToken)
    {
        var entitiesToCreate = request.EntitiesToCreate;

        var entityLogicalName = typeof(TDataverseEntity).GetField("EntityLogicalName")?.GetValue(typeof(TDataverseEntity))?.ToString();

        var syncResults = new ConcurrentDictionary<string, SyncEntityResult>(entitiesToCreate.ToDictionary(k => k.id, v => new SyncEntityResult()
        {
            SourceEntityType = entityLogicalName,
            DataHubEntityType = typeof(TDataHubEntity).Name,
            DataHubEntityId = v.id,
            SyncOutcome = SyncOutcomes.NewSourceEntityCreated
        }));

        var isUntrackedEntity = typeof(TDataHubEntity).GetCustomAttributes(typeof(DoNotTrackAttribute), true).Any();
        var resolutionPromises = new List<ResolutionPromise>(request.ResolutionPromises);


        #region Make sure any other entities referenced by the entities to create exist in Dataverse
        //If a referenced entity doesn't exist in Dataverse then the referencing entity can't be sync'd or the reference could be lost.  

        var ensureReferencedEntitiesAreSyncdResponse = (await mediator.TrySend<EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TDataverseEntity>>(new EnsureReferencedEntitiesAreSyncdRequest<TDataHubEntity, TDataverseEntity>()
        {
            Entities = entitiesToCreate,
            DependencyTree = request.DependencyTree,
            ResolutionPromises = request.ResolutionPromises
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        if (ensureReferencedEntitiesAreSyncdResponse.Failures.Any())
        {
            ensureReferencedEntitiesAreSyncdResponse.Failures.ForEach(e =>
            {
                syncResults[e.Entity.id].SyncOutcome = SyncOutcomes.SyncFailed;
                syncResults[e.Entity.id].FailureReason = e.Exception?.Message;
                entitiesToCreate.Remove(entitiesToCreate.Find(f => f.id == e.Entity.id));
            });
        }

        var referencedEntities = ensureReferencedEntitiesAreSyncdResponse.CachedEntities;
        resolutionPromises.AddRange(ensureReferencedEntitiesAreSyncdResponse.ResolutionPromises.Except(resolutionPromises));

        var entityCache = request.Cache.Merge(referencedEntities
            .GroupBy(g => g.Value<string>(nameof(DataHubEntity.entityType)))
            .ToDictionary(k => k.Key, v => (object)v.ToList()));

        #endregion

        #region Convert the entities to create to their Dataverse equivalents

        var entitiesToCreateAsTDataverse = new ConcurrentDictionary<TDataHubEntity, TDataverseEntity>();
        var mappingFailures = new ConcurrentBag<TDataHubEntity>();

        await Parallel.ForEachAsync(entitiesToCreate, new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        }, async (entityToCreate, ct) =>
        {
            try
            {
                var entityToCreateAsTdataverse = await mapper.MapAsync<TDataverseEntity>(entityToCreate, cancellationToken, entityCache);
                entitiesToCreateAsTDataverse.TryAdd(entityToCreate, entityToCreateAsTdataverse);
            }
            catch (Exception ex)
            {
                var failedEntityId = entityToCreate.id;
                syncResults[failedEntityId].SyncOutcome = SyncOutcomes.SyncFailed;
                syncResults[failedEntityId].FailureReason = ex.Message;
                mappingFailures.Add(entityToCreate);
            }

        });

        if (mappingFailures.Any())
        {
            entitiesToCreate = entitiesToCreate.Except(mappingFailures).ToList();
        }

        #endregion

        cancellationToken.ThrowIfCancellationRequested(); //Throw before beginning DB operations if cancellation or shutdown is requested

        #region Create the sync'd entities in Dataverse

        var bulkCreateResponse = (await mediator.TrySend<CreateDataverseRecordsResponse<TDataverseEntity>>(new CreateDataverseRecordsCommand<TDataverseEntity>()
        {
            Records = entitiesToCreateAsTDataverse.ToDictionary(k => k.Key.id, v => v.Value)
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        #region Register creation successes or failures for submission later

        var dataverseToDHIdMap = entitiesToCreateAsTDataverse.Where(w => w.Value.Id != default).ToDictionary(k => k.Value.Id, v => v.Key);

        foreach (var createResult in bulkCreateResponse.Results)
        {
            var syncResult = syncResults[createResult.Key];
            if (!createResult.Value.Success)
            {
                syncResult.SyncOutcome = SyncOutcomes.SyncFailed;
                syncResult.FailureReason = createResult.Value.FailureReason;
                continue;
            }

            syncResult.SourceEntityId = createResult.Value.EntityId.ToString();

            var resultingDHEntity = dataverseToDHIdMap[createResult.Value.EntityId!.Value!];
            resultingDHEntity.alternateKeys ??= new List<AlternateKey>();
            resultingDHEntity.alternateKeys.Add(new AlternateKey($"{dataverseAgentConfig.Value.DataSource}.{entityLogicalName}".ToLower(), createResult.Value.EntityId!.Value!.ToString()));
            syncResult.ResultingDataHubEntity = JObject.FromObject(resultingDHEntity);
            syncResult.ResultingSourceEntity = JObject.FromObject(createResult.Value.ResultingEntity, Serializers.DataverseEntitySerializer);
        }

        #endregion

        #endregion

        var failedEntityCreations = entitiesToCreate.Where(w => syncResults.Where(result => SyncOutcomes.IsFailure(result.Value.SyncOutcome)).Select(s => s.Key).ToList().Contains(w.id));
        var successfullyCreatedEntities = entitiesToCreate.Except(failedEntityCreations).ToList();

        #region Register alternate keys with the Data Hub

        if (successfullyCreatedEntities.Any())
        {
            var registrationRequests = successfullyCreatedEntities.Select((dataHubEntity, i) => new RegisterAlternateKeyRequest()
            {
                EntityType = typeof(TDataHubEntity).Name,
                Untracked = isUntrackedEntity,
                DataHubEntityId = dataHubEntity.id,
                SourceEntityId = syncResults[dataHubEntity.id].SourceEntityId,
                Key = $"{dataverseAgentConfig.Value.DataSource}.{entityLogicalName}".ToLower()
            }).ToList();

            var registerAlternateKeysResponse = await dataHubClient.PostRequestAsync<RegisterAlternateKeysRequest, RegisterAlternateKeysResponse>(new RegisterAlternateKeysRequest()
            {
                CorrelationId = request.CorrelationId,
                Requests = registrationRequests
            }, cancellationToken);

            var alternateKeyRegistrationFailures = registerAlternateKeysResponse.Responses.Where(w => !w.Success).ToList();
            alternateKeyRegistrationFailures.ForEach(failure =>
            {
                var entity = successfullyCreatedEntities[alternateKeyRegistrationFailures.IndexOf(failure)];
                var syncResult = syncResults[entity.id];
                syncResult.SyncOutcome = SyncOutcomes.SyncFailed;
                syncResult.FailureReason = $"Failed to register alternate key: {failure.FailureReason}";
            });
        }

        #endregion

        #region Refresh the entity cache 

        var successfulEntityTypes = successfullyCreatedEntities.GroupBy(g => g.entityType);
        foreach (var entityTypeGroup in successfulEntityTypes)
        {
            dataHubEntityCache.InvalidateCacheEntries<TDataHubEntity>(entityTypeGroup.Select(s => s.id).ToList());
        }

        #endregion

        #region Optimistically update source entity tracking in DataHub

        var successfulCreateResponses = bulkCreateResponse.Results.Where(w => w.Value.Success).ToDictionary(k => k.Key, v => v.Value);
        if (successfulCreateResponses.Any())
        {
            if (!isUntrackedEntity)
            {
                var optimisticUpdateRequest = new UpdateEntitiesRequest()
                {
                    CorrelationId = request.CorrelationId,
                    Requests = successfulCreateResponses.Select(createResult =>
                    {
                        var entity = dataverseToDHIdMap[createResult.Value.EntityId!.Value];

                        var data = JObject.FromObject(entity);
                        data[nameof(DataHubEntity.id)] = createResult.Value.EntityId.ToString();
                        data.Remove(nameof(DataHubEntity._ts));
                        data.Remove(nameof(DataHubEntity.pk));
                        data.Remove(nameof(DataHubEntity._dt));
                        data.Remove(nameof(DataHubEntity.alternateKeys));

                        return new UpdateEntityRequest()
                        {
                            DataSource = dataverseAgentConfig.Value.DataSource,
                            EntityType = entityLogicalName,
                            EntityId = createResult.Value.EntityId.ToString(),
                            Timestamp = entity.lastUpdated,
                            Data = data.RemoveNullValues(),
                            ReturnResultingEntity = false
                        };
                    }).ToList()
                };

                await dataHubClient.PostRequestAsync<UpdateEntitiesRequest, UpdateEntitiesResponse>(optimisticUpdateRequest, cancellationToken);
            }
        }

        #endregion

        #region Process Resolution Promises

        if (resolutionPromises.Any() && successfullyCreatedEntities.Any())
        {
            var resolutionResponse = (await mediator.TrySend<ResolveResolutionPromisesResponse<TDataHubEntity, TDataverseEntity>>(new ResolveResolutionPromisesRequest<TDataHubEntity, TDataverseEntity>()
            {
                EntitiesToResolve = successfullyCreatedEntities,
                ResolutionPromises = resolutionPromises
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            resolutionPromises = resolutionPromises.Except(resolutionResponse.ResolvedPromises).ToList();
            if (resolutionResponse.UpdatedEntities != null)
            {
                foreach (var resolutionResponseUpdatedEntity in resolutionResponse.UpdatedEntities)
                {
                    var mergeSpecificDataverseEntitiesRequestBaseType = typeof(MergeSpecificDataverseEntitiesRequest<,>);
                    var mergeSpecificDataverseEntitiesRequestType = mergeSpecificDataverseEntitiesRequestBaseType.MakeGenericType(resolutionResponseUpdatedEntity.DataverseType, resolutionResponseUpdatedEntity.DataHubType);
                    var d3365Eid = new List<Guid>() { resolutionResponseUpdatedEntity.DataverseEntityId };
                    dataHubEntityCache.InvalidateCacheEntries(resolutionResponseUpdatedEntity.DataHubType, new List<string>() { resolutionResponseUpdatedEntity.DataHubEntityId });
                    dynamic mergeSpecificDataverseEntitiesRequest = Activator.CreateInstance(mergeSpecificDataverseEntitiesRequestType, d3365Eid);
                    _ = (await mediator.SendAsync((IRequest)mergeSpecificDataverseEntitiesRequest, new CancellationToken())) switch { { IsT1: true } result => throw result.AsT1, { AsT0: var mediatorResultValue } => mediatorResultValue };
                }
            }
        }

        #endregion

        return new ProcessNewEntitiesResponse()
        {
            SyncResults = syncResults.Values.ToList(),
            ResolutionPromises = resolutionPromises
        };
    }
}
