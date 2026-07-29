using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeEntities;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeReferencedEntities;

public class MergeReferencedEntitiesRequestHandler<TDataverseAssemblyMarker, TDataHubAssemblyMarker>(IDataHubClient dataHubClient, IMediator mediator) : IHandler<MergeReferencedEntitiesRequest<TDataverseAssemblyMarker, TDataHubAssemblyMarker>, MergeReferencedEntitiesResponse>
    where TDataverseAssemblyMarker : Microsoft.Xrm.Sdk.Entity
    where TDataHubAssemblyMarker : DataHubEntity
{
    public async Task<MergeReferencedEntitiesResponse> HandleAsync(MergeReferencedEntitiesRequest<TDataverseAssemblyMarker, TDataHubAssemblyMarker> request, CancellationToken cancellationToken)
    {
        var referencedEntities = request.ReferencedEntities;

        var resolveEntityRefsResponse = await dataHubClient.PostRequestAsync<ResolveEntityReferencesRequest, ResolveEntityReferencesResponse>(new ResolveEntityReferencesRequest()
        {
            EntityReferences = referencedEntities
        }, cancellationToken);

        var resolvedEntityRefs = resolveEntityRefsResponse.Results;

        var foundEntityRefs = referencedEntities.Join(resolvedEntityRefs, a => a.EntityId, b => b.SourceEntityReference.EntityId, (a, b) => a).ToList();
        var missingEntityRefs = referencedEntities.Except(foundEntityRefs).ToList();
        
        if (missingEntityRefs.Any())
        {
            var typeGroups = missingEntityRefs.GroupBy(g => new { g.SourceEntityType, g.EntityType });

            foreach (var typeGroup in typeGroups)
            {
                var dataverseEntityIds = typeGroup.Select(s => new Guid(s.EntityId)).Distinct().ToList();
                var dataverseEntityIdsAsString = dataverseEntityIds.Select(g => g.ToString()).ToList();

                if (request.DependencyTree.Select(s => s.EntityId).Intersect(dataverseEntityIdsAsString).Any()) continue;
                
                try
                {
                    var entityRefDataverseType = typeof(TDataverseAssemblyMarker).Assembly.GetType($"{typeof(TDataverseAssemblyMarker).Namespace}.{typeGroup.Key.SourceEntityType}".ToLower(), false, true);
                    var entityRefDataHubType = typeof(TDataHubAssemblyMarker).Assembly.GetExportedTypes().FirstOrDefault(w => w.Name == typeGroup.Key.EntityType && typeof(DataHubEntity).IsAssignableFrom(w));
                    if (entityRefDataverseType is null || entityRefDataHubType is null)
                    {
                        continue;
                    }

                    var dependencyTree = request.DependencyTree.Concat(missingEntityRefs).ToList();

                    var mergeEntitiesRequestType = typeof(MergeEntitiesRequest<,>);
                    var mergeEntitiesRequestTypeGeneric = mergeEntitiesRequestType.MakeGenericType(entityRefDataverseType!, entityRefDataHubType!);

                    dynamic mergeEntitiesRequest = Activator.CreateInstance(mergeEntitiesRequestTypeGeneric, dataverseEntityIds, dependencyTree, request.CorrelationId);
                    
                    var response = (dynamic)((await mediator.SendAsync((IRequest)mergeEntitiesRequest, cancellationToken)) switch { { IsT1: true } result => throw result.AsT1, { AsT0: var mediatorResultValue } => mediatorResultValue });
                    
                    var results = (List<MergeEntityResult>)response.Results;

                    var successes = results.Where(w => MergeOutcomes.IsSuccess(w.MergeOutcome)).ToList();
                    var failures = results.Except(successes);

                    resolvedEntityRefs.AddRange(successes.Select(s => new ResolvedEntityReference()
                    {
                        DataHubEntityReference = new EntityReference()
                        {
                            EntityId = s.DataHubEntityId,
                            EntityType = s.DataHubEntityType
                        },
                        SourceEntityReference = new ExternalEntityReference()
                        {
                            DataSource = s.DataSource,
                            SourceEntityType = s.SourceEntityType,
                            EntityType = s.SourceEntityType,
                            EntityId = s.SourceEntityId,
                        }
                    }));
                }
                catch (Exception ex)
                {
                    if (!request.DependencyTree.Any())
                    {
                        //Ignore as we do not have a circular dependency
                    }
                    else
                        throw;
                }
            }
        }
        

        return new MergeReferencedEntitiesResponse()
        {
            ResolvedEntityReferences = resolvedEntityRefs
        };
    }
}
