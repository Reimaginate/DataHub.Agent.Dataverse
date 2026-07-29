using Newtonsoft.Json;

namespace Reimaginate.DataHub.Agent.Dataverse.Helpers;

internal static class Serializers
{
    internal static JsonSerializer DataverseEntitySerializer = new JsonSerializer()
    {
        Converters = { new DataverseEntityReferenceConverter() },
        ContractResolver = new DataverseEntityResolver()
    };
}