using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Helpers;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeReferencedEntities;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mapper;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;

public class ProcessMergeRequestHandler<TDataverseEntity, TDataHubEntity>(IOptions<DataverseAgentOptions> config, IDataHubClient dataHubClient, IMediator mediator, IMapper mapper)
    : IHandler<ProcessMergeRequest<TDataverseEntity, TDataHubEntity>, ProcessMergeResponse>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
    where TDataHubEntity : DataHubEntity
{
    public async Task<ProcessMergeResponse> HandleAsync(ProcessMergeRequest<TDataverseEntity, TDataHubEntity> request, CancellationToken cancellationToken)
    {
        if (!request.DataverseEntities.Any())
        {
            return new ProcessMergeResponse()
            {
                Results = []
            };
        }

        var entityLogicalName = typeof(TDataverseEntity).GetField("EntityLogicalName")?.GetValue(typeof(TDataverseEntity))?.ToString();

        var mappedDataverseEntities = await mapper.MapAsync<List<TDataHubEntity>>(request.DataverseEntities, cancellationToken, request.Cache);

        var externalEntityReferences = mappedDataverseEntities
            .Select(entity => JObject.FromObject(entity).ExtractExternalEntityReferences())
            .SelectMany(entityRefs => entityRefs)
            .DistinctBy(d => $"{d.DataSource}_{d.EntityType}_{d.SourceEntityType}_{d.EntityId}")
            .ToList();

        List<ResolvedEntityReference> resolvedEntityRefs = null;

        if (externalEntityReferences.Any())
        {
            var mergeReferencedEntitiesResponse = (await mediator.TrySend<MergeReferencedEntitiesResponse>(new MergeReferencedEntitiesRequest<TDataverseEntity, TDataHubEntity>()
            {
                ReferencedEntities = externalEntityReferences,
                CorrelationId = request.CorrelationId,
                DependencyTree = request.DependencyTree
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            resolvedEntityRefs = mergeReferencedEntitiesResponse.ResolvedEntityReferences;
        }

        var mergeRequests = mappedDataverseEntities.Select(s =>
        {
            var dataHubEntityJObject = JObject.FromObject(s);
            var entityRefs = dataHubEntityJObject.ExtractExternalEntityReferenceTokens();

            foreach (var entityRef in entityRefs)
            {
                var resolvedEntity = resolvedEntityRefs?.FirstOrDefault(f => f.SourceEntityReference.EntityId == entityRef.Value<string>(nameof(ExternalEntityReference.EntityId)));
                if (resolvedEntity != null)
                {
                    entityRef.Replace(JObject.FromObject(new EntityReference()
                    {
                        EntityType = resolvedEntity.DataHubEntityReference.EntityType,
                        EntityId = resolvedEntity.DataHubEntityReference.EntityId
                    }));
                }
            }

            return new MergeEntityRequest()
            {
                DataSource = config.Value.DataSource,
                DataHubEntityType = typeof(TDataHubEntity).Name,
                SourceEntityType = entityLogicalName,
                SourceEntityId = s.id,
                Data = dataHubEntityJObject
            };
        }).ToList();


        MergeEntitiesResponse response;
      
        var doNotTrackAttributes = typeof(TDataHubEntity).GetCustomAttributes(typeof(DoNotTrackAttribute), true);
        if (doNotTrackAttributes.Any(a => ((DoNotTrackAttribute)a).DoNotTrack))
        {
            response = await dataHubClient.PostRequestAsync<MergeUntrackedEntitiesRequest, MergeEntitiesResponse>(new MergeUntrackedEntitiesRequest()
            {
                DataSource = config.Value.DataSource,
                Requests = mergeRequests,
                CorrelationId = request.CorrelationId
            }, cancellationToken);
        }
        else
        {
            response = await dataHubClient.PostRequestAsync<MergeEntitiesRequest, MergeEntitiesResponse>(new MergeEntitiesRequest()
            {
                DataSource = config.Value.DataSource,
                Requests = mergeRequests,
                CorrelationId = request.CorrelationId
            }, cancellationToken);
        }
        
        return new ProcessMergeResponse()
        {
            Results = response.Results
        };
    }
}