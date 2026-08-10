using System.Collections.Concurrent;
using JsonDiffPatchDotNet;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecords;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Helpers;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.EnsureReferencedEntitiesAreSyncd;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using Options = JsonDiffPatchDotNet.Options;
// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUpdatedEntities;

public class ProcessUpdatedEntitiesRequestHandler<TDataHubEntity, TDataverseEntity>(IOptions<DataverseAgentOptions> dataverseAgentConfig, IDataHubClient dataHubClient, IMapper mapper, IMediator mediator, ITimeService timeService)
    : IHandler<ProcessUpdatedEntitiesRequest<TDataHubEntity, TDataverseEntity>, ProcessUpdatedEntitiesResponse>
    where TDataHubEntity : DataHubEntity, new()
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity, new()
{
    public async Task<ProcessUpdatedEntitiesResponse> HandleAsync(ProcessUpdatedEntitiesRequest<TDataHubEntity, TDataverseEntity> request, CancellationToken cancellationToken)
    {
        var dataverseAltKey = $"{dataverseAgentConfig.Value.DataSource}.{typeof(TDataverseEntity).Name}".ToLower();
        var entityLogicalName = typeof(TDataverseEntity).GetField("EntityLogicalName")?.GetValue(typeof(TDataverseEntity))?.ToString();
        var relatedEntitiesAttr = typeof(TDataHubEntity).GetRelatedEntityTypeAttribute(dataverseAgentConfig.Value.DataSource);

        var entitiesToUpdate = request.EntitiesToUpdate;
        var dataHubEntitiesDic = entitiesToUpdate.ToDictionary(k => k.id, v => v);
        var syncResultsDic = entitiesToUpdate.ToDictionary(k => k.id, v => new SyncEntityResult()
        {
            SourceEntityType = entityLogicalName,
            DataHubEntityType = typeof(TDataHubEntity).Name,
            DataHubEntityId = v.id,
            SourceEntityId = v.alternateKeys.First(f => f.Key == dataverseAltKey).Value,
            SyncOutcome = SyncOutcomes.NoSourceEntityUpdateToProcess,
            ResultingDataHubEntity = JObject.FromObject(v)
        });

        var syncResults = new ConcurrentDictionary<string, SyncEntityResult>(syncResultsDic);

        var jdp = new JsonDiffPatch(options: new Options()
        {
            TextDiff = TextDiffMode.Simple
        });

        var ignoreProps = new List<string>() { "overriddencreatedon", "createdon", "modifiedon" };

        var dataverseEntitiesToMerge = new ConcurrentBag<Guid>();
        var alreadyMergedDataverseEntities = new ConcurrentBag<Guid>();

        //Check for duplicate contacts

        var idPairs = entitiesToUpdate.SelectMany(entity => entity.alternateKeys.Where(w => w.Key == dataverseAltKey).Select(ak => new { dataverseId = ak.Value, dataHubId = entity.id })).ToList();
        var duplicateDataverseIdPairs = idPairs.GroupBy(g => g.dataverseId).Where(w => w.Count() > 1).ToList();
        if (duplicateDataverseIdPairs.Any())
        {
            foreach (var duplicateGroup in duplicateDataverseIdPairs)
            {
                foreach (var duplicate in duplicateGroup)
                {
                    var syncResult = syncResults[duplicate.dataHubId];
                    syncResult.SyncOutcome = SyncOutcomes.SyncFailed;
                    syncResult.FailureReason = "Duplicate DataHub Entities Detected";
                    idPairs.Remove(duplicate);
                    entitiesToUpdate.Remove(dataHubEntitiesDic[duplicate.dataHubId]);
                }
            }
        }

        var duplicateDataHubIdPairs = idPairs.GroupBy(g => g.dataHubId).Where(w => w.Count() > 1).ToList();
        if (duplicateDataHubIdPairs.Any())
        {
            foreach (var duplicateGroup in duplicateDataHubIdPairs)
            {
                foreach (var duplicate in duplicateGroup)
                {
                    var syncResult = syncResults[duplicate.dataHubId];
                    syncResult.SyncOutcome = SyncOutcomes.SyncFailed;
                    syncResult.FailureReason = "Duplicate DataHub Entities Detected";
                    idPairs.Remove(duplicate);
                    entitiesToUpdate.Remove(dataHubEntitiesDic[duplicate.dataHubId]);
                }
            }
        }


        var dataverseToDataHubEntityIdMap = idPairs.ToDictionary(k => k.dataverseId, v => v.dataHubId);

        var dataverseEntityIds = dataverseToDataHubEntityIdMap.Keys.ToList();

        #region Retrieve the current tracked state of the Dataverse entities from the Data Hub

        var getTrackedEntitiesTask = Task.Run(() => dataHubClient.PostRequestAsync<GetTrackedEntitiesRequest, GetTrackedEntitiesResponse>(new GetTrackedEntitiesRequest()
        {
            DataSource = dataverseAgentConfig.Value.DataSource,
            EntityType = entityLogicalName,
            EntityIds = dataverseEntityIds
        }, cancellationToken), cancellationToken);

        var mappedProperties = relatedEntitiesAttr.GetColumnSet()?.Except(new List<string>() { "id" }).ToList();

        var getDataverseEntitiesTask = Task.Run(async () => (await mediator.TrySend<GetSpecificDataverseEntitiesResponse<TDataverseEntity>>(new GetSpecificDataverseEntitiesRequest<TDataverseEntity>()
        {
            EntityIds = dataverseEntityIds.Select(s => new Guid(s)).ToList(),
            ColumnSet = mappedProperties,
            ThrowOnNotFound = false
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue }, cancellationToken);

        var tasks = new List<Task> { getTrackedEntitiesTask, getDataverseEntitiesTask };

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            //ignore;
        }

        if (getTrackedEntitiesTask.IsFaulted)
        {
            throw new Exception("ERR: " + getTrackedEntitiesTask.Exception.Message);
        }

        if (getDataverseEntitiesTask.IsFaulted)
        {
            throw new Exception("ERR: " + getDataverseEntitiesTask.Exception.Message);
        }


        var getTrackedEntitiesResponse = getTrackedEntitiesTask.Result;
        var getDataverseEntitiesResponse = getDataverseEntitiesTask.Result;

        if (!getDataverseEntitiesResponse.Success)
        {
            //TODO: Handle if Dataverse Entity Not Found

        }

        var dataverseEntitiesDict = getDataverseEntitiesResponse.Results.ToDictionary(k => k.Id, v => v);

        var failures = getTrackedEntitiesResponse.Results.Where(w => !w.Success).ToList();
        if (failures.Any())
        {
            failures.ForEach(f =>
            {
                var dataHubEntityId = dataverseToDataHubEntityIdMap[f.EntityId];
                var syncResult = syncResults[dataHubEntityId];
                syncResult.SyncOutcome = SyncOutcomes.SyncFailed;
                syncResult.FailureReason = f.FailureReason;
                dataverseEntityIds.Remove(f.EntityId);
                entitiesToUpdate.Remove(dataHubEntitiesDic[dataHubEntityId]);
            });
        }

        if (!dataverseEntityIds.Any())
        {
            return new ProcessUpdatedEntitiesResponse()
            {
                SyncResults = syncResults.Values.ToList()
            };
        }

        var trackedEntities = getTrackedEntitiesResponse.Results.Except(failures).ToList();

        #region Add any untracked Dataverse entities to the dataverseEntitiesToMerge collection

        foreach (var trackedEntity in trackedEntities)
        {
            if (trackedEntity.Data == null)
            {
                dataverseEntitiesToMerge.Add(new Guid(trackedEntity.EntityId));
            }
        }

        #endregion

        var trackedDataverseEntities = trackedEntities.Where(w => w.Data != null).Select(s => s.Data.ToObjectIgnoreErrors<TDataHubEntity>()).ToList();

        #endregion

        #region Make sure any other entities referenced by the entities to create exist in Dataverse
        //If a referenced entity doesn't exist in Dataverse then the referencing entity can't be sync'd or the reference could be lost.

        var syncReferencedEntitiesResponse = (await mediator.TrySend<EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TDataverseEntity>>(new EnsureReferencedEntitiesAreSyncdRequest<TDataHubEntity, TDataverseEntity>()
        {
            Entities = entitiesToUpdate.Concat(trackedDataverseEntities).ToList(),
            DependencyTree = request.DependencyTree,
            ResolutionPromises = request.ResolutionPromises
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        if (syncReferencedEntitiesResponse.Failures.Any())
        {
            syncReferencedEntitiesResponse.Failures.ForEach(e =>
            {
                syncResults[e.Entity.id].SyncOutcome = SyncOutcomes.SyncFailed;
                syncResults[e.Entity.id].FailureReason = e.Exception.Message;

                entitiesToUpdate.Remove(dataHubEntitiesDic[e.Entity.id]);
                var dataverseId = e.Entity.alternateKeys.FirstOrDefault(f => f.Key == dataverseAltKey);
                if (dataverseId != null)
                {
                    var dataverseEntityToRemoveFromMerge = dataverseEntitiesToMerge.FirstOrDefault(f => f == new Guid(dataverseId.Value));
                    if (dataverseEntityToRemoveFromMerge != default) alreadyMergedDataverseEntities.Add(dataverseEntityToRemoveFromMerge);
                }
            });
        }

        var referencedEntities = syncReferencedEntitiesResponse.CachedEntities;

        var entityCache = request.Cache.Merge(referencedEntities
            .GroupBy(g => g.Value<string>(nameof(DataHubEntity.entityType)))
            .ToDictionary(k => k.Key, v => (object)v.ToList()));

        #endregion

        var entitiesToUpdateDict = entitiesToUpdate
            .SelectMany(e => e.alternateKeys.Where(w => w.Key == dataverseAltKey).Select(ak => new { Key = ak.Key + "_" + ak.Value, Value = e }))
            .ToDictionary(e => e.Key, e => e.Value);

        var failedItems = new ConcurrentBag<TDataHubEntity>();

        var tasksTest = trackedDataverseEntities.Select(trackedDataverseEntity => Task.Run(async () =>
        {
            var key = dataverseAltKey + "_" + trackedDataverseEntity.id;
            if (!entitiesToUpdateDict.TryGetValue(key, out var dataHubEntity)) return;

            try
            {
                var trackedDataverseEntityAsTDataverse = await mapper.MapAsync<TDataHubEntity, TDataverseEntity>(trackedDataverseEntity, cancellationToken, entityCache);
                trackedDataverseEntityAsTDataverse.Id = new Guid(trackedDataverseEntity.id);

                if (!dataverseEntitiesDict.TryGetValue(trackedDataverseEntityAsTDataverse.Id, out var dataverseEntity))
                {
                    var failedEntityId = dataHubEntity.id;
                    syncResults[failedEntityId].SyncOutcome = SyncOutcomes.SyncFailed;
                    syncResults[failedEntityId].FailureReason = $"Tracked entity {trackedDataverseEntityAsTDataverse.Id} not found in Dataverse";
                    failedItems.Add(dataHubEntity);
                    return;
                }

                var mappedProps = relatedEntitiesAttr.GetMappedPropertiesOut();
                if (mappedProps == null)
                {
                    dataverseEntitiesToMerge.Add(dataverseEntity.Id);
                }
                else
                {
                    var areEqual = await AreDataverseEntitiesEqual(dataverseEntity, trackedDataverseEntityAsTDataverse, mappedProps, ignoreProps, cancellationToken);
                    if (!areEqual.Item1)
                    {
                        dataverseEntitiesToMerge.Add(dataverseEntity.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                var failedEntityId = dataHubEntity.id;
                syncResults[failedEntityId].SyncOutcome = SyncOutcomes.SyncFailed;
                syncResults[failedEntityId].FailureReason = ex.Message;
                failedItems.Add(dataHubEntity);
            }
        }, cancellationToken)).ToList();

        try
        {
            await Task.WhenAll(tasksTest);
        }
        catch (Exception ex)
        {
            // Handle exceptions from tasks
            // Note: This catch block will catch exceptions only if they are unhandled within the tasks themselves.
        }

        if (failedItems.Any())
        {
            entitiesToUpdate.RemoveAll(r => failedItems.Contains(r));
        }

        #region Merge any Dataverse entities that have unmerged updates

        if (dataverseEntitiesToMerge.Any())
        {
            var mergeIds = dataverseEntitiesToMerge.Except(alreadyMergedDataverseEntities).ToList();
            var mergeRequest = new MergeSpecificDataverseEntitiesRequest<TDataverseEntity, TDataHubEntity>()
            {
                CorrelationId = request.CorrelationId,
                EntityIds = mergeIds
            };

            if (mergeRequest.EntityIds.Any())
            {
                var mergeEntitiesResponse = (await mediator.TrySend<ProcessMergeResponse>(mergeRequest, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

                var mergeFailures = mergeEntitiesResponse.Results.Where(a => MergeOutcomes.IsFailure(a.MergeOutcome)).ToList();
                if (mergeFailures.Any())
                {
                    foreach (var mergeEntityResult in mergeFailures)
                    {
                        var dataHubEntity = entitiesToUpdate.FirstOrDefault(f => f.alternateKeys.Any(ak => ak.Key == dataverseAltKey && ak.Value == mergeEntityResult.SourceEntityId));

                        if (dataHubEntity?.id != null)
                        {
                            syncResults[dataHubEntity!.id].SyncOutcome = SyncOutcomes.SyncFailed;
                            syncResults[dataHubEntity.id].FailureReason = $"{Constants.SyncFailureTypes.SourceEntityMergeFailed}: {mergeEntityResult.FailureReason}";
                            entitiesToUpdate = entitiesToUpdate.Where(w => w.id != dataHubEntity.id).ToList();
                        }
                        else
                        {
                            var matchingSyncResults = syncResults.Where(w => w.Value.SourceEntityId == mergeEntityResult.SourceEntityId).ToList();
                            foreach (var syncResult in matchingSyncResults)
                            {
                                syncResult.Value.SyncOutcome = SyncOutcomes.SyncFailed;
                                syncResult.Value.FailureReason = $"{Constants.SyncFailureTypes.SourceEntityMergeFailed}: {mergeEntityResult.FailureReason}";
                                var syncResultDataHubId = syncResult.Value.DataHubEntityId;
                                if (syncResultDataHubId != null)
                                {
                                    entitiesToUpdate = entitiesToUpdate.Where(w => w.id != syncResultDataHubId).ToList();
                                }
                            }
                        }
                    }
                }

                #region Update the cache with the updated entities

                var entityIdsToRefresh = mergeEntitiesResponse.Results.Where(w => MergeOutcomes.IsSuccess(w.MergeOutcome)).Select(s => s.DataHubEntityId).ToList();

                if (entityIdsToRefresh.Any())
                {
                    var getUpdatedDataHubEntitiesResponse = await dataHubClient.PostRequestAsync<GetDataHubEntitiesByIdRequest, GetDataHubEntitiesByIdResponse>(new GetDataHubEntitiesByIdRequest()
                    {
                        EntityType = typeof(TDataHubEntity).Name,
                        EntityIds = entityIdsToRefresh
                    }, cancellationToken);

                    var updatedDataHubEntities = getUpdatedDataHubEntitiesResponse.Results.Select(s => s.ToObjectIgnoreErrors<TDataHubEntity>()).ToList();
                    entitiesToUpdate.RemoveAll(w => updatedDataHubEntities.Select(s => s.id).Contains(w.id));
                    entitiesToUpdate.AddRange(updatedDataHubEntities);

                    updatedDataHubEntities.ForEach(e =>
                    {
                        if (syncResults.TryGetValue(e.id, out var result))
                        {
                            result.ResultingDataHubEntity = JObject.FromObject(e);
                        }
                    });
                }

                #endregion
            }
        }

        #endregion

        var updatedTDataverseEntities = new ConcurrentBag<(TDataverseEntity, TDataverseEntity)>();

        await Parallel.ForEachAsync(entitiesToUpdate, new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 1
        }, async (updatedEntity, ct) =>
        {
            try
            {
                #region Map the DataHub Entity to its Dataverse equivalent

                var updatedDataverseEntity = await mapper.MapAsync<TDataHubEntity, TDataverseEntity>(updatedEntity, ct, entityCache);
                updatedDataverseEntity.Id = new Guid(updatedEntity.alternateKeys.First(f => f.Key == dataverseAltKey).Value);

                #endregion

                #region Calculate any changes that need to be made to the Dataverse entity to align it to the DataHub entity

                if (!dataverseEntitiesDict.TryGetValue(updatedDataverseEntity.Id, out var currentDataverseEntity))
                {
                    var error = $"Tracked entity {updatedDataverseEntity.Id} not found in Dataverse";
                    throw new Exception(error);
                }

                #endregion

                #region Prepare an update to the Dataverse entity if required

                var mappedProps = relatedEntitiesAttr.GetMappedPropertiesOut();

                if (mappedProps == null)
                {
                    updatedTDataverseEntities.Add((updatedDataverseEntity, currentDataverseEntity));
                }
                else
                {
                    var areEqual = await AreDataverseEntitiesEqual(currentDataverseEntity, updatedDataverseEntity, mappedProps, ignoreProps, ct);

                    if (!areEqual.Item1)
                    {
                        var originalDataverseEntity = new TDataverseEntity
                        {
                            Id = currentDataverseEntity.Id,
                            RowVersion = currentDataverseEntity.RowVersion
                        };

                        var changedDataverseEntity = new TDataverseEntity
                        {
                            Id = currentDataverseEntity.Id,
                            RowVersion = currentDataverseEntity.RowVersion
                        };

                        foreach (var diff in areEqual.Item2)
                        {
                            originalDataverseEntity[diff.Item1] = diff.Item2;
                            changedDataverseEntity[diff.Item1] = diff.Item3;
                        }

                        updatedTDataverseEntities.Add((changedDataverseEntity, originalDataverseEntity));
                        syncResultsDic[updatedEntity.id].SyncOutcome = SyncOutcomes.SourceEntityUpdated;
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

            var updatedDataverseEntitiesWithRowVersion = updatedTDataverseEntities.Select(s =>
            {
                var ret = s.Item1;
                ret.RowVersion = s.Item2.RowVersion;
                return s.Item1;
            }).DistinctBy(d => d.Id).ToDictionary(k => k.Id.ToString(), v => v);

            var updateRecordsResponse = (await mediator.TrySend<UpdateDataverseRecordsResponse<TDataverseEntity>>(new UpdateDataverseRecordsCommand<TDataverseEntity>()
            {
                Records = updatedDataverseEntitiesWithRowVersion,
                AutoFetchUpdatedEntities = false
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            #endregion

            #region Optimistically update source entity tracking in DataHub

            var resultingDataverseEntitiesDict = updateRecordsResponse.Results;

            var updateEntityRequests = new List<UpdateEntityRequest>();
            foreach (var updatedTDataverseEntity in updatedTDataverseEntities)
            {
                var resultingEntity = resultingDataverseEntitiesDict[updatedTDataverseEntity.Item1.Id.ToString()].ResultingEntity;
                var timestamp = timeService.ToDataHubTimeZone((DateTime)resultingEntity.Attributes["modifiedon"]);
                updatedTDataverseEntity.Item1.Attributes.Remove("modifiedon");

                var fromDHEntity = await mapper.MapAsync<TDataHubEntity>(updatedTDataverseEntity.Item2, cancellationToken, entityCache);
                var toDHEntity = await mapper.MapAsync<TDataHubEntity>(updatedTDataverseEntity.Item1, cancellationToken, entityCache);
                var diffs = jdp.Diff(JObject.FromObject(fromDHEntity).RemoveNullValues(), JObject.FromObject(toDHEntity).RemoveNullValues());

                if (diffs is null or { HasValues: false }) continue;

                if (updateEntityRequests.All(a => a.EntityId != updatedTDataverseEntity.Item1.Id.ToString()))
                {
                    updateEntityRequests.Add(new UpdateEntityRequest()
                    {
                        UpdateType = "Update",
                        DataSource = dataverseAgentConfig.Value.DataSource,
                        EntityType = entityLogicalName,
                        EntityId = updatedTDataverseEntity.Item1.Id.ToString(),
                        Timestamp = timestamp,
                        Data = (JObject)diffs,
                        ReturnResultingEntity = false
                    });
                }
            }

            if (updateEntityRequests.Any())
            {
                var optimisticUpdateRequest = new UpdateEntitiesRequest()
                {
                    CorrelationId = request.CorrelationId,
                    Requests = updateEntityRequests
                };

                await dataHubClient.PostRequestAsync<UpdateEntitiesRequest, UpdateEntitiesResponse>(optimisticUpdateRequest, cancellationToken);
            }

            #endregion
        }

        return new ProcessUpdatedEntitiesResponse()
        {
            SyncResults = syncResults.Values.ToList()
        };
    }

    private async Task<(bool, List<Tuple<string, object, object>>)> AreDataverseEntitiesEqual(TDataverseEntity dataverseEntity1, TDataverseEntity dataverseEntity2, List<string> propertiesToConsider, List<string> ignoreProps, CancellationToken cancellationToken)
    {
        var diffs = new List<Tuple<string, object, object>>();

        var dataverseEntity1Keys = dataverseEntity1.Attributes.Where(w => w.Value != null).Select(s => s.Key).ToList();
        var dataverseEntity2Keys = dataverseEntity2.Attributes.Where(w => w.Value != null).Select(s => s.Key).ToList();

        var sharedAttributes = dataverseEntity1Keys.Intersect(dataverseEntity2Keys).ToList();
        var propResults = new ConcurrentBag<bool>();

        await Parallel.ForEachAsync(sharedAttributes, cancellationToken, (attKey, _) =>
        {
            var dataverseEntity1Val = dataverseEntity1.Attributes[attKey];
            var dataverseEntity2Val = dataverseEntity2.Attributes[attKey];
            var propEqual = dataverseEntity1Val == null && dataverseEntity2Val == null || dataverseEntity1Val != null && dataverseEntity1Val.Equals(dataverseEntity2Val) || dataverseEntity2Val != null && dataverseEntity2Val.Equals(dataverseEntity1Val);
            propResults.Add(propEqual);

            if (!propEqual)
            {
                diffs.Add(new(attKey, dataverseEntity1Val, dataverseEntity2Val));
            }

            return ValueTask.CompletedTask;
        });

        var areEqual = propResults.All(r => r);

        var uniqueToDataverseEntity1 = dataverseEntity1Keys.Except(dataverseEntity2Keys).ToList();
        var uniqueToDataverseEntity2 = dataverseEntity2Keys.Except(dataverseEntity1Keys).ToList();

        var mutuallyExclusiveKeys = uniqueToDataverseEntity1.Concat(uniqueToDataverseEntity2).Where(propertiesToConsider.Contains).Except(ignoreProps).ToList();
        if (mutuallyExclusiveKeys.Any())
        {
            areEqual = false;
            foreach (var key in mutuallyExclusiveKeys)
            {
                diffs.Add(new(key, dataverseEntity1.Attributes.ContainsKey(key) ? dataverseEntity1.Attributes[key] : null, dataverseEntity2.Attributes.ContainsKey(key) ? dataverseEntity2.Attributes[key] : null));
            }
        }

        return (areEqual, diffs);
    }
}
