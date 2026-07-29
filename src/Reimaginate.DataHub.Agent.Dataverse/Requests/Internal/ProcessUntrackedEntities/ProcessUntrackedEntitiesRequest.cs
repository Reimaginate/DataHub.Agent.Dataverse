using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUntrackedEntities;

public class ProcessUntrackedEntitiesRequest<TDataHubEntity, TDataverseEntity> : IRequest<ProcessUntrackedEntitiesResponse> where TDataHubEntity : DataHubEntity where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public string CorrelationId { get; set; }
    public List<TDataHubEntity> EntitiesToUpdate { get; set; }
    public List<EntityReference> DependencyTree { get; set; } = new();
    public List<ResolutionPromise> ResolutionPromises { get; set; } = new();
    public Dictionary<string, object> Cache { get; set; } = new();
}