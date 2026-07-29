using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Helpers;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncDependencyDataHubEntities;
using Reimaginate.DataHub.Agent.Dataverse.Services.DataHubEntityCache;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.EnsureReferencedEntitiesAreSyncd;

public class EnsureReferencedEntitiesAreSyncdRequestHandler<TDataHubEntity, TDataverseEntity>(IOptions<DataverseAgentOptions> dataverseAgentConfig, IDataHubEntityCache dataHubEntityCache, IMediator mediator)
    : IHandler<EnsureReferencedEntitiesAreSyncdRequest<TDataHubEntity, TDataverseEntity>, EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TDataverseEntity>>
    where TDataHubEntity : DataHubEntity, new()
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity, new()
{
    public async Task<EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TDataverseEntity>> HandleAsync(EnsureReferencedEntitiesAreSyncdRequest<TDataHubEntity, TDataverseEntity> request, CancellationToken cancellationToken)
    {
        var resolvedEntityReferences = new Dictionary<string, JObject>();
        var resolutionPromises = new List<ResolutionPromise>();
        var failures = new List<ReferenceEntitySyncFailure>();

        var entityReferenceProps = GetAllEntityReferenceProps(typeof(TDataHubEntity)).ToList();
        var entityReferences = request.Entities.SelectMany(entity => GetEntityReferencesFromPaths(entity, entityReferenceProps).Select(s => s.Value)).ToList();

        var externalEntityReferences = entityReferences.Where(a => a._tag == nameof(ExternalEntityReference)).Select(s => JObject.FromObject(s).ToObject<ExternalEntityReference>()).ToList();
        var entityReferencesToResolve = entityReferences.Except(externalEntityReferences).ToList();

        var entityReferenceTypes = entityReferencesToResolve.GroupBy(g => g.EntityType);
        foreach (var entityReferenceTypeGroup in entityReferenceTypes)
        {
            if (string.IsNullOrEmpty(entityReferenceTypeGroup.Key)) continue;

            #region Retrieve referenced entities from the Data Hub

            var entityIds = entityReferenceTypeGroup.Select(entityReference => entityReference.EntityId).Distinct().ToList();
            var foundEntities = await dataHubEntityCache.GetDataHubEntities(entityReferenceTypeGroup.Key, entityIds, cancellationToken);

            foreach (var foundEntity in foundEntities)
            {
                var entityId = foundEntity.Value<string>(nameof(DataHubEntity.id));
                var entityRefsInEntity = entityReferencesToResolve.Where(w => w.EntityType == entityReferenceTypeGroup.Key && w.EntityId == entityId).ToList();
                entityRefsInEntity.ForEach(er =>
                {
                    var key = $"{er.EntityType}.{er.EntityId}".ToLower();
                    resolvedEntityReferences.TryAdd(key, foundEntity);
                });
            }

            #endregion
        }

        #region Find referenced entities that do not yet exist in Dataverse

        var unsyncedReferencedEntities = resolvedEntityReferences
            .Where(w => !w.Value.TryGetSourceSystemAlternateKeys("dataverse").Any())
            .Distinct()
            .ToList();

        #endregion

        if (unsyncedReferencedEntities.Any())
        {
            var unsyncedRefsByType = unsyncedReferencedEntities.GroupBy(g => g.Value.Value<string>(nameof(DataHubEntity.entityType)));
            foreach (var typeGroup in unsyncedRefsByType)
            {
                #region Find the DataHub and Dataverse Entity Types from the type group and custom atrributes attached to the entity definitions

                var entityRefDataHubType = typeof(TDataHubEntity).Assembly.GetType($"{typeof(TDataHubEntity).Namespace}.{typeGroup.Key}", true, true);

                var entityRefDataverseTypeName = entityRefDataHubType!
                    .GetCustomAttributes(typeof(RelatedEntityTypeAttribute), true)
                    .Select(s => (RelatedEntityTypeAttribute)s)
                    .FirstOrDefault(f => f.DataSource == dataverseAgentConfig.Value.DataSource)?.TypeName;

                var entityRefDataverseType = Type.GetType(entityRefDataverseTypeName!);

                #endregion

                #region Check if referenced entities would create a circular reference if syncd

                var dataHubEntityIds = typeGroup.Select(s => s.Value.Value<string>(nameof(DataHubEntity.id))).ToList();
                var circularDependencies = request.DependencyTree?.Select(s => s.EntityId).ToList().Intersect(dataHubEntityIds).ToList();

                #endregion

                if (circularDependencies?.Any() ?? false)
                {
                    foreach (var circularDependency in circularDependencies)
                    {
                        var props = resolvedEntityReferences.Where(w => w.Value.Value<string>(nameof(DataHubEntity.id)) == circularDependency).ToDictionary(k => k.Key, v => v.Value);
                        foreach (var entity in request.Entities)
                        {
                            foreach (var path in props)
                            {
                                var prop = GetPropertyByPath(entity, path.Key);
                                prop.SetValue(entity, null);
                                resolutionPromises.Add(new ResolutionPromise()
                                {
                                    DataHubEntityId = entity.id,
                                    DataHubEntityType = entity.entityType,
                                    ExternalEntityReference = new ExternalEntityReference()
                                    {
                                        DataSource = "DataHub",
                                        EntityType = path.Value.Value<string>(nameof(DataHubEntity.entityType)),
                                        EntityId = path.Value.Value<string>(nameof(DataHubEntity.id))
                                    },
                                    EntityReferencePath = path.Key
                                });
                            }
                        }
                    }

                    continue;
                }

                #region Dispatch a sync request for the referenced entities

                var syncRequestBaseType = typeof(SyncDependencyDataHubEntitiesRequest<,>);
                var syncRequestType = syncRequestBaseType.MakeGenericType(entityRefDataHubType, entityRefDataverseType!);

                dynamic syncEntityRefsRequest = Activator.CreateInstance(syncRequestType, dataHubEntityIds, request.DependencyTree, request.ResolutionPromises);
                _ = (await mediator.SendAsync((IRequest)syncEntityRefsRequest, cancellationToken)) switch { { IsT1: true } result => throw result.AsT1, { AsT0: var mediatorResultValue } => mediatorResultValue };
                resolutionPromises = syncEntityRefsRequest!.ResolutionPromises;

                #endregion

                #region Reload the sync'd entities to the cache to ensure cache has latest details

                dataHubEntityCache.InvalidateCacheEntries(entityRefDataHubType, dataHubEntityIds);
                var reloadedEntities = await dataHubEntityCache.GetDataHubEntities(typeGroup.Key, dataHubEntityIds, cancellationToken);

                reloadedEntities.ForEach(e =>
                {
                    var key = $"{e.Value<string>(nameof(DataHubEntity.entityType))}.{e.Value<string>(nameof(DataHubEntity.id))}".ToLower();
                    resolvedEntityReferences[key] = e;
                });

                #endregion
            }
        }

        return new EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TDataverseEntity>()
        {
            CachedEntities = resolvedEntityReferences.Select(s => s.Value).ToList(),
            ResolutionPromises = resolutionPromises,
            Failures = failures
        };
    }

    private IEnumerable<string> GetAllEntityReferenceProps(Type type, string path = "")
    {
        var entityReferenceProps = new List<string>();

        if (type == null)
            return entityReferenceProps;

        foreach (var prop in type.GetProperties())
        {
            if (prop.PropertyType == typeof(EntityReference))
            {
                entityReferenceProps.Add(string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}");
            }
            else if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
            {
                var subPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";

                if (prop.PropertyType != typeof(List<EntityReference>) && prop.PropertyType != typeof(JArray) && prop.PropertyType != typeof(JObject))
                    entityReferenceProps.AddRange(GetAllEntityReferenceProps(prop.PropertyType, subPath));
            }
        }

        return entityReferenceProps;
    }

    private Dictionary<string, EntityReference> GetEntityReferencesFromPaths(object rootEntity, List<string> paths)
    {
        var entityReferences = new Dictionary<string, EntityReference>();

        foreach (var path in paths)
        {
            var propNames = path.Split('.');
            var propValue = rootEntity;

            foreach (var propName in propNames)
            {
                if (propValue == null)
                    break;

                var propInfo = propValue.GetType().GetProperty(propName);

                if (propInfo == null)
                    break;

                propValue = propInfo.GetValue(propValue);

                if (typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType))
                    break;
            }

            if (propValue is EntityReference entityReference)
            {
                entityReferences.Add(path, entityReference);
            }

            if (propValue is List<EntityReference> entityReferenceList)
            {
                foreach (var entityReferenceItem in entityReferenceList)
                {
                    var indexOfItem = entityReferenceList.IndexOf(entityReferenceItem);
                    entityReferences.Add($"{path}[{indexOfItem}]", entityReferenceItem);
                }
            }
        }

        return entityReferences;
    }

    private PropertyInfo GetPropertyByPath(object rootEntity, string path)
    {
        var propNames = path.Split('.');
        var propValue = rootEntity;
        PropertyInfo propInfo = null;

        foreach (var propName in propNames)
        {
            if (propValue == null)
                throw new NullReferenceException($"Null value encountered at path: {path}");

            propInfo = propValue.GetType().GetProperty(propName);

            if (propInfo == null)
                throw new ArgumentException($"Property '{propName}' not found on type '{propValue.GetType().Name}'");

            if (propName != propNames.Last())
            {
                propValue = propInfo.GetValue(propValue);
            }
        }

        return propInfo;
    }
}

public class ReferenceEntitySyncFailure
{
    public DataHubEntity Entity { get; set; }
    public Exception Exception { get; set; }
}