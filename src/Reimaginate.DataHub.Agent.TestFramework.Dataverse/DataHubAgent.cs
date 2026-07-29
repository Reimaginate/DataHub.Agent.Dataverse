using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Test.Framework;
using Reimaginate.Test.Framework.Helpers;

namespace Reimaginate.DataHub.Agent.TestFramework.Dataverse;

public class DataHubAgent : TestAgentBase<DataHubAgent>
{
    private const string DefaultDataSource = "DataverseIntegrationSeed";

    public DataHubAgent()
    { }

    public DataHubAgent(IServiceProvider serviceProvider)
    {
        AgentServices = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        HostServices = serviceProvider;
        ActivitySource = DiagnosticConfig.DataHubAgent.ActivitySource;
    }

    public DataHubAgent(Func<ServiceCollection> serviceCollectionBuilder) : base(serviceCollectionBuilder, DiagnosticConfig.DataHubAgent.ActivitySource)
    { }

    private IDataHubClient DataHubClient => AgentServices.GetRequiredService<IDataHubClient>();

    private async Task<TDataHubEntity> createDataHubEntity<TDataHubEntity>(
        object currentObject,
        Dictionary<string, object?> stash,
        Func<object, Dictionary<string, object?>, TDataHubEntity> entityFunc,
        string? stashTo = null,
        string? sourceEntityType = null)
        where TDataHubEntity : DataHubEntity
    {
        var entity = entityFunc(currentObject, stash);
        var data = JObject.FromObject(entity);

        var mergeResponse = await DataHubClient.PostRequestAsync<MergeEntitiesRequest, MergeEntitiesResponse>(
            new MergeEntitiesRequest
            {
                DataSource = DefaultDataSource,
                Requests =
                [
                    new MergeEntityRequest
                    {
                        DataSource = DefaultDataSource,
                        DataHubEntityType = entity.entityType,
                        SourceEntityType = sourceEntityType ?? typeof(TDataHubEntity).Name,
                        SourceEntityId = entity.id,
                        Data = data
                    }
                ]
            },
            CancellationToken.None);

        var entityId = mergeResponse.Results.FirstOrDefault()?.DataHubEntityId;
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new InvalidOperationException("DataHub merge did not return a DataHub entity id.");
        }

        var resultingEntity = await getDataHubEntity<TDataHubEntity>(entityId);

        if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = resultingEntity;
        return resultingEntity;
    }

    public DataHubAgent CreateDataHubEntity<TDataHubEntity>(TDataHubEntity entity, string? stashTo = null, string? sourceEntityType = null)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingEntity = await createDataHubEntity(currentObject, stash, (_, _) => entity, stashTo, sourceEntityType);
            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataHubAgent CreateDataHubEntity<TDataHubEntity>(
        Func<object, Dictionary<string, object?>, TDataHubEntity> entityFunc,
        string? stashTo = null,
        string? sourceEntityType = null)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingEntity = await createDataHubEntity(currentObject, stash, entityFunc, stashTo, sourceEntityType);
            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataHubAgent CreateDataHubEntity<TDataHubEntity>(string fromStash, string? stashTo = null, string? sourceEntityType = null)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingEntity = await createDataHubEntity(
                currentObject,
                stash,
                (_, s) => s[fromStash].ToObject<TDataHubEntity>()!,
                stashTo,
                sourceEntityType);

            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(f);
        return this;
    }

    private async Task<TDataHubEntity> getDataHubEntity<TDataHubEntity>(string entityId)
        where TDataHubEntity : DataHubEntity
    {
        var response = await DataHubClient.PostRequestAsync<GetDataHubEntityRequest, GetDataHubEntityResponse>(
            new GetDataHubEntityRequest
            {
                EntityType = typeof(TDataHubEntity).Name,
                EntityId = entityId
            },
            CancellationToken.None);

        return response.Entity.ToObject<TDataHubEntity>()!;
    }

    public DataHubAgent GetDataHubEntity<TDataHubEntity>(string entityId, string? stashTo = null)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingEntity = await getDataHubEntity<TDataHubEntity>(entityId);
            if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = resultingEntity;
            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataHubAgent GetDataHubEntity<TDataHubEntity>(Func<object, Dictionary<string, object?>, string> entityIdFunc, string? stashTo = null)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingEntity = await getDataHubEntity<TDataHubEntity>(entityIdFunc(currentObject, stash));
            if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = resultingEntity;
            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataHubAgent GetDataHubEntityFromStash<TDataHubEntity>(string fromStash, string? stashTo = null)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var sourceEntity = stash[fromStash].ToObject<TDataHubEntity>()!;
            var resultingEntity = await getDataHubEntity<TDataHubEntity>(sourceEntity.id);
            if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = resultingEntity;
            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataHubAgent PatchDataHubEntity<TDataHubEntity>(
        string fromStash,
        Func<TDataHubEntity, List<Patch>> patchFunc,
        string? stashTo = null)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var entity = stash[fromStash].ToObject<TDataHubEntity>()!;
            var operations = patchFunc(entity);
            var patchResponse = await DataHubClient.PostRequestAsync<PatchEntitiesRequest, PatchEntitiesResponse>(
                new PatchEntitiesRequest
                {
                    DispatchNotifications = false,
                    Requests =
                    [
                        new PatchEntityRequest
                        {
                            DataSource = DataSources.DataHub,
                            EntityType = entity.entityType,
                            EntityId = entity.id,
                            Operations = operations
                        }
                    ]
                },
                CancellationToken.None);

            if (!patchResponse.Success)
            {
                var failures = patchResponse.Results
                    .Where(result => !result.Success)
                    .Select(result => result.FailureReason)
                    .Where(reason => !string.IsNullOrWhiteSpace(reason));

                var message = string.Join(Environment.NewLine, failures);
                if (string.IsNullOrWhiteSpace(message))
                {
                    message = $"DataHub patch failed. Response: {JsonConvert.SerializeObject(patchResponse)}. Operations: {JsonConvert.SerializeObject(operations)}";
                }

                throw new InvalidOperationException(message);
            }

            var resultingEntity = await getDataHubEntity<TDataHubEntity>(entity.id);
            if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = resultingEntity;
            return new ScenarioActionResult { CurrentObject = resultingEntity, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(f);
        return this;
    }

    private async Task deleteDataHubEntity<TDataHubEntity>(string entityId)
        where TDataHubEntity : DataHubEntity
    {
        await DataHubClient.PostRequestAsync<DeleteDataHubEntitiesRequest, DeleteDataHubEntitiesResponse>(
            new DeleteDataHubEntitiesRequest
            {
                EntityType = typeof(TDataHubEntity).Name,
                EntityIds = [entityId],
                IncludeTrackingEntries = true
            },
            CancellationToken.None);
    }

    public DataHubAgent DeleteDataHubEntity<TDataHubEntity>(string entityId)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            await deleteDataHubEntity<TDataHubEntity>(entityId);
            return new ScenarioActionResult { CurrentObject = null, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataHubAgent DeleteDataHubEntity<TDataHubEntity>(Func<object, Dictionary<string, object?>, string?> entityIdFunc)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var entityId = entityIdFunc(currentObject, stash);
            if (!string.IsNullOrEmpty(entityId))
            {
                try
                {
                    await deleteDataHubEntity<TDataHubEntity>(entityId);
                }
                catch
                {
                    // Cleanup is best-effort and limited to test-created records.
                }
            }

            return new ScenarioActionResult { CurrentObject = null, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataHubAgent DeleteDataHubEntityFromStash<TDataHubEntity>(string fromStash)
        where TDataHubEntity : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            if (stash.TryGetValue(fromStash, out var value))
            {
                var entityId = value switch
                {
                    string id => id,
                    DataHubEntity entity => entity.id,
                    _ => value?.ToObject<TDataHubEntity>()?.id
                };

                if (!string.IsNullOrEmpty(entityId))
                {
                    try
                    {
                        await deleteDataHubEntity<TDataHubEntity>(entityId);
                    }
                    catch
                    {
                        // Cleanup is best-effort and limited to test-created records.
                    }
                }
            }

            return new ScenarioActionResult { CurrentObject = null, Outputs = stash };
        }

        ScenarioBuilder.Enqueue(f);
        return this;
    }
}
