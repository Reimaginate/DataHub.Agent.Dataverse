using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecords;
using Reimaginate.DataHub.Agent.Dataverse.Services.DataHubEntityCache;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using EntityReference = Reimaginate.DataHub.SharedModels.Core.EntityReference;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ResolveResolutionPromises;

public class ResolveResolutionPromisesRequestHandler<TDataHubEntity, TDataverseSibling>(IDataHubEntityCache dataHubEntityCache, IMapper mapper, IMediator mediator, IOptions<DataverseAgentOptions> dataverseAgentConfig)
    : IHandler<ResolveResolutionPromisesRequest<TDataHubEntity, TDataverseSibling>, ResolveResolutionPromisesResponse<TDataHubEntity, TDataverseSibling>>
    where TDataHubEntity : DataHubEntity
    where TDataverseSibling : Microsoft.Xrm.Sdk.Entity
{
    public async Task<ResolveResolutionPromisesResponse<TDataHubEntity, TDataverseSibling>> HandleAsync(ResolveResolutionPromisesRequest<TDataHubEntity, TDataverseSibling> request, CancellationToken cancellationToken)
    {
        var sourceSystemPrefix = $"{dataverseAgentConfig.Value.DataSource.Trim().ToLowerInvariant()}.";
        var resolvedPromises = new List<ResolutionPromise>();
        List<ResolvedResolutionPromise> updatedEntities = null;

        var resolutionPromises = request.ResolutionPromises;

        var successfullyCreatedEntityKeys = request.EntitiesToResolve.Select(s => $"{s.entityType}.{s.id}").ToList();

        var relevantResolutionPromises = resolutionPromises.Where(w => successfullyCreatedEntityKeys.Contains($"{w.ExternalEntityReference.EntityType}.{w.ExternalEntityReference.EntityId}"));
        foreach (var resolutionPromise in relevantResolutionPromises)
        {
            var referencedEntity = (await dataHubEntityCache.GetDataHubEntities(resolutionPromise.ExternalEntityReference.EntityType, new List<string>() { resolutionPromise.ExternalEntityReference.EntityId }, cancellationToken)).FirstOrDefault();
            if (referencedEntity != null)
            {
                var referencedEntityDataHubType = referencedEntity.Value<string>(nameof(DataHubEntity.entityType));
                var referencedEntityDataHubId = referencedEntity.Value<string>(nameof(DataHubEntity.id));

                var referringEntities = await dataHubEntityCache.GetDataHubEntities(resolutionPromise.DataHubEntityType, new List<string>() { resolutionPromise.DataHubEntityId }, cancellationToken);
                foreach (var referringEntity in referringEntities)
                {
                    var referringEntityAltKeys = referringEntity.Value<JArray>(nameof(DataHubEntity.alternateKeys))?.ToObject<List<AlternateKey>>();
                    var referringEntityDataverseAltKey = referringEntityAltKeys?.FirstOrDefault(f => f.Key.StartsWith(sourceSystemPrefix, StringComparison.Ordinal));
                    if (referringEntityDataverseAltKey != null)
                    {
                        var referringEntityDataverseTypeName = referringEntityDataverseAltKey.Key[sourceSystemPrefix.Length..];
                        var referringEntityDataHubTypeName = referringEntity.Value<string>(nameof(DataHubEntity.entityType));

                        var referringEntityDataverseType = typeof(TDataverseSibling).Assembly.GetType($"{typeof(TDataverseSibling).Namespace}.{referringEntityDataverseTypeName}", true, true);
                        var referringEntityDataHubType = typeof(TDataHubEntity).Assembly.GetType($"{typeof(TDataHubEntity).Namespace}.{referringEntityDataHubTypeName}", true, true);

                        var referringEntityDataverseId = new Guid(referringEntityDataverseAltKey.Value);

                        var referringEntityPatch = Activator.CreateInstance(referringEntityDataHubType!);
                        referringEntityDataHubType.GetProperty(resolutionPromise.EntityReferencePath)!.SetValue(referringEntityPatch, new EntityReference()
                        {
                            EntityType = referencedEntityDataHubType,
                            EntityId = referencedEntityDataHubId
                        });

                        var referringEntityDataverseEntityUpdate = (Entity)await mapper.MapAsync(referringEntityPatch, referringEntityDataverseType, cancellationToken, cache: new Dictionary<string, object>() { { referencedEntityDataHubType, new List<JObject>() { referencedEntity } } });
                        referringEntityDataverseEntityUpdate.Id = referringEntityDataverseId;
                        foreach (var dataverseEAttribute in referringEntityDataverseEntityUpdate.Attributes.Where(w => w.Value == null))
                        {
                            referringEntityDataverseEntityUpdate.Attributes.Remove(dataverseEAttribute);
                        }

                        var updateDataverseRecordsResponse = (await mediator.TrySend<UpdateDataverseRecordsResponse<Entity>>(new UpdateDataverseRecordsCommand()
                        {
                            Records = new Dictionary<string, Entity>() { { referringEntityDataverseEntityUpdate.Id.ToString(), referringEntityDataverseEntityUpdate } },
                            DisableRowVersionCheck = true,
                            AutoFetchUpdatedEntities = true
                        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

                        updatedEntities = updateDataverseRecordsResponse.Results.Values.Where(s => s.ResultingEntity != null).Select(s => new ResolvedResolutionPromise()
                        {
                            DataHubType = referringEntityDataHubType,
                            DataHubEntityId = referencedEntityDataHubId,
                            DataverseType = s.ResultingEntity.GetType(),
                            DataverseEntityId = s.ResultingEntity.Id,
                        }).ToList();

                        resolvedPromises.Add(resolutionPromise);
                    }
                }
            }
        }

        return new ResolveResolutionPromisesResponse<TDataHubEntity, TDataverseSibling>()
        {
            UpdatedEntities = updatedEntities,
            ResolvedPromises = resolvedPromises
        };
    }
}
