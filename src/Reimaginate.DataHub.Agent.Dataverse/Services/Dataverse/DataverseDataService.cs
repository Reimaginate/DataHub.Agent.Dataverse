using System.Collections.Concurrent;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Reimaginate.DataServices.Responses;
using System.ServiceModel;
using Microsoft.PowerPlatform.Dataverse.Client;
using OneOf.Types;

namespace Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;

public interface IDataverseDataService
{
    Task<Guid> CreateAsync(Entity entity, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync<TDataverseEntity>(TDataverseEntity entity, CancellationToken cancellationToken = default) where TDataverseEntity : Entity;
    Task<Dictionary<string, CreateRecordResponse>> CreateAsync<TDataverseEntity>(Dictionary<string, TDataverseEntity> records, CancellationToken cancellationToken = default) where TDataverseEntity : Entity;
    Task DeleteAsync(EntityReference entityRef, CancellationToken cancellationToken = default);
    Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(TRequestType request, CancellationToken cancellationToken = default) where TRequestType : OrganizationRequest where TResponseType : OrganizationResponse;
    Task<List<OrganizationServiceFault>> ExecuteInTransactionAsync<TRequestType, TDataverseEntity>(List<TDataverseEntity> entities, bool returnResponses, CancellationToken cancellationToken = default) where TRequestType : OrganizationRequest where TDataverseEntity : Entity;
    Task<ExecuteMultipleResponse> ExecuteMultipleAsync(ExecuteMultipleRequest request, CancellationToken cancellationToken = default);
    Task<ExecuteMultipleResponse> ExecuteMultipleAsync(OrganizationRequestCollection requests, CancellationToken cancellationToken = default);
    Task<Dictionary<string, OperationResult>> ExecuteInParallelAsync<TRequestType, TResponseType, TDataverseEntity>(Dictionary<string, TDataverseEntity> entities, CancellationToken cancellationToken = default, bool disableRowVersionCheck = false) where TRequestType : OrganizationRequest where TResponseType : OrganizationResponse, new() where TDataverseEntity : Entity;
    Task<List<TDataverseEntity>> ExecuteQueryAsync<TDataverseEntity>(QueryExpression query, CancellationToken cancellationToken = default) where TDataverseEntity : Entity;
    Task<Entity> GetAsync(string entityLogicalName, Guid id, ColumnSet columns = null, CancellationToken cancellationToken = default);
    Task<TDataverseEntity> GetAsync<TDataverseEntity>(Guid id, ColumnSet columns = null, CancellationToken cancellationToken = default) where TDataverseEntity : Entity;
    Task<List<Entity>> GetAsync(string entityLogicalName, List<Guid> ids, ColumnSet columns = null, CancellationToken cancellationToken = default);
    Task<GetAsyncResponse<TDataverseEntity>> GetAsync<TDataverseEntity>(List<Guid> ids, ColumnSet columns = null, bool throwOnNotFound = true, CancellationToken cancellationToken = default) where TDataverseEntity : Entity;
    Task<List<OneToManyRelationshipMetadata>> GetOneToManyRelationshipsAsync(string referencedEntity, string referencingEntity, CancellationToken cancellationToken = default);
    Task<Dictionary<string, List<TDataverseEntity>>> GetRelatedEntities<TDataverseEntity>(Type entityType, string referencedEntity, Guid referencedEntityId, CancellationToken cancellationToken = default) where TDataverseEntity : Entity;
    Task<PagedResults<TDataverseEntity>> PagedWhereAsync<TDataverseEntity>(FilterExpression filterExpression, int page = 1, int pageSize = 500, ColumnSet columns = null, Dictionary<string, OrderType> orders = null, string continuationToken = null, CancellationToken cancellationToken = default) where TDataverseEntity : Entity;
    Task<BusinessEntityChanges> RetrieveEntityChanges<TDataverseEntity>(int page = 1, int pageSize = 500, string pagingCookie = null, string token = null, ColumnSet columns = null, CancellationToken cancellationToken = default) where TDataverseEntity : Entity;
    Task<Dictionary<string, UpdateRecordResponse>> UpdateAsync<TDataverseEntity>(Dictionary<string, TDataverseEntity> records, CancellationToken cancellationToken = default, bool disableRowVersionCheck = false) where TDataverseEntity : Entity;
    Task<UpdateResponse> UpdateAsync(Entity entity, CancellationToken cancellationToken = default);
    Task<UpdateResponse> UpdateAsync<TDataverseEntity>(TDataverseEntity entity, CancellationToken cancellationToken = default) where TDataverseEntity : Entity;
    Task<TDataverseEntity> UpsertAsync<TDataverseEntity>(TDataverseEntity entity, CancellationToken cancellationToken = default) where TDataverseEntity : Entity;
    Task<List<TDataverseEntity>> WhereAsync<TDataverseEntity>(FilterExpression filterExpression = null, ColumnSet columns = null, CancellationToken cancellationToken = default) where TDataverseEntity : Entity;
    Task<List<Entity>> WhereAsync(string entityLogicalName, FilterExpression filterExpression = null, ColumnSet columns = null, CancellationToken cancellationToken = default);
}

public class DataverseDataService(ServiceClient serviceClient) : IDataverseDataService
{
    public async Task<Guid> CreateAsync(Entity entity, CancellationToken cancellationToken = default)
    {
        var req = new CreateRequest()
        {
            Target = entity
        };

        var response = (CreateResponse)await serviceClient.ExecuteAsync(req, cancellationToken);

        #region Deactivate created entity if needed

        if (entity.Attributes.ContainsKey("statecode") && ((OptionSetValue)entity["statecode"]).Value == 1)
        {
            var updateRequest = new UpdateRequest()
            {
                Target = new Entity(entity.LogicalName, response.id)
                {
                    Attributes = new AttributeCollection() { { "statecode", new OptionSetValue(1) } }
                }
            };

            await serviceClient.ExecuteAsync(updateRequest, cancellationToken);
        }

        #endregion

        return response.id;
    }

    public async Task<Guid> CreateAsync<TDataverseEntity>(TDataverseEntity entity, CancellationToken cancellationToken = default) where TDataverseEntity : Entity
        => await CreateAsync((Entity)entity, cancellationToken);


    public async Task<Dictionary<string, CreateRecordResponse>> CreateAsync<TDataverseEntity>(Dictionary<string, TDataverseEntity> records, CancellationToken cancellationToken = default) where TDataverseEntity : Entity
    {
        var executeMultipleResponse = await ExecuteInParallelAsync<CreateRequest, CreateResponse, TDataverseEntity>(records, cancellationToken);

        var inactiveEntities = records.Where(entity => entity.Value.Attributes.ContainsKey("statecode") && ((OptionSetValue)entity.Value["statecode"]).Value == 1).ToDictionary(k => k.Key, v => v.Value);
        var recordsToDeactivate = executeMultipleResponse.Where(w => w.Value.Success && inactiveEntities.ContainsKey(w.Key)).ToList();
        if (recordsToDeactivate.Any())
        {
            var updateRequests = new OrganizationRequestCollection();
            updateRequests.AddRange(recordsToDeactivate.Select(entity => new UpdateRequest()
            {
                Target = new Entity(entity.Value.SourceRecord.LogicalName, entity.Value.ResultingEntityId!.Value)
                {
                    Attributes = new AttributeCollection() { { "statecode", new OptionSetValue(1) } }
                }
            }).ToList());

            var deactivateRequest = new ExecuteMultipleRequest()
            {
                Requests = updateRequests,
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                }
            };

            await serviceClient.ExecuteAsync(deactivateRequest, cancellationToken);
        }

        var ret = executeMultipleResponse.ToDictionary(k => k.Key, v => new CreateRecordResponse()
        {
            EntityId = v.Value.ResultingEntityId,
            Success = v.Value.Success,
            Error = !v.Value.Success ? v.Value.Exception?.Message : null
        });

        return ret;
    }

    public async Task DeleteAsync(EntityReference entityRef, CancellationToken cancellationToken = default)
    {
        try
        {
            var req = new DeleteRequest()
            {
                Target = entityRef,
            };

            var response = (DeleteResponse)await serviceClient.ExecuteAsync(req, cancellationToken);
        }
        catch (Exception)
        {
            //Don't do anything
        }
    }

    public async Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(TRequestType request, CancellationToken cancellationToken = default) where TRequestType : OrganizationRequest where TResponseType : OrganizationResponse
    {
        return (TResponseType)await serviceClient.ExecuteAsync(request, cancellationToken);
    }

    public async Task<List<OrganizationServiceFault>> ExecuteInTransactionAsync<TRequestType, TDataverseEntity>(List<TDataverseEntity> entities, bool returnResponses, CancellationToken cancellationToken = default) where TRequestType : OrganizationRequest where TDataverseEntity : Entity
    {
        var faults = new List<OrganizationServiceFault>();

        var request = new ExecuteTransactionRequest
        {
            Requests = new OrganizationRequestCollection(),
            ReturnResponses = returnResponses
        };

        foreach (var entity in entities)
        {
            if (typeof(TRequestType) == typeof(CreateRequest))
                request.Requests.Add(new CreateRequest { Target = entity });
            else if (typeof(TRequestType) == typeof(UpdateRequest))
            {
                entity.EntityState = EntityState.Changed;
                request.Requests.Add(new UpdateRequest { Target = entity });
            }
            else if (typeof(TRequestType) == typeof(DeleteRequest))
                request.Requests.Add(new DeleteRequest { Target = entity.ToEntityReference() });
            else if (typeof(TRequestType) == typeof(UpsertRequest))
                request.Requests.Add(new UpsertRequest { Target = entity });
        }

        try
        {
            await serviceClient.ExecuteAsync(request, cancellationToken);
        }
        catch (FaultException<OrganizationServiceFault> ex)
        {
            faults.Add((ExecuteTransactionFault)(ex.Detail));
        }

        return faults;
    }

    public async Task<ExecuteMultipleResponse> ExecuteMultipleAsync(ExecuteMultipleRequest request, CancellationToken cancellationToken = default)
    {
        var response = (ExecuteMultipleResponse)await serviceClient.ExecuteAsync(request, cancellationToken);
        return response;
    }

    public async Task<ExecuteMultipleResponse> ExecuteMultipleAsync(OrganizationRequestCollection requests, CancellationToken cancellationToken = default)
    {
        var req = new ExecuteMultipleRequest()
        {
            Requests = requests,
            Settings = new ExecuteMultipleSettings()
            {
                ContinueOnError = true,
                ReturnResponses = true
            }
        };

        var response = (ExecuteMultipleResponse)await serviceClient.ExecuteAsync(req, cancellationToken);
        return response;
    }

    public async Task<Dictionary<string, OperationResult>> ExecuteInParallelAsync<TRequestType, TResponseType, TDataverseEntity>(Dictionary<string, TDataverseEntity> entities, CancellationToken cancellationToken = default, bool disableRowVersionCheck = false) where TRequestType : OrganizationRequest where TResponseType : OrganizationResponse, new() where TDataverseEntity : Entity
    {
        var results = new ConcurrentDictionary<string, OperationResult>(entities.ToDictionary(k => k.Key, v => new OperationResult()
        {
            SourceRecord = v.Value
        }));

        await Parallel.ForEachAsync(entities, new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = serviceClient.RecommendedDegreesOfParallelism
        }, async (entity, ct) =>
        {
            var result = results[entity.Key];

            if (entity.Value.Attributes.ContainsKey("ownerid") && entity.Value.Attributes["ownerid"] == null)
            {
                entity.Value.Attributes.Remove("ownerid");
            }

            try
            {
                OrganizationRequest req = null;

                if (typeof(TRequestType) == typeof(CreateRequest))
                {
                    req = new CreateRequest { Target = entity.Value };
                }
                else if (typeof(TRequestType) == typeof(UpdateRequest))
                {
                    entity.Value.EntityState = EntityState.Changed;
                    req = new UpdateRequest { Target = entity.Value, ConcurrencyBehavior = disableRowVersionCheck ? ConcurrencyBehavior.Default : ConcurrencyBehavior.IfRowVersionMatches };
                    result.ResultingEntityId = entity.Value.Id;
                }
                else if (typeof(TRequestType) == typeof(DeleteRequest))
                {
                    req = new DeleteRequest { Target = entity.Value.ToEntityReference() };
                    result.ResultingEntityId = entity.Value.Id;
                }
                else if (typeof(TRequestType) == typeof(UpsertRequest))
                {
                    req = new UpsertRequest { Target = entity.Value };
                    result.ResultingEntityId = entity.Value.Id;
                }

                var response = (TResponseType)await serviceClient.ExecuteAsync(req, ct);
                result.Success = true;

                if (response.Results.ContainsKey("id"))
                    result.ResultingEntityId = (Guid)response.Results["id"];
            }
            catch (Exception ex)
            {

                result.Success = false;
                result.Exception = ex;
            }
        });

        return results.ToDictionary(k => k.Key, v => v.Value);
    }

    public async Task<List<TDataverseEntity>> ExecuteQueryAsync<TDataverseEntity>(QueryExpression query, CancellationToken cancellationToken = default) where TDataverseEntity : Entity
    {
        var req = new RetrieveMultipleRequest()
        {
            Query = query
        };

        var response = (RetrieveMultipleResponse)await serviceClient.ExecuteAsync(req, cancellationToken);
        var ret = response.EntityCollection.Entities.Select(s => s.ToEntity<TDataverseEntity>()).ToList();
        return ret;
    }


    public async Task<Entity> GetAsync(string entityLogicalName, Guid id, ColumnSet columns = null, CancellationToken cancellationToken = default)
    {
        columns ??= new ColumnSet(true);

        var req = new RetrieveRequest()
        {
            ColumnSet = columns,
            Target = new EntityReference(entityLogicalName, id),
        };
        var response = (RetrieveResponse)await serviceClient.ExecuteAsync(req, cancellationToken);
        var result = response.Entity;
        return result;
    }

    public async Task<TDataverseEntity> GetAsync<TDataverseEntity>(Guid id, ColumnSet columns = null, CancellationToken cancellationToken = default) where TDataverseEntity : Entity
    {
        var entityLogicalName = typeof(TDataverseEntity).GetField("EntityLogicalName")?.GetValue(typeof(TDataverseEntity))?.ToString();
        if (entityLogicalName == null) throw new Exception("EntityLogicalName not found");

        if (columns == null)
        {
            var primaryIdAttribute = typeof(TDataverseEntity).GetField("PrimaryIdAttribute")?.GetValue(typeof(TDataverseEntity))?.ToString();
            var primaryNameAttribute = typeof(TDataverseEntity).GetField("PrimaryNameAttribute")?.GetValue(typeof(TDataverseEntity))?.ToString();

            columns = new ColumnSet(new string[]
            {
                primaryIdAttribute,
                primaryNameAttribute
            });
        }

        var req = new RetrieveRequest()
        {
            ColumnSet = columns,
            Target = new EntityReference(entityLogicalName, id),
        };
        var response = (RetrieveResponse)await serviceClient.ExecuteAsync(req, cancellationToken);
        var result = response.Entity.ToEntity<TDataverseEntity>();
        return result;
    }

    public async Task<List<Entity>> GetAsync(string entityLogicalName, List<Guid> ids, ColumnSet columns = null, CancellationToken cancellationToken = default)
    {
        if (ids != null && !ids.Any()) return new List<Entity>();
        columns ??= new ColumnSet(true);

        var req = new RetrieveMultipleRequest()
        {
            Query = new QueryExpression(entityLogicalName)
            {
                ColumnSet = columns,
                Criteria =
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression($"{entityLogicalName}id", ConditionOperator.In, ids),
                    }
                }
            }
        };

        var response = (RetrieveMultipleResponse)await serviceClient.ExecuteAsync(req, cancellationToken);
        var ret = response.EntityCollection.Entities.ToList();
        return ret;
    }

    public async Task<GetAsyncResponse<TDataverseEntity>> GetAsync<TDataverseEntity>(List<Guid> ids, ColumnSet columns = null, bool throwOnNotFound = true, CancellationToken cancellationToken = default) where TDataverseEntity : Entity
    {
        var entityLogicalName = typeof(TDataverseEntity).GetField("EntityLogicalName")?.GetValue(typeof(TDataverseEntity))?.ToString();
        if (entityLogicalName == null) throw new Exception("EntityLogicalName not found");

        if (ids != null && !ids.Any())
        {
            return new GetAsyncResponse<TDataverseEntity>()
            {
                Results = []
            };
        }

        var primaryIdAttribute = typeof(TDataverseEntity).GetField("PrimaryIdAttribute")?.GetValue(typeof(TDataverseEntity))?.ToString();
        var primaryNameAttribute = typeof(TDataverseEntity).GetField("PrimaryNameAttribute")?.GetValue(typeof(TDataverseEntity))?.ToString();

        columns ??= new ColumnSet([
            primaryIdAttribute,
            primaryNameAttribute
        ]);

        var tasks = ids!.Select(id => Task.Run(() => serviceClient.RetrieveAsync(entityLogicalName, id, columns, cancellationToken), cancellationToken)).ToList();
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            //ignore
        }

        if (tasks.Any(a => a.IsFaulted))
        {
            if (throwOnNotFound)
            {
                throw new Exception("ERR: " + string.Join(" : ", tasks.Where(w => w.IsFaulted).Select(s => s.Exception?.Message)));
            }

            return new GetAsyncResponse<TDataverseEntity>()
            {
                Results = tasks.Where(w=>!w.IsFaulted).Select(t => t.Result.ToEntity<TDataverseEntity>()).ToList(),
                NotFound = tasks.Where(w=>w.IsFaulted).Select(s=> ids[tasks.IndexOf(s)]).ToList()
            };
        }

        var entities = tasks.Select(t => t.Result.ToEntity<TDataverseEntity>()).ToList();
        return new GetAsyncResponse<TDataverseEntity>()
        {
            Results = entities
        };
    }


    public async Task<List<OneToManyRelationshipMetadata>> GetOneToManyRelationshipsAsync(string referencedEntity, string referencingEntity, CancellationToken cancellationToken = default)
    {
        var entityReq = new RetrieveEntityRequest
        {
            EntityFilters = EntityFilters.Relationships,
            LogicalName = referencedEntity,
        };

        var entityResponse = (RetrieveEntityResponse)await serviceClient.ExecuteAsync(entityReq, cancellationToken);
        var relationships = entityResponse.EntityMetadata.OneToManyRelationships.ToList();

        return relationships.Where(r => r.ReferencingEntity == referencingEntity).ToList();
    }

    public async Task<Dictionary<string, List<TDataverseEntity>>> GetRelatedEntities<TDataverseEntity>(Type entityType, string referencedEntity, Guid referencedEntityId, CancellationToken cancellationToken = default) where TDataverseEntity : Entity
    {
        var entityLogicalName = entityType.GetField("EntityLogicalName")?.GetValue(entityType)?.ToString();
        if (entityLogicalName == null) throw new Exception("EntityLogicalName not found.");

        var ret = new Dictionary<string, List<TDataverseEntity>>();

        var oneToManyRelationships = await GetOneToManyRelationshipsAsync(referencedEntity, entityLogicalName, cancellationToken);

        foreach (var r in oneToManyRelationships)
        {
            var query = QueryExpressionHelper.GetQueryExpression(entityLogicalName, new ColumnSet(oneToManyRelationships.Select(c => c.ReferencingAttribute).ToArray()), new ConditionExpression(r.ReferencingAttribute, ConditionOperator.Equal, referencedEntityId));
            if (entityLogicalName == "activitypointer")
            {
                query.ColumnSet.AddColumn("activitytypecode");
                query.ColumnSet.AddColumn("allparties");
            }

            var results = await ExecuteQueryAsync<TDataverseEntity>(query, cancellationToken);
            ret.Add(r.ReferencingAttribute, results);
        }

        //TODO: Many to Many Relationships
        if (entityLogicalName == "activitypointer")
        {
            var query = new QueryExpression(entityLogicalName)
            {
                ColumnSet = new ColumnSet(true),
            };

            var activityParty = query.AddLink("activityparty", "activityid", "activityid");
            activityParty.LinkCriteria.AddCondition("partyid", ConditionOperator.Equal, referencedEntityId);

            var results = await ExecuteQueryAsync<TDataverseEntity>(query, cancellationToken);
            ret.Add("allparties", results);
        }

        return ret;
    }




    public async Task<PagedResults<TDataverseEntity>> PagedWhereAsync<TDataverseEntity>(FilterExpression filterExpression, int page = 1, int pageSize = 500, ColumnSet columns = null, Dictionary<string, OrderType> orders = null, string continuationToken = null, CancellationToken cancellationToken = default) where TDataverseEntity : Entity
    {

        var entityLogicalName = typeof(TDataverseEntity).GetField("EntityLogicalName")?.GetValue(typeof(TDataverseEntity))?.ToString();
        if (entityLogicalName == null) throw new Exception("EntityLogicalName not found");

        columns ??= new ColumnSet(true);

        var req = new RetrieveMultipleRequest()
        {
            Query = new QueryExpression(entityLogicalName)
            {
                ColumnSet = columns,
                PageInfo = new PagingInfo()
                {
                    Count = pageSize,
                    PageNumber = page,
                    ReturnTotalRecordCount = false,
                    PagingCookie = continuationToken
                },
                Criteria = filterExpression,
                NoLock = true
            }
        };

        if (orders == null || orders.Count == 0)
        {
            var primaryIdAttribute = typeof(TDataverseEntity).GetField("PrimaryIdAttribute")?.GetValue(typeof(TDataverseEntity))?.ToString();
            ((QueryExpression)req.Query).AddOrder(primaryIdAttribute, OrderType.Ascending);
        }
        else
        {
            foreach (var order in orders)
            {
                ((QueryExpression)req.Query).AddOrder(order.Key, order.Value);
            }
        }

        var response = (RetrieveMultipleResponse)await serviceClient.ExecuteAsync(req, cancellationToken);
        var results = response.EntityCollection.Entities.Select(s => s.ToEntity<TDataverseEntity>()).ToList();

        var newPagedResults = new PagedResults<TDataverseEntity>()
        {
            Results = results,
            ContinuationToken = response.EntityCollection.PagingCookie,
            MoreResultsAvailable = response.EntityCollection.MoreRecords,
            ResultCount = response.EntityCollection.TotalRecordCount
        };
        return newPagedResults;
    }

    public async Task<BusinessEntityChanges> RetrieveEntityChanges<TDataverseEntity>(int page = 1, int pageSize = 500, string pagingCookie = null, string token = null, ColumnSet columns = null, CancellationToken cancellationToken = default) where TDataverseEntity : Entity
    {
        columns ??= new ColumnSet(true);

        var request = new RetrieveEntityChangesRequest
        {
            EntityName = typeof(TDataverseEntity).Name.ToLower(),
            Columns = columns,
            DataVersion = token ?? string.Empty,
            GetGlobalMetadataVersion = true,
            PageInfo = new PagingInfo()
            {
                Count = pageSize,
                PageNumber = page,
                ReturnTotalRecordCount = false,
                PagingCookie = pagingCookie
            }
        };
        var response = (RetrieveEntityChangesResponse)await serviceClient.ExecuteAsync(request, cancellationToken);
        return response.EntityChanges;
    }


    public async Task<Dictionary<string, UpdateRecordResponse>> UpdateAsync<TDataverseEntity>(Dictionary<string, TDataverseEntity> records, CancellationToken cancellationToken = default, bool disableRowVersionCheck = false) where TDataverseEntity : Entity
    {
        var executeMultipleResponse = await ExecuteInParallelAsync<UpdateRequest, UpdateResponse, TDataverseEntity>(records, cancellationToken, disableRowVersionCheck);

        var ret = executeMultipleResponse.ToDictionary(k => k.Key, v => new UpdateRecordResponse()
        {
            EntityId = v.Value.ResultingEntityId!.Value,
            Success = v.Value.Success,
            Error = !v.Value.Success ? v.Value.Exception?.Message : null
        });

        return ret;
    }

    public async Task<UpdateResponse> UpdateAsync(Entity entity, CancellationToken cancellationToken = default)
    {
        var req = new UpdateRequest()
        {
            Target = entity
        };

        return (UpdateResponse)await serviceClient.ExecuteAsync(req, cancellationToken);
    }

    public async Task<UpdateResponse> UpdateAsync<TDataverseEntity>(TDataverseEntity entity, CancellationToken cancellationToken = default) where TDataverseEntity : Entity
    {
        var req = new UpdateRequest()
        {
            Target = entity
        };

        return (UpdateResponse)await serviceClient.ExecuteAsync(req, cancellationToken);
    }


    public async Task<TDataverseEntity> UpsertAsync<TDataverseEntity>(TDataverseEntity entity, CancellationToken cancellationToken = default) where TDataverseEntity : Entity
    {
        if (entity.Id != Guid.Empty)
        {
            entity.KeyAttributes = new KeyAttributeCollection
            {
                {"Id", entity.Id}
            };
        }

        var req = new UpsertRequest()
        {
            Target = entity
        };

        var response = (UpsertResponse)await serviceClient.ExecuteAsync(req, cancellationToken);
        entity.Id = response.Target.Id;
        return entity;
    }


    public async Task<List<TDataverseEntity>> WhereAsync<TDataverseEntity>(FilterExpression filterExpression = null, ColumnSet columns = null, CancellationToken cancellationToken = default) where TDataverseEntity : Entity
    {
        var entityLogicalName = typeof(TDataverseEntity).GetField("EntityLogicalName")?.GetValue(typeof(TDataverseEntity))?.ToString();
        if (entityLogicalName == null) throw new Exception("EntityLogicalName not found");

        var req = new RetrieveMultipleRequest()
        {
            Query = new QueryExpression(entityLogicalName)
            {
                ColumnSet = columns,
                Criteria = filterExpression
            }
        };

        var response = (RetrieveMultipleResponse)await serviceClient.ExecuteAsync(req, cancellationToken);
        var ret = response.EntityCollection.Entities.Select(s => s.ToEntity<TDataverseEntity>()).ToList();

        return ret;
    }

    public async Task<List<Entity>> WhereAsync(string entityLogicalName, FilterExpression filterExpression = null, ColumnSet columns = null, CancellationToken cancellationToken = default)
    {
        var req = new RetrieveMultipleRequest()
        {
            Query = new QueryExpression(entityLogicalName)
            {
                ColumnSet = columns,
                Criteria = filterExpression
            }
        };

        var response = (RetrieveMultipleResponse)await serviceClient.ExecuteAsync(req, cancellationToken);
        var ret = response.EntityCollection.Entities.ToList();

        return ret;
    }
}

public class GetAsyncResponse<TDataverseEntity>
{
    public List<TDataverseEntity> Results { get; set; }
    public List<Guid> NotFound { get; set; }
}