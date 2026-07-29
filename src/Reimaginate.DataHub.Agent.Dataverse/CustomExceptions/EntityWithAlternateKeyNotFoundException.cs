using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.CustomExceptions;

public class EntityWithAlternateKeyNotFoundException : Exception
{
    public EntityWithAlternateKeyNotFoundException(string alternateKey, string sourceEntityId) : base($"Data Hub Entity with alternate key {alternateKey}:{sourceEntityId} not found")
    {
        AlternateKey = alternateKey;
        SourceEntityId = sourceEntityId;
    }

    public EntityWithAlternateKeyNotFoundException(string alternateKey, string sourceEntityId, Exception innerException = null) : base($"Data Hub Entity with alternate key {alternateKey}:{sourceEntityId} not found", innerException)
    {
        AlternateKey = alternateKey;
        SourceEntityId = sourceEntityId;
    }

    public EntityWithAlternateKeyNotFoundException(string altKeyKey, string altKeyValue, List<DataHubEntity> collection = null, Exception innerException = null) : this(altKeyKey, altKeyValue, innerException)
    {
        Collection = collection;
    }

    public string AlternateKey { get; set; }
    public string SourceEntityId { get; set; }
    public List<DataHubEntity> Collection { get; set; }
}