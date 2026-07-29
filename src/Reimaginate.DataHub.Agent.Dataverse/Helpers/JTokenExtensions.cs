using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Helpers;

public static class JTokenExtensions
{
    public static string DataHubEntityId(this JToken jt)
    {
        return jt.Value<string>(nameof(DataHubEntity.id));
    }

    public static string DataHubEntityType(this JToken jt)
    {
        return jt.Value<string>(nameof(DataHubEntity.entityType));
    }

    public static DateTimeOffset DataHubLastUpdated(this JToken jo)
    {
        return jo.Value<DateTimeOffset>(nameof(DataHubEntity.lastUpdated));
    }

    public static JToken RemoveNullValues(this JToken jt)
    {
        switch (jt.Type)
        {
            case JTokenType.Object:
            {
                var copy = new JObject();
                foreach (var prop in jt.Children<JProperty>())
                {
                    var child = prop.Value;
                    if (child.HasValues)
                    {
                        child = RemoveNullValues(child);
                    }
                    if (child.Type != JTokenType.Null)
                    {
                        copy.Add(prop.Name, child);
                    }
                }
                return copy;
            }
            case JTokenType.Array:
            {
                var copy = new JArray();
                foreach (var item in jt.Children())
                {
                    var child = item;
                    if (item.HasValues)
                    {
                        child = RemoveNullValues(child);
                    }
                    if (child.Type != JTokenType.Null)
                    {
                        copy.Add(child);
                    }
                }
                return copy;
            }
            default:
                return jt;
        }
    }
    
    public static bool IsEmpty(this JToken token)
    {
        if (token == null || token.Type == JTokenType.None || token.Type == JTokenType.Null)
        {
            return true;
        }

        if (token.Type == JTokenType.Array && !token.HasValues)
        {
            return true;
        }

        if (token.Type == JTokenType.Object && !token.HasValues)
        {
            return true;
        }

        return false;
    }

}