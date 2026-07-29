using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessNewEntities;

public class ProcessNewEntitiesRequest<TDataHubEntity, TDataverseEntity> : IRequest<ProcessNewEntitiesResponse> where TDataHubEntity : DataHubEntity where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public string CorrelationId { get; set; }
    public List<TDataHubEntity> EntitiesToCreate { get; set; }
    public List<EntityReference> DependencyTree { get; set; } = new();
    public List<ResolutionPromise> ResolutionPromises { get; set; } = new();
    public Dictionary<string, object> Cache { get; set; } = new();
}