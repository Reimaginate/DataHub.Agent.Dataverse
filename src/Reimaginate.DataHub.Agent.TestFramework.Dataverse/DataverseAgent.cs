using Azure.Core;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.AddMembersToMarketingList;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.CreateDataverseRecord;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.CreateDataverseRecords;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.DeleteDataverseRecord;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.RemoveMemberFromMarketingList;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecord;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetAllDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncSpecificDataHubEntities;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;
using Reimaginate.Test.Framework;
using Reimaginate.Test.Framework.Helpers;
using System.Threading;
using EntityReference = Microsoft.Xrm.Sdk.EntityReference;


// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.TestFramework.Dataverse;

public class DataverseAgent : TestAgentBase<DataverseAgent>
{
    public DataverseAgent()
    { }

    public DataverseAgent(IServiceProvider serviceProvider)
    {
        AgentServices = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        HostServices = serviceProvider;
        ActivitySource = DiagnosticConfig.DataverseAgent.ActivitySource;
    }

    public DataverseAgent(Func<ServiceCollection> serviceCollectionBuilder) : base(serviceCollectionBuilder, DiagnosticConfig.DataverseAgent.ActivitySource)
    { }

    #region SendUsingMediator

    public async Task<TResponse> SendUsingMediator<TResponse>(IRequest<TResponse> request)
    {
        var mediator = AgentServices.GetRequiredService<IMediator>();
        var response = (await mediator.TrySend<TResponse>(request, CancellationToken.None)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
        return response;
    }

    #endregion

    #region CreateRecord

    private async Task<TDataverse?> createRecord<TDataverse>(object currentObject, Dictionary<string, object?> stash, Func<object, Dictionary<string, object?>, TDataverse> recordFunc, string? stashTo = null) where TDataverse : Entity
    {
        try
        {
            var record = recordFunc(currentObject, stash);
            var createResponse = await SendUsingMediator(new CreateDataverseRecordCommand<TDataverse>()
            {
                DataverseEntity = record
            });

            var getResponse = await SendUsingMediator(new GetSpecificDataverseEntitiesRequest<TDataverse>()
            {
                EntityIds = [createResponse]
            });

            var resultingRecord = getResponse.Results.FirstOrDefault();
            if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = resultingRecord;
            return resultingRecord;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to create Dataverse record", ex);
        }
    }

    public DataverseAgent CreateRecord<TDataverse>(TDataverse record, string? stashTo = null) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await createRecord(currentObject, stash, (co, _) => record, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent CreateRecord<TDataverse>(TDataverse record, Func<TDataverse, TDataverse> modifierFunc, string? stashTo = null) where TDataverse : Entity
    {
        var modifiedRecord = modifierFunc(record);

        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await createRecord(currentObject, stash, (co, _) => modifiedRecord, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent CreateRecord<TDataverse>(Func<object, Dictionary<string, object?>, TDataverse> recordFunc, string? stashTo = null) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await createRecord(currentObject, stash, recordFunc, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    #endregion

    #region CreateRecords

    private async Task<List<TDataverse>> createRecords<TDataverse>(object currentObject, Dictionary<string, object?> stash, Func<object, Dictionary<string, object?>, List<TDataverse>> recordsFunc, string stashTo = null) where TDataverse : Entity
    {
        var records = recordsFunc(currentObject, stash);
        var createResponse = await SendUsingMediator(new CreateDataverseRecordsCommand<TDataverse>()
        {
            Records = records.ToDictionary(k => k.Id.ToString(), v => v)
        });

        var getResponse = await SendUsingMediator(new GetSpecificDataverseEntitiesRequest<TDataverse>()
        {
            EntityIds = createResponse.Results.Select(s => s.Value.ResultingEntity.Id).ToList()
        });

        if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = getResponse;
        return getResponse.Results;
    }

    public DataverseAgent CreateRecords<TDataverse>(List<TDataverse> records, string stashTo = null) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await createRecords(currentObject, stash, (co, _) => records, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent CreateRecords<TDataverse>(Func<object, Dictionary<string, object?>, List<TDataverse>> recordFunc, string stashTo = null) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await createRecords(currentObject, stash, recordFunc, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    #endregion

    #region DeleteRecord

    private async Task deleteRecord<TDataverse>(object currentObject, Dictionary<string, object?> stash, Func<object, Dictionary<string, object?>, TDataverse> recordFunc, string stashTo = null) where TDataverse : Entity
    {
        var record = recordFunc(currentObject, stash);
        try
        {
            await SendUsingMediator(new DeleteDataverseRecordCommand()
            {
                DataverseRecordType = record.LogicalName,
                DataverseRecordId = record.Id
            });
        }
        catch (Exception)
        {
            //ignore
        }

        if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = null;
    }

    public DataverseAgent DeleteRecord<TDataverse>(TDataverse? record) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            if (record != null) await deleteRecord(currentObject, stash, (_, _) => record);
            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent DeleteRecord<TDataverse>(string fromStash) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            if (stash.ContainsKey(fromStash))
            {
                var o = stash[fromStash]?.ToObject<TDataverse>();
                if (o != null) await deleteRecord(currentObject, stash, (_, _) => o);
            }
            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent DeleteRecord<TDataverse>(Guid id) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var entityLogicalName = typeof(TDataverse).GetField("EntityLogicalName")?.GetValue(typeof(TDataverse))?.ToString();

            await SendUsingMediator(new DeleteDataverseRecordCommand()
            {
                DataverseRecordType = entityLogicalName,
                DataverseRecordId = id
            });

            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    #endregion

    #region DeleteRecords

    private async Task<TDataverse> deleteRecords<TDataverse>(object currentObject, Dictionary<string, object?> stash, Func<object, Dictionary<string, object?>, List<TDataverse>> recordsFunc, string stashTo = null) where TDataverse : Entity
    {
        var records = recordsFunc(currentObject, stash);
        var groupedByLogicalName = records.GroupBy(g => g.LogicalName);

        foreach (var logicalNameGroup in groupedByLogicalName)
        {
            foreach (var record in records)
            {
                try
                {
                    await SendUsingMediator(new DeleteDataverseRecordCommand()
                    {
                        DataverseRecordType = logicalNameGroup.Key,
                        DataverseRecordId = record.Id
                    });
                }
                catch (Exception)
                {
                    //ignore
                }
            }
        }

        if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = null;
        return null;
    }

    public DataverseAgent DeleteRecords<TDataverse>(List<TDataverse> records) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            if (records != null) await deleteRecords(currentObject, stash, (_, _) => records);
            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent DeleteRecords<TDataverse>(string fromStash) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            if (stash.ContainsKey(fromStash))
            {
                var o = stash[fromStash].ToObject<List<TDataverse>>();
                if (o != null) await deleteRecords(currentObject, stash, (_, _) => o);
            }
            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent DeleteRecords<TDataverse>(List<Guid> ids) where TDataverse : Entity, new()
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var entityLogicalName = typeof(TDataverse).GetField("EntityLogicalName")?.GetValue(typeof(TDataverse))?.ToString();

            if (ids != null) await deleteRecords(currentObject, stash, (_, _) => ids.Select(id => new TDataverse()
            {
                LogicalName = entityLogicalName,
                Id = id

            }).ToList());
            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    #endregion

    #region AddContactToMarketingList

    public DataverseAgent AddContactToMarketingList(Guid marketingListId, Guid contactId, string stashTo = null)
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var response = await addContactsToMarketingList(marketingListId, [contactId]);

            if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = response;

            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent AddContactToMarketingList(Guid marketingListId, Func<object, Dictionary<string, object?>, Guid> contactIdFunc, string stashTo = null)
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var contactId = contactIdFunc(currentObject, stash);
            var response = await addContactsToMarketingList(marketingListId, [contactId]);

            if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = response;

            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }




    private async Task<AddListMembersListResponse> addContactsToMarketingList(Guid marketingListId, List<Guid> contactIds)
    {
        var response = await SendUsingMediator(new AddMembersToMarketingListCommand()
        {
            MarketingListId = marketingListId,
            MemberIds = contactIds
        });

        return response;
    }

    public DataverseAgent AddContactsToMarketingList(Guid marketingListId, List<Guid> contactIds)
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var response = await addContactsToMarketingList(marketingListId, contactIds);

            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }



    public DataverseAgent AddContactToMarketingLists(Func<object, Dictionary<string, object?>, Guid> contactIdFunc, List<Guid> marketingListIds, string stashTo = null)
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var contactId = contactIdFunc(currentObject, stash);

            var responses = new List<AddListMembersListResponse>();

            foreach (var marketingListId in marketingListIds)
            {
                var response = await addContactsToMarketingList(marketingListId, [contactId]);
                responses.Add(response);
            }

            if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = responses;

            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent AddContactToMarketingLists(Guid contactId, List<Guid> marketingListIds, string stashTo = null)
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var responses = new List<AddListMembersListResponse>();

            foreach (var marketingListId in marketingListIds)
            {
                var response = await addContactsToMarketingList(marketingListId, [contactId]);
                responses.Add(response);
            }

            if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = responses;

            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }


    public DataverseAgent AddContactsToMarketingList(Guid marketingListId, Func<object, Dictionary<string, object?>, List<Guid>> contactIdsFunc, string stashTo = null)
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var contactIds = contactIdsFunc(currentObject, stash);
            var response = await addContactsToMarketingList(marketingListId, contactIds);

            if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = response;

            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent AddContactsToMarketingList(Func<object, Dictionary<string, object?>, Guid> listIdFunc, Func<object, Dictionary<string, object?>, List<Guid>> contactIdsFunc, string stashTo = null)
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var listId = listIdFunc(currentObject, stash);
            var contactIds = contactIdsFunc(currentObject, stash);
            var response = await addContactsToMarketingList(listId, contactIds);

            if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = response;

            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    #endregion

    #region RemoveContactFromMarketingList

    private async Task removeContactFromMarketingList(Guid marketingListId, Guid contactId)
    {
        var response = await SendUsingMediator(new RemoveMemberFromMarketingListCommand()
        {
            MarketingListId = marketingListId,
            MemberId = contactId
        });
    }

    public DataverseAgent RemoveContactFromMarketingList(Func<object, Dictionary<string, object?>, Guid> marketingListFunc, Guid contactId)
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var marketingListId = marketingListFunc(currentObject, stash);
            await removeContactFromMarketingList(marketingListId, contactId);

            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent RemoveContactFromMarketingList(Guid marketingListId, Guid contactId)
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            await removeContactFromMarketingList(marketingListId, contactId);

            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    #endregion

    #region RemoveContactFromMarketingLists

    private async Task removeMemberFromMarketingLists(Guid contactId, List<Guid> marketingListIds)
    {
        foreach (var marketingListId in marketingListIds)
        {
            await SendUsingMediator(new RemoveMemberFromMarketingListCommand()
            {
                MarketingListId = marketingListId,
                MemberId = contactId
            });
        }
    }

    public DataverseAgent RemoveContactFromMarketingLists(Guid contactId, Func<object, Dictionary<string, object?>, List<Guid>> marketingListFunc)
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var marketingListIds = marketingListFunc(currentObject, stash);
            await removeMemberFromMarketingLists(contactId, marketingListIds);

            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent RemoveContactFromMarketingLists(Guid contactId, List<Guid> marketingListIds)
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            await removeMemberFromMarketingLists(contactId, marketingListIds);

            return new ScenarioActionResult() { CurrentObject = null, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    #endregion

    #region MergeDuplicates

    private async Task<MergeResponse> mergeDuplicates<TDataverse>(object currentObject, Dictionary<string, object?> stash, Func<object, Dictionary<string, object?>, Guid> fromEntityIdFunc, Func<object, Dictionary<string, object?>, Guid> toEntityIdFunc, string? stashTo = null) where TDataverse : Entity
    {
        var fromEntityId = fromEntityIdFunc(currentObject, stash);
        var toEntityId = toEntityIdFunc(currentObject, stash);

        var mergeRequest = new MergeRequest()
        {
            Target = new EntityReference(typeof(TDataverse).Name.ToLower(), toEntityId),
            SubordinateId = fromEntityId,
            PerformParentingChecks = false
        };

        var dataverseDataService = AgentServices.GetRequiredService<IDataverseDataService>();
        var dataverseMergeResponse = await dataverseDataService.ExecuteAsync<MergeRequest, MergeResponse>(mergeRequest, CancellationToken.None);

        return dataverseMergeResponse;
    }

    public DataverseAgent MergeDuplicates<TDataverse>(Guid fromDataverseId, Guid toDataverseId, string? stashTo = null) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await mergeDuplicates<TDataverse>(currentObject, stash, (co, _) => fromDataverseId, (co, _) => toDataverseId, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    #endregion

    #region MergeRecord

    private async Task<TDataHub> mergeRecord<TDataverse, TDataHub>(object currentObject, Dictionary<string, object?> stash, Func<object, Dictionary<string, object?>, Guid> entityIdFunc, string? stashTo = null, bool forceUpdate = false) where TDataverse : Entity where TDataHub : DataHubEntity
    {
        var entityId = entityIdFunc(currentObject, stash);
        var mergeResponse = await SendUsingMediator(new MergeSpecificDataverseEntitiesRequest<TDataverse, TDataHub>()
        {
            EntityIds = [entityId],
            ForceUpdate = forceUpdate
        });

        var mergeResult = mergeResponse.Results.SingleOrDefault(result => result.SourceEntityId == entityId.ToString()) ?? mergeResponse.Results.FirstOrDefault();

        var dataHubClient = AgentServices.GetRequiredService<IDataHubClient>();
        var getDataHubEntityResponse = await dataHubClient.PostRequestAsync<GetDataHubEntityRequest, GetDataHubEntityResponse>(new GetDataHubEntityRequest()
        {
            EntityType = mergeResult!.DataHubEntityType,
            EntityId = mergeResult!.DataHubEntityId
        }, CancellationToken.None);

        var resultingDataHubEntity = getDataHubEntityResponse.Entity.ToObject<TDataHub>();

        if (!string.IsNullOrEmpty(stashTo))
        {
            stash[stashTo] = resultingDataHubEntity;
            stash[$"{stashTo}_mergeResult"] = mergeResult;
            stash[$"{stashTo}_mergeResults"] = mergeResponse.Results;
        }
        return resultingDataHubEntity!;
    }

    public DataverseAgent MergeRecord<TDataverse, TDataHub>(TDataverse record, string stashTo = null, bool forceUpdate = false) where TDataverse : Entity where TDataHub : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await mergeRecord<TDataverse, TDataHub>(currentObject, stash, (co, _) => record.Id, stashTo, forceUpdate);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent MergeRecord<TDataverse, TDataHub>(string fromStash, string stashTo = null, bool forceUpdate = false) where TDataverse : Entity where TDataHub : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await mergeRecord<TDataverse, TDataHub>(currentObject, stash, (_, _) => stash[fromStash].ToObject<TDataverse>().Id, stashTo, forceUpdate);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent MergeRecord<TDataverse, TDataHub>(Guid dataverseId, string stashTo = null, bool forceUpdate = false) where TDataverse : Entity where TDataHub : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await mergeRecord<TDataverse, TDataHub>(currentObject, stash, (co, _) => dataverseId, stashTo, forceUpdate);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    private async Task<List<TDataHub>> mergeRecords<TDataverse, TDataHub>(object currentObject, Dictionary<string, object?> stash, Func<object, Dictionary<string, object?>, List<Guid>> entityIdsFunc, string stashTo = null, bool forceUpdate = false) where TDataverse : Entity where TDataHub : DataHubEntity
    {
        var entityIds = entityIdsFunc(currentObject, stash);
        var mergeResponse = await SendUsingMediator(new MergeSpecificDataverseEntitiesRequest<TDataverse, TDataHub>()
        {
            EntityIds = entityIds,
            ForceUpdate = forceUpdate
        });

        var mergeResult = mergeResponse.Results;

        var dataHubClient = AgentServices.GetRequiredService<IDataHubClient>();
        var getDataHubEntityResponse = await dataHubClient.PostRequestAsync<GetDataHubEntitiesByIdRequest, GetDataHubEntitiesByIdResponse>(new GetDataHubEntitiesByIdRequest()
        {
            EntityType = mergeResult!.FirstOrDefault().DataHubEntityType,
            EntityIds = mergeResult!.Select(s => s.DataHubEntityId).ToList()
        }, CancellationToken.None);

        var resultingDataHubEntities = getDataHubEntityResponse.Results.Select(s => s.ToObject<TDataHub>()).ToList();

        if (!string.IsNullOrEmpty(stashTo))
        {
            stash[stashTo] = resultingDataHubEntities;
            stash[$"{stashTo}_mergeResults"] = mergeResult;
        }
        return resultingDataHubEntities!;
    }

    public DataverseAgent MergeRecords<TDataverse, TDataHub>(string fromStash, string? stashTo = null, bool forceUpdate = false) where TDataverse : Entity where TDataHub : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecords = await mergeRecords<TDataverse, TDataHub>(currentObject, stash, (_, _) => stash[fromStash].ToObject<List<TDataverse>>().Select(s => s.Id).ToList(), stashTo, forceUpdate);
            return new ScenarioActionResult() { CurrentObject = resultingRecords, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    #endregion

    #region UpdateRecord

    private async Task<TDataverse> updateRecord<TDataverse>(object currentObject, Dictionary<string, object?> stash, Func<object, Dictionary<string, object?>, TDataverse> recordFunc, string stashTo = null) where TDataverse : Entity
    {
        var record = recordFunc(currentObject, stash);
        var updateResponse = await SendUsingMediator(new UpdateDataverseRecordCommand<TDataverse>()
        {
            DataverseEntity = record
        });

        var getResponse = await SendUsingMediator(new GetSpecificDataverseEntitiesRequest<TDataverse>()
        {
            EntityIds = [updateResponse]
        });

        var resultingRecord = getResponse.Results.FirstOrDefault();
        if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = resultingRecord;
        return resultingRecord;
    }

    public DataverseAgent UpdateRecord<TDataverse>(TDataverse record) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await updateRecord(currentObject, stash, (co, _) => record);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent UpdateRecord<TDataverse>(TDataverse record, string stashTo) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await updateRecord(currentObject, stash, (co, _) => record, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent UpdateRecord<TDataverse>(Func<object, Dictionary<string, object?>, TDataverse> recordFunc, string stashTo) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await updateRecord(currentObject, stash, recordFunc, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    #endregion

    #region GetRecord

    private async Task<TDataverse?> getRecord<TDataverse>(object currentObject, Dictionary<string, object?> stash, Func<object, Dictionary<string, object?>, Guid> recordFunc, string? stashTo = null) where TDataverse : Entity
    {
        var recordId = recordFunc(currentObject, stash);
        var getResponse = await SendUsingMediator(new GetSpecificDataverseEntitiesRequest<TDataverse>()
        {
            EntityIds = [recordId]
        });

        if (!getResponse.Success)
        {
            if (getResponse.FailureReason.Contains("Does Not Exist"))
            {
                return null;
            }

            throw new Exception(getResponse.FailureReason);
        }

        var resultingRecord = getResponse.Results.FirstOrDefault();
        if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = resultingRecord;
        return resultingRecord;
    }

    public DataverseAgent GetRecord<TDataverse>(Func<object, Dictionary<string, object?>, Guid> recordFunc, string? stashTo) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await getRecord<TDataverse>(currentObject, stash, recordFunc, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent GetRecord<TDataverse>(Guid recordId, string? stashTo) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await getRecord<TDataverse>(currentObject, stash, (_, _) => recordId, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    #endregion

    #region Get Records

    private async Task<List<TDataverse>> getRecordBysFilterExpression<TDataverse>(FilterExpression filterExpression, Dictionary<string, object?> stash, string stashTo = null) where TDataverse : Entity
    {
        var getResponse = await SendUsingMediator(new GetAllDataverseEntitiesRequest<TDataverse>()
        {
            FilterExpression = filterExpression
        });

        if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = getResponse;
        return getResponse;
    }

    public DataverseAgent GetRecords<TDataverse>(FilterExpression filterExpression, string stashTo) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecords = await getRecordBysFilterExpression<TDataverse>(filterExpression, stash, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecords, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent GetRecords<TDataverse>(Func<object, Dictionary<string, object?>, FilterExpression> filterExpression, string stashTo) where TDataverse : Entity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var fe = filterExpression(currentObject, stash);
            var resultingRecords = await getRecordBysFilterExpression<TDataverse>(fe, stash, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecords, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    //public DataverseAgent GetRecords<TDataverse>(string filterExpressionCache, string stashTo) where TDataverse : Entity
    //{
    //    async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
    //    {
    //        var filterExpression = (FilterExpression)stash[filterExpressionCache];
    //        var resultingRecords = await getRecordBysFilterExpression<TDataverse>(filterExpression, stash, stashTo);
    //        return new ScenarioActionResult() { CurrentObject = resultingRecords, Outputs = stash };
    //    }
    //    ScenarioBuilder.Enqueue(f);
    //    return this;
    //}

    #endregion

    #region Sync Record

    private async Task<TDataverse> syncRecord<TDataHub, TDataverse>(object currentObject, Dictionary<string, object?> stash, Func<object, Dictionary<string, object?>, string> entityIdFunc, string stashTo = null) where TDataverse : Entity where TDataHub : DataHubEntity
    {
        var entityId = entityIdFunc(currentObject, stash);
        var syncResponse = await SendUsingMediator(new SyncSpecificDataHubEntitiesRequest<TDataHub, TDataverse>()
        {
            EntityIds = [entityId]
        });

        var syncResult = syncResponse.Results.FirstOrDefault();
        if (syncResult is null)
        {
            throw new InvalidOperationException("Dataverse sync did not return a result.");
        }

        if (SyncOutcomes.IsFailure(syncResult.SyncOutcome))
        {
            throw new InvalidOperationException(syncResult.FailureReason ?? "Dataverse sync failed.");
        }

        TDataverse? resultingRecord = null;

        if (syncResult != null)
        {
            var dataHubClient = AgentServices.GetRequiredService<IDataHubClient>();
            var getDataHubEntityResponse = await dataHubClient.PostRequestAsync<GetDataHubEntityRequest, GetDataHubEntityResponse>(new GetDataHubEntityRequest()
            {
                EntityType = syncResult!.DataHubEntityType,
                EntityId = syncResult!.DataHubEntityId
            }, CancellationToken.None);

            var resultingDataHubEntity = getDataHubEntityResponse.Entity.ToObject<TDataHub>();

            var dataverseKey = GetDataverseAlternateKey<TDataverse>(resultingDataHubEntity!);

            var getResponse = await SendUsingMediator(new GetSpecificDataverseEntitiesRequest<TDataverse>()
            {
                EntityIds = [Guid.Parse(dataverseKey)]
            });

            resultingRecord = getResponse.Results.FirstOrDefault();
        }

        if (!string.IsNullOrEmpty(stashTo))
        {
            stash[stashTo] = resultingRecord;
            stash[$"{stashTo}_syncResult"] = syncResult;
        }

        return resultingRecord!;
    }

    public DataverseAgent SyncRecord<TDataHub, TDataverse>(TDataHub record, string stashTo = null) where TDataverse : Entity where TDataHub : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecord = await syncRecord<TDataHub, TDataverse>(currentObject, stash, (co, _) => record.id, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    public DataverseAgent SyncRecord<TDataHub, TDataverse>(string fromStash, string stashTo = null) where TDataverse : Entity where TDataHub : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            if (!stash.TryGetValue(fromStash, out var value)) throw new Exception($"SyncRecord: key {fromStash} not found in stash");
            if (value == null) throw new Exception($"SyncRecord: {fromStash} value is null");

            var resultingRecord = await syncRecord<TDataHub, TDataverse>(currentObject, stash, (_, _) => stash[fromStash].ToObject<TDataHub>()!.id, stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecord, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    private async Task<List<TDataverse>> syncRecords<TDataHub, TDataverse>(object currentObject, Dictionary<string, object?> stash, Func<object, Dictionary<string, object?>, List<string>> entityIdsFunc, string stashTo = null) where TDataverse : Entity where TDataHub : DataHubEntity
    {
        var entityIds = entityIdsFunc(currentObject, stash);
        var syncResponse = await SendUsingMediator(new SyncSpecificDataHubEntitiesRequest<TDataHub, TDataverse>()
        {
            EntityIds = entityIds
        });

        var syncResult = syncResponse.Results;
        var failures = syncResult.Where(result => SyncOutcomes.IsFailure(result.SyncOutcome)).ToList();
        if (failures.Count != 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures.Select(result => result.FailureReason ?? "Dataverse sync failed.")));
        }

        var dataHubClient = AgentServices.GetRequiredService<IDataHubClient>();
        var getDataHubEntityResponse = await dataHubClient.PostRequestAsync<GetDataHubEntitiesByIdRequest, GetDataHubEntitiesByIdResponse>(new GetDataHubEntitiesByIdRequest()
        {
            EntityType = syncResult.FirstOrDefault()!.DataHubEntityType,
            EntityIds = syncResult!.Select(s => s.DataHubEntityId).ToList()
        }, CancellationToken.None);

        var resultingDataHubEntities = getDataHubEntityResponse.Results.Select(s => s.ToObject<TDataHub>()).ToList();

        var dataverseKeys = resultingDataHubEntities!.Select(GetDataverseAlternateKey<TDataverse>).ToList();

        var getResponse = await SendUsingMediator(new GetSpecificDataverseEntitiesRequest<TDataverse>()
        {
            EntityIds = dataverseKeys.Select(s => new Guid(s)).ToList()
        });

        var resultingRecords = getResponse;
        if (!string.IsNullOrEmpty(stashTo)) stash[stashTo] = resultingRecords;
        if (!string.IsNullOrEmpty(stashTo))
        {
            stash[stashTo] = resultingRecords;
            stash[$"{stashTo}_syncResults"] = syncResult;
        }
        return resultingRecords!.Results;
    }

    public DataverseAgent SyncRecords<TDataHub, TDataverse>(string fromStash, string stashTo = null) where TDataverse : Entity where TDataHub : DataHubEntity
    {
        async Task<ScenarioActionResult> f(object currentObject, Dictionary<string, object?> stash)
        {
            var resultingRecords = await syncRecords<TDataHub, TDataverse>(currentObject, stash, (_, _) => stash[fromStash].ToObject<List<TDataHub>>().Select(s => s.id).ToList(), stashTo);
            return new ScenarioActionResult() { CurrentObject = resultingRecords, Outputs = stash };
        }
        ScenarioBuilder.Enqueue(f);
        return this;
    }

    private static string GetDataverseAlternateKey<TDataverse>(DataHubEntity dataHubEntity)
        where TDataverse : Entity
    {
        var logicalName = typeof(TDataverse).GetField("EntityLogicalName")?.GetValue(null)?.ToString();
        var alternateKeyNames = new[]
            {
                $"dataverse.{logicalName}",
                $"dataverse.{typeof(TDataverse).Name}"
            }
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.ToLowerInvariant())
            .ToHashSet();

        var alternateKey = dataHubEntity.alternateKeys.FirstOrDefault(key => alternateKeyNames.Contains(key.Key.ToLowerInvariant()));
        if (alternateKey is null)
        {
            throw new InvalidOperationException($"DataHub entity '{dataHubEntity.entityType}/{dataHubEntity.id}' does not contain a Dataverse alternate key for '{typeof(TDataverse).Name}'.");
        }

        return alternateKey.Value;
    }

    #endregion

}
