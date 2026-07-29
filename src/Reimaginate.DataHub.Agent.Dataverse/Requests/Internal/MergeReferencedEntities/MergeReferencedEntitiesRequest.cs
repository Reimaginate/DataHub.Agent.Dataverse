using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

// ReSharper disable IdentifierTypo

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeReferencedEntities;

public class MergeReferencedEntitiesRequest<TDataverseAssemblyMarker, TDataHubAssemblyMarker> : IRequest<MergeReferencedEntitiesResponse> where TDataverseAssemblyMarker : Microsoft.Xrm.Sdk.Entity where TDataHubAssemblyMarker : DataHubEntity
{
    public List<ExternalEntityReference> ReferencedEntities { get; set; } = new();

    public List<ExternalEntityReference> DependencyTree { get; set; } = new();

    public string CorrelationId { get; set; }
}