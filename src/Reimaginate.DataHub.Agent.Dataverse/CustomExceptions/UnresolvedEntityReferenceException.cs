using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.CustomExceptions;

public class UnresolvedEntityReferenceException : Exception
{
    public UnresolvedEntityReferenceException(string dataHubEntityId) : base($"Data Hub Entity {dataHubEntityId} has an unresolved entity reference")
    {
    }

    public UnresolvedEntityReferenceException(string dataHubEntityId, Exception innerException = null) : base($"Data Hub Entity {dataHubEntityId} has an unresolved entity reference", innerException)
    {
    }

    public UnresolvedEntityReferenceException(DataHubEntity dataHubEntity, List<ExternalEntityReference> unresolvedEntityReferences = null, Exception innerException = null) : this(dataHubEntity.id, innerException)
    {
        DataHubEntity = dataHubEntity;
        UnresolvedEntityReferences = unresolvedEntityReferences;
    }

    public DataHubEntity DataHubEntity { get; set; }
    public List<ExternalEntityReference> UnresolvedEntityReferences { get; set; }
}