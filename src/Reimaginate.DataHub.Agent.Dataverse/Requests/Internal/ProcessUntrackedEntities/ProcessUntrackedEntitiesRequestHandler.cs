using System.Collections.Concurrent;
using JsonDiffPatchDotNet;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecords;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Helpers;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.EnsureReferencedEntitiesAreSyncd;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using Options = JsonDiffPatchDotNet.Options;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUntrackedEntities;

public class ProcessUntrackedEntitiesRequestHandler<TDataHubEntity, TDataverseEntity>(IOptions<DataverseAgentOptions> dataverseAgentConfig, IDataHubClient dataHubClient, IMapper mapper, IMediator mediator)
    : IHandler<ProcessUntrackedEntitiesRequest<TDataHubEntity, TDataverseEntity>, ProcessUntrackedEntitiesResponse>
    where TDataHubEntity : DataHubEntity, new()
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity, new()
{
    public async Task<ProcessUntrackedEntitiesResponse> HandleAsync(ProcessUntrackedEntitiesRequest<TDataHubEntity, TDataverseEntity> request, CancellationToken cancellationToken)
    {
        var entitiesToUpdate = request.EntitiesToUpdate;

        var entityLogicalName = typeof(TDataverseEntity).GetField("EntityLogicalName")?.GetValue(typeof(TDataverseEntity))?.ToString();

        var syncResults = new ConcurrentDictionary<string, SyncEntityResult>(entitiesToUpdate.ToDictionary(k => k.id, v => new SyncEntityResult()
        {
            SourceEntityType = entityLogicalName,
            DataHubEntityType = typeof(TDataHubEntity).Name,
            DataHubEntityId = v.id,
            SyncOutcome = SyncOutcomes.SourceEntityUpdated
        }));

        var dataverseAltKey = $"{dataverseAgentConfig.Value.DataSource}.{typeof(TDataverseEntity).Name}".ToLower();
        var ignoreProps = new List<string>() { "overriddencreatedon" };

        var jdp = new JsonDiffPatch(options: new Options()
        {
            TextDiff = TextDiffMode.Simple
        });

        var dataverseEntityIds = entitiesToUpdate.SelectMany(entity => entity.alternateKeys.Where(w => w.Key == dataverseAltKey).Select(ak => ak.Value)).ToList();

        #region Retrieve the entities to update from the source system

        var relatedEntitiesAttr = typeof(TDataHubEntity).GetRelatedEntityTypeAttribute(dataverseAgentConfig.Value.DataSource);
        var columnSet = relatedEntitiesAttr.GetColumnSet();

        var getDataverseEntitiesResponse = (await mediator.TrySend<GetSpecificDataverseEntitiesResponse<TDataverseEntity>>(new GetSpecificDataverseEntitiesRequest<TDataverseEntity>()
        {
            EntityIds = dataverseEntityIds.Select(s => new Guid(s)).ToList(),
            ColumnSet = columnSet,
            ThrowOnNotFound = false
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        if (!getDataverseEntitiesResponse.Success)
        {
            //TODO: Handle if Dataverse entity not found
        }

        #endregion

        #region Make sure any other entities referenced by the entities exist in Dataverse
        //If a referenced entity doesn't exist in Dataverse then the referencing entity can't be sync'd or the reference could be lost.

        var syncReferencedEntitiesResponse = (await mediator.TrySend<EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TDataverseEntity>>(new EnsureReferencedEntitiesAreSyncdRequest<TDataHubEntity, TDataverseEntity>()
        {
            Entities = entitiesToUpdate,
            DependencyTree = request.DependencyTree,
            ResolutionPromises = request.ResolutionPromises
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        if (syncReferencedEntitiesResponse.Failures.Any())
        {
            syncReferencedEntitiesResponse.Failures.ForEach(e =>
            {
                syncResults[e.Entity.id].SyncOutcome = SyncOutcomes.SyncFailed;
                syncResults[e.Entity.id].FailureReason = e.Exception.Message;

                entitiesToUpdate.Remove(entitiesToUpdate.Find(f => f.id == e.Entity.id));
            });
        }

        var referencedEntities = syncReferencedEntitiesResponse.CachedEntities;

        var entityCache = request.Cache.Merge(referencedEntities
            .GroupBy(g => g.Value<string>(nameof(DataHubEntity.entityType)))
            .ToDictionary(k => k.Key, v => (object)v.ToList()));

        #endregion

        var foundDataverseEntitiesDict = getDataverseEntitiesResponse.Results.ToDictionary(k => k.Id, v => v);

        var updatedTDataverseEntities = new ConcurrentBag<(TDataverseEntity, JToken, DateTimeOffset?)>();

        var dataverseComparisonSerializer = new JsonSerializer()
        {
            Converters =
            {
                new DataverseEntityReferenceConverter(), new JObjectDateTimeConverter(),
                new DataverseOptionSetValueConverter(), new DataverseMoneyConverter()
            },
            ContractResolver = new DataverseEntityResolver()
        };

        var dataverseUpdateSerializer = new JsonSerializer()
        {
            ContractResolver = new DataverseEntityResolver()
        };

        await Parallel.ForEachAsync(entitiesToUpdate, new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        }, async (updatedEntity, ct) =>
        {
            try
            {
                #region Map the DataHub Entity to its Dataverse equivalent

                var updatedEntityAsTDataverse = await mapper.MapAsync<TDataHubEntity, TDataverseEntity>(updatedEntity, ct, entityCache);
                updatedEntityAsTDataverse.Id = new Guid(updatedEntity.alternateKeys.First(f => f.Key == dataverseAltKey).Value);

                #endregion

                #region Calculate any changes that need to be made to the Dataverse entity to align it to the DataHub entity

                if (!foundDataverseEntitiesDict.TryGetValue(updatedEntityAsTDataverse.Id, out var sourceEntity))
                {
                    var error = $"Tracked entity {updatedEntityAsTDataverse.Id} not found in Dataverse";
                    throw new Exception(error);
                }

                ((DataverseEntityResolver)dataverseComparisonSerializer.ContractResolver).Reset(updatedEntityAsTDataverse.Attributes, ignoreProps);

                var left = JObject.FromObject(sourceEntity!, dataverseComparisonSerializer);
                var right = JObject.FromObject(updatedEntityAsTDataverse, dataverseComparisonSerializer);

                var areEqual = JToken.DeepEquals(left, right);

                #endregion

                #region Prepare an update to the Dataverse entity if required

                if (!areEqual)
                {
                    var dataverseEntityDiffs = jdp.Diff(left, right);
                    if (dataverseEntityDiffs != null)
                    {
                        var updatedEntityData = jdp.Patch(new JObject(), dataverseEntityDiffs);

                        ((DataverseEntityResolver)dataverseUpdateSerializer.ContractResolver).Reset(updatedEntityAsTDataverse.Attributes, ignoreProps);

                        var updatedDataverseEntity = updatedEntityData.ToObject<TDataverseEntity>(dataverseUpdateSerializer);
                        updatedDataverseEntity.Id = sourceEntity.Id;
                        updatedDataverseEntity.RowVersion = sourceEntity.RowVersion;

                        #region Fix issue with diffing Entity References

                        var updatedDataverseEntityType = updatedDataverseEntity.GetType();
                        foreach (var diff in dataverseEntityDiffs)
                        {
                            var p = updatedDataverseEntityType.GetProperty(diff.Path);
                            if (p == null) continue;
                            if (p.PropertyType != typeof(Microsoft.Xrm.Sdk.EntityReference)) continue;

                            var val1 = (Microsoft.Xrm.Sdk.EntityReference)p.GetValue(sourceEntity);
                            var val2 = (Microsoft.Xrm.Sdk.EntityReference)p.GetValue(updatedDataverseEntity);
                            if (val1 != null && val2 is { LogicalName: null }) val2.LogicalName = val1.LogicalName;
                            p.SetValue(updatedDataverseEntity, val2);
                        }

                        #endregion

                        updatedTDataverseEntities.Add((updatedDataverseEntity, dataverseEntityDiffs, updatedEntity.lastUpdated));
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                var syncResult = syncResults[updatedEntity.id];
                syncResult.SyncOutcome = SyncOutcomes.SyncFailed;
                syncResult.FailureReason = ex.Message;
            }
        });

        if (updatedTDataverseEntities.Any())
        {
            cancellationToken.ThrowIfCancellationRequested(); //Throw before attempting to send to the Data Hub if cancellation requested

            #region Update the entities in Dataverse

            _ = (await mediator.TrySend<UpdateDataverseRecordsResponse<TDataverseEntity>>(new UpdateDataverseRecordsCommand<TDataverseEntity>()
            {
                Records = updatedTDataverseEntities.Select(s => s.Item1).ToDictionary(k => k.Id.ToString(), v => v),
                AutoFetchUpdatedEntities = false
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            #endregion

            #region Optimistically update source entity tracking in DataHub

            var optimisticUpdateRequest = new UpdateEntitiesRequest()
            {
                CorrelationId = request.CorrelationId,
                Requests = updatedTDataverseEntities.Select(s => new UpdateEntityRequest()
                {
                    DataSource = "Dataverse",
                    EntityType = s.Item1.LogicalName,
                    EntityId = s.Item1.Id.ToString(),
                    Timestamp = s.Item3,
                    Data = (JObject)s.Item2,
                    ReturnResultingEntity = false
                }).ToList()
            };

            await dataHubClient.PostRequestAsync<UpdateEntitiesRequest, UpdateEntitiesResponse>(optimisticUpdateRequest, cancellationToken);


            #endregion
        }

        return new ProcessUntrackedEntitiesResponse()
        {
            SyncResults = syncResults.Values.ToList()
        };
    }
}