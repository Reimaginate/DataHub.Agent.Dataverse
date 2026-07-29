using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeReferencedEntities;

public class MergeReferencedEntitiesResponse
{
    public List<ResolvedEntityReference> ResolvedEntityReferences { get; set; }
}