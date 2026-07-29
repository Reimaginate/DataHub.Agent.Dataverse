using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUpdatedEntities;

public class ProcessUpdatedEntitiesRequest<TDataHubEntity, TDataverseEntity> : IRequest<ProcessUpdatedEntitiesResponse> where TDataHubEntity : DataHubEntity where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public string CorrelationId { get; set; }
    public List<TDataHubEntity> EntitiesToUpdate { get; set; }
    public List<EntityReference> DependencyTree { get; set; } = new();
    public List<ResolutionPromise> ResolutionPromises { get; set; } = new();
    public Dictionary<string, object> Cache { get; set; } = new();
}