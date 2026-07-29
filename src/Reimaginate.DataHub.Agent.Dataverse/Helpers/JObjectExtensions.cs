using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Helpers;

public static class JObjectExtensions
{
    public static JObject RemoveNullValues(this JObject obj)
    {
        return (JObject)JTokenExtensions.RemoveNullValues(obj);
    }

    public static T ToObjectIgnoreErrors<T>(this JObject jObject)
    {
        var ser = new JsonSerializer();
        ser.Error += (_, args) =>
        {
            if (args.ErrorContext.Error.Message.StartsWith("Error reading") || args.ErrorContext.Error.Message.StartsWith("Error converting value"))
            {
                args.ErrorContext.Handled = true;
            }
        };

        return jObject.ToObject<T>(ser);
    }

    public static List<JObject> ExtractExternalEntityReferenceTokens(this JObject source)
    {
        return source.Descendants()
            .OfType<JObject>()
            .Where(w => w.ContainsKey("@Tag") && w.Value<string>("@Tag") == nameof(ExternalEntityReference))
            .ToList();
    }
    public static List<ExternalEntityReference> ExtractExternalEntityReferences(this JObject source)
    {
        var ret =  source.Descendants()
            .OfType<JObject>()
            .Where(w => w.ContainsKey("@Tag") && w.Value<string>("@Tag") == nameof(ExternalEntityReference))
            .Select(entityRef => entityRef.ToObjectIgnoreErrors<ExternalEntityReference>())
            .DistinctBy(d => $"{d.DataSource}_{d.EntityType}_{d.SourceEntityType}_{d.EntityId}");

        return ret.ToList();
    }

    public static string TryGetAlternateKeyValue(this JObject dataHubEntity, string key)
    {
        var alternateKeys = dataHubEntity.Value<JArray>(nameof(DataHubEntity.alternateKeys));
        var match = alternateKeys?.Children().FirstOrDefault(w => w.Value<string>(nameof(AlternateKey.Key)) == key);
        return match?.Value<string>(nameof(AlternateKey.Value));
    }

    public static List<JObject> TryGetSourceSystemAlternateKeys(this JObject dataHubEntity, string key)
    {
        var alternateKeys = dataHubEntity.Value<JArray>(nameof(DataHubEntity.alternateKeys));
        return alternateKeys?.Children()
                   .Where(w => w.Value<string>(nameof(AlternateKey.Key))?.Split('.').FirstOrDefault() == key)
                   .Select(s => (JObject)s)
                   .ToList()
               ?? new List<JObject>();
    }
}