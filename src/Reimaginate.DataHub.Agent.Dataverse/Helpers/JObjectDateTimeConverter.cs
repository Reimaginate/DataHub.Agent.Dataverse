using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Reimaginate.DataHub.Agent.Dataverse.Helpers;

public class JObjectDateTimeConverter : JsonConverter<DateTime?>
{
    public override void WriteJson(JsonWriter writer, DateTime? value, JsonSerializer serializer)
    {
        if (value == null) return;

        var t = JToken.FromObject(value, new JsonSerializer()
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        });
        t.WriteTo(writer);
    }
    public override DateTime? ReadJson(JsonReader reader, Type objectType, DateTime? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        reader.DateParseHandling = DateParseHandling.DateTimeOffset;
        var j = serializer.Deserialize<DateTime?>(reader);
        return j;
    }
}