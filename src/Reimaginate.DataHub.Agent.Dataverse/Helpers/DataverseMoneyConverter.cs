using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Reimaginate.DataHub.Agent.Dataverse.Helpers;

public class DataverseMoneyConverter : JsonConverter<Money>
{
    public override void WriteJson(JsonWriter writer, Money value, JsonSerializer serializer)
    {
        var t = JToken.FromObject(value, new JsonSerializer()
        {
            ContractResolver = new DataverseMoneyResolver()
        });
        t.WriteTo(writer);
    }
    public override Money ReadJson(JsonReader reader, Type objectType, Money existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        reader.DateParseHandling = DateParseHandling.DateTimeOffset;
        var j = serializer.Deserialize<Money>(reader);
        return j;
    }
}