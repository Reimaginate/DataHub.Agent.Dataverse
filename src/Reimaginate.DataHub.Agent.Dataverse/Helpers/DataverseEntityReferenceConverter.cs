using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Reimaginate.DataHub.Agent.Dataverse.Helpers;

public class DataverseEntityReferenceConverter : JsonConverter<EntityReference>
{
    public override void WriteJson(JsonWriter writer, EntityReference value, JsonSerializer serializer)
    {
        var t = JToken.FromObject(value, new JsonSerializer()
        {
            ContractResolver = new DataverseEntityReferenceResolver()
        });
        t.WriteTo(writer);
    }
    public override EntityReference ReadJson(JsonReader reader, Type objectType, EntityReference existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        reader.DateParseHandling = DateParseHandling.DateTimeOffset;
        var j = serializer.Deserialize<EntityReference>(reader);
        return j;
    }
}