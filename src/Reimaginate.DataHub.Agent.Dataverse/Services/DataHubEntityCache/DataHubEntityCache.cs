using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;

namespace Reimaginate.DataHub.Agent.Dataverse.Services.DataHubEntityCache;

public interface IDataHubEntityCache
{
    Task<List<TDataHub>> GetDataHubEntities<TDataHub>(List<string> entityIds, CancellationToken cancellationToken, bool? retryNulls = false) where TDataHub : DataHubEntity;
    Task<List<JObject>> GetDataHubEntities(string entityType, List<string> entityIds, CancellationToken cancellationToken, bool? retryNulls = false);
    void InvalidateCacheEntries<TDataHub>(List<string> entityIds) where TDataHub : DataHubEntity;
    void InvalidateCacheEntries(Type dataHubEntityType, List<string> entityIds);
    void InvalidateCacheEntries(string dataHubEntityTypeName, List<string> entityIds);
}

public class DataHubEntityCache(IDataHubClient dataHubClient) : IDataHubEntityCache
{
    private ConcurrentDictionary<string, object> Cache { get; set; } = new();

    public async Task<List<TDataHub>> GetDataHubEntities<TDataHub>(List<string> entityIds, CancellationToken cancellationToken, bool? retryNulls = false) where TDataHub : DataHubEntity
    {
        var foundInCache = entityIds.Where(w => Cache.ContainsKey($"{typeof(TDataHub).Name}.{w}")).ToList();
        var notFoundInCache = entityIds.Except(foundInCache).ToList();

        if (notFoundInCache.Any())
        {
            var getDataHubEntitiesResponse = await dataHubClient.PostRequestAsync<GetDataHubEntitiesByIdRequest, GetDataHubEntitiesByIdResponse>(new GetDataHubEntitiesByIdRequest()
            {
                EntityType = typeof(TDataHub).Name,
                EntityIds = notFoundInCache,
            }, cancellationToken);

            var results = getDataHubEntitiesResponse.Results.Select(s => s.ToObject<TDataHub>()).ToList();
            foreach (var result in results)
            {
                Cache.TryAdd($"{typeof(TDataHub).Name}.{result.id}", result);
            }

            if (retryNulls == false)
            {
                var stillNotFound = notFoundInCache.Except(results.Select(s => s.id)).ToList();
                foreach (var entityId in stillNotFound)
                {
                    Cache.TryAdd($"{typeof(TDataHub).Name}.{entityId}", null);
                }
            }
        }

        var ret = entityIds.Select(s => (TDataHub)Cache[$"{typeof(TDataHub).Name}.{s}"]).Where(w => w != null).ToList();
        return ret;
    }

    public async Task<List<JObject>> GetDataHubEntities(string entityType, List<string> entityIds, CancellationToken cancellationToken, bool? retryNulls = false)
    {
        var foundInCache = entityIds.Where(w => Cache.ContainsKey($"{entityType}.{w}")).ToList();
        var notFoundInCache = entityIds.Except(foundInCache).Distinct().ToList();

        if (notFoundInCache.Any())
        {
            var getDataHubEntitiesResponse = await dataHubClient.PostRequestAsync<GetDataHubEntitiesByIdRequest, GetDataHubEntitiesByIdResponse>(new GetDataHubEntitiesByIdRequest()
            {
                EntityType = entityType,
                EntityIds = notFoundInCache,
            }, cancellationToken);

            var results = getDataHubEntitiesResponse.Results.ToList();
            foreach (var result in results)
            {
                var id = result.Value<string>(nameof(DataHubEntity.id));
                if (!Cache.ContainsKey($"{entityType}.{id}"))
                    Cache.TryAdd($"{entityType}.{id}", result);
            }

            if (retryNulls == false)
            {
                var stillNotFound = notFoundInCache.Except(results.Select(s => s.Value<string>(nameof(DataHubEntity.id)))).ToList();
                foreach (var entityId in stillNotFound)
                {
                    Cache.TryAdd($"{entityType}.{entityId}", null);
                }
            }
        }

        var ret = entityIds.Select(s =>
        {
            var o = Cache[$"{entityType}.{s}"];
            if (o == null) return null;
            return o.GetType() == typeof(JObject) ? (JObject)o : JObject.FromObject(o);
        }).Where(w => w != null).ToList();
        return ret;
    }

    public void InvalidateCacheEntries<TDataHub>(List<string> entityIds) where TDataHub : DataHubEntity
    {
        if (!entityIds.Any()) return;

        var keys = entityIds.Select(s => $"{typeof(TDataHub).Name}.{s}").ToList();
        var cacheEntries = Cache.Where(w => keys.Contains(w.Key)).ToList();
        cacheEntries.ForEach(entry => Cache.Remove(entry.Key, out _));
    }

    public void InvalidateCacheEntries(Type dataHubEntityType, List<string> entityIds)
    {
        if (!entityIds.Any()) return;

        var keys = entityIds.Select(s => $"{dataHubEntityType.Name}.{s}").ToList();
        var cacheEntries = Cache.Where(w => keys.Contains(w.Key)).ToList();
        cacheEntries.ForEach(entry => Cache.Remove(entry.Key, out _));
    }

    public void InvalidateCacheEntries(string dataHubEntityTypeName, List<string> entityIds)
    {
        if (!entityIds.Any()) return;

        var keys = entityIds.Select(s => $"{dataHubEntityTypeName}.{s}").ToList();
        var cacheEntries = Cache.Where(w => keys.Contains(w.Key)).ToList();
        cacheEntries.ForEach(entry => Cache.Remove(entry.Key, out _));
    }
}