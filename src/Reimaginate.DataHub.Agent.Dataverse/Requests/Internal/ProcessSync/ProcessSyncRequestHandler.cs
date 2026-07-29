using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessNewEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUntrackedEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUpdatedEntities;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using EntityReference = Reimaginate.DataHub.SharedModels.Core.EntityReference;


// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;

public class ProcessSyncRequestHandler<TDataHubEntity, TDataverseEntity>(IOptions<DataverseAgentOptions> dataverseAgentConfig, IMediator mediator)
    : IHandler<ProcessSyncRequest<TDataHubEntity, TDataverseEntity>, ProcessSyncResponse>
    where TDataHubEntity : DataHubEntity, new()
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity, new()
{
    public async Task<ProcessSyncResponse> HandleAsync(ProcessSyncRequest<TDataHubEntity, TDataverseEntity> request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = new List<SyncEntityResult>();

        var dataverseAltKey = $"{dataverseAgentConfig.Value.DataSource}.{typeof(TDataverseEntity).Name}".ToLower();

        var existingEntities = request.DataHubEntities.Where(w => w.alternateKeys?.Any(a => a.Key == dataverseAltKey) ?? false).ToList();
        var newEntities = request.DataHubEntities.Except(existingEntities).ToList();
        var dependencyTree = request.DependencyTree?.Concat(newEntities.Select(s => new EntityReference() { EntityType = typeof(TDataHubEntity).Name, EntityId = s.id })).ToList() ?? new List<EntityReference>();

        var resolutionPromises = new List<ResolutionPromise>();

        if (newEntities.Any())
        {
            var processNewEntitiesResponse = (await mediator.TrySend<ProcessNewEntitiesResponse>(new ProcessNewEntitiesRequest<TDataHubEntity, TDataverseEntity>()
            {
                Cache = request.Cache,
                CorrelationId = request.CorrelationId,
                ResolutionPromises = request.ResolutionPromises,
                DependencyTree = dependencyTree,
                EntitiesToCreate = newEntities
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            resolutionPromises.AddRange(processNewEntitiesResponse.ResolutionPromises);
            results.AddRange(processNewEntitiesResponse.SyncResults);
        }

        if (existingEntities.Any())
        {
            var isUntrackedEntity = typeof(TDataHubEntity).GetCustomAttributes(typeof(DoNotTrackAttribute), true).Any();
            if (isUntrackedEntity)
            {
                var untrackedEntitiesResponse = (await mediator.TrySend<ProcessUntrackedEntitiesResponse>(new ProcessUntrackedEntitiesRequest<TDataHubEntity, TDataverseEntity>()
                {
                    Cache = request.Cache,
                    CorrelationId = request.CorrelationId,
                    ResolutionPromises = resolutionPromises,
                    DependencyTree = dependencyTree,
                    EntitiesToUpdate = existingEntities

                }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

                results.AddRange(untrackedEntitiesResponse.SyncResults);

            }
            else
            {
                var updatedEntitiesResponse = (await mediator.TrySend<ProcessUpdatedEntitiesResponse>(new ProcessUpdatedEntitiesRequest<TDataHubEntity, TDataverseEntity>()
                {
                    Cache = request.Cache,
                    CorrelationId = request.CorrelationId,
                    ResolutionPromises = resolutionPromises,
                    DependencyTree = dependencyTree,
                    EntitiesToUpdate = existingEntities

                }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

                results.AddRange(updatedEntitiesResponse.SyncResults);
            }
        }


        return new ProcessSyncResponse()
        {
            Results = results,
            ResolutionPromises = resolutionPromises
        };
    }
}
