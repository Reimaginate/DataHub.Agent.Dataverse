using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Reimaginate.DataHub.Agent.Dataverse.Helpers;

public class DataverseOptionSetValueConverter : JsonConverter<OptionSetValue>
{
    public override void WriteJson(JsonWriter writer, OptionSetValue value, JsonSerializer serializer)
    {
        var t = JToken.FromObject(value, new JsonSerializer()
        {
            ContractResolver = new DataverseOptionSetValueResolver()
        });
        t.WriteTo(writer);
    }
    public override OptionSetValue ReadJson(JsonReader reader, Type objectType, OptionSetValue existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        reader.DateParseHandling = DateParseHandling.DateTimeOffset;
        var j = serializer.Deserialize<OptionSetValue>(reader);
        return j;
    }
}