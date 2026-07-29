using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Helpers;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Core.Models.Events;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseDeletionEvents;

public class ProcessDataverseDeletionEventsRequestHandler(IDataHubClient dataHubClient, IOptions<DataverseAgentOptions> dataverseAgentOptions)
    : IHandler<ProcessDataverseDeletionEventsRequest, ProcessDataverseDeletionEventsResponse>
{
    public async Task<ProcessDataverseDeletionEventsResponse> HandleAsync(ProcessDataverseDeletionEventsRequest request, CancellationToken cancellationToken)
    {
        var resolveDataHubEntitiesReq = new ResolveEntityReferencesRequest()
        {
            EntityReferences = request.Events.Select(deletion =>
            {
                var dataHubEntityType = request.DataHubAssemblyMarker.Assembly.GetExportedTypes().FirstOrDefault(dhType =>
                {
                    var atts = dhType.GetCustomAttributes(typeof(RelatedEntityTypeAttribute), true);
                    return atts.Any(att =>
                    {
                        var ret = (RelatedEntityTypeAttribute)att;
                        var dataverseType = Type.GetType(ret.TypeName);
                        if (dataverseType == null) return false;

                        var entityLogicalName = dataverseType!.GetField("EntityLogicalName")?.GetValue(dataverseType)?.ToString();
                        var dataSource = dataverseAgentOptions.Value.DataSource;
                        return ret.DataSource == dataSource && entityLogicalName?.ToLower() == deletion.EntityType.ToLower();
                    });
                });

                if (dataHubEntityType == null) return null;

                return new ExternalEntityReference()
                {
                    DataSource = dataverseAgentOptions.Value.DataSource,
                    EntityType = dataHubEntityType.Name,
                    SourceEntityType = deletion.EntityType,
                    EntityId = deletion.EntityId
                };
            }).Where(w => w != null).ToList()
        };

        if (!resolveDataHubEntitiesReq.EntityReferences.Any())
        {
            return new ProcessDataverseDeletionEventsResponse()
            {
                Success = true
            };
        }


        var resolveEntitiesResponse = await dataHubClient.PostRequestAsync<ResolveEntityReferencesRequest, ResolveEntityReferencesResponse>(resolveDataHubEntitiesReq, cancellationToken);
        if (resolveEntitiesResponse.ResolutionFailures.Any())
        {
            //TODO: Determine what to do here
        }

        var failedMessages = new List<string>();

        var entityRefs = resolveEntitiesResponse.Results;
        if (entityRefs.Any())
        {
            var entityIdsDic = entityRefs.ToDictionary(k => $"{k.DataHubEntityReference.EntityId}", v => $"{v.SourceEntityReference.EntityId}");

            var entityTypeGroups = entityRefs.GroupBy(g => new { DataHubEntityType = g.DataHubEntityReference.EntityType, g.SourceEntityReference.SourceEntityType });
            foreach (var entityTypeGroup in entityTypeGroups)
            {
                var getDataHubEntitiesResponse = await dataHubClient.PostRequestAsync<GetDataHubEntitiesByIdRequest, GetDataHubEntitiesByIdResponse>(new GetDataHubEntitiesByIdRequest()
                {
                    EntityType = entityTypeGroup.Key.DataHubEntityType,
                    EntityIds = entityTypeGroup.Select(s => s.DataHubEntityReference.EntityId).ToList()

                }, cancellationToken);

                var dataHubEntities = getDataHubEntitiesResponse.Results;

                var patchRequests = dataHubEntities.Select((entity, i) =>
                {
                    var alternateKeys = entity.Value<JArray>(nameof(DataHubEntity.alternateKeys));
                    var altKey = $"dataverse.{entityTypeGroup.Key.SourceEntityType.ToLower()}";
                    var dataverseAltKey = alternateKeys.FirstOrDefault(f => f.Value<string>(nameof(AlternateKey.Key)) == altKey);
                    if (dataverseAltKey != null)
                    {
                        alternateKeys.Remove(dataverseAltKey);
                    }

                    var syncBlacklist = entity.Value<JArray>(nameof(DataHubEntity.syncBlacklist));
                    syncBlacklist ??= [];
                    if (!syncBlacklist.Contains(new JValue("dataverse")))
                    {
                        syncBlacklist.Add("dataverse");
                    }

                    var ret = new PatchEntityRequest()
                    {
                        DataSource = DataSources.DataHub,
                        EntityType = entity.DataHubEntityType(),
                        EntityId = entity.DataHubEntityId(),
                        Operations =
                        [
                            new Patch
                            {
                                Path =nameof(DataHubEntity.syncBlacklist), Operation = "set", Value = syncBlacklist
                            },
                            new Patch
                            {
                                Path = nameof(DataHubEntity.alternateKeys), Operation = "set", Value = alternateKeys
                            }
                        ]
                    };

                    return ret;

                }).ToList();

                var patchReq = new PatchEntitiesRequest()
                {
                    Requests = patchRequests,
                    Silent = true,
                    DispatchNotifications = false
                };

                var patchEntitiesResponse = await dataHubClient.PostRequestAsync<PatchEntitiesRequest, PatchEntitiesResponse>(patchReq, cancellationToken);
                if (patchEntitiesResponse.Success) continue;

                var batchFailures = patchEntitiesResponse.Results.Where(w => !w.Success).ToList();
                if (!batchFailures.Any()) continue;

                var patchFailures = batchFailures.Select(s => new PatchFailure()
                {
                    Timestamp = DateTime.Now,
                    EntityId = s.PatchRequest.EntityId,
                    EntityType = s.PatchRequest.EntityType,
                    Patch = JArray.FromObject(s.PatchRequest.Operations),
                    DataSource = s.PatchRequest.DataSource,
                    FailureReason = s.FailureReason,
                    EventSource = typeof(ProcessDataverseDeletionEventsRequestHandler).FullName

                }).ToList();

                await dataHubClient.PostRequestAsync<RegisterPatchFailuresRequest, NullResponse>(new RegisterPatchFailuresRequest()
                {
                    PatchFailures = patchFailures
                }, cancellationToken);

                failedMessages.AddRange(batchFailures.Select(s => entityIdsDic[s.PatchRequest.EntityId]));
            }
        }

        if (failedMessages.Any())
        {
            return new ProcessDataverseDeletionEventsResponse()
            {
                Success = false,
                FailureReason = "ONE_OR_MORE_MESSAGES_FAILED",
                FailedMessages = failedMessages
            };
        }

        return new ProcessDataverseDeletionEventsResponse()
        {
            Success = true
        };
    }
}