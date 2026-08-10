using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Reimaginate.DataHub;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.CreateDataverseRecords;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecords;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Models;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.EnsureReferencedEntitiesAreSyncd;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseDeletionEvents;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessNewEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUntrackedEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUpdatedEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ResolveResolutionPromises;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SyncEntities;
using Reimaginate.DataHub.Agent.Dataverse.Services.DataHubEntityCache;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using Reimaginate.ProcessingLockService;
using Reimaginate.ProcessingLockService.Abstractions;
using Xunit;
using DataHubEntityReference = Reimaginate.DataHub.SharedModels.Core.EntityReference;
using DataverseAccount = DataverseModel.Account;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Requests.Internal;

public sealed class D365AlternateKeyTests
{
    [Fact(DisplayName = "D365 alternate key routes a tracked entity to the update path")]
    [Trait("Category", "Unit")]
    public async Task ProcessSyncRoutesD365AlternateKeyToTrackedUpdatePath()
    {
        var mediator = Substitute.For<IMediator>();
        IRequest dispatchedRequest = null;
        mediator.TrySend<ProcessUpdatedEntitiesResponse>(
                Arg.Do<IRequest<ProcessUpdatedEntitiesResponse>>(request => dispatchedRequest = request),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(new ProcessUpdatedEntitiesResponse { SyncResults = [] }));
        var entity = TrackedEntityWithD365Key(Guid.NewGuid());
        var handler = new ProcessSyncRequestHandler<D365TrackedAccount, DataverseAccount>(D365Options(), mediator);

        await handler.HandleAsync(new ProcessSyncRequest<D365TrackedAccount, DataverseAccount>([entity]), CancellationToken.None);

        var updateRequest = Assert.IsType<ProcessUpdatedEntitiesRequest<D365TrackedAccount, DataverseAccount>>(dispatchedRequest);
        Assert.Same(entity, Assert.Single(updateRequest.EntitiesToUpdate));
    }

    [Fact(DisplayName = "D365 alternate key routes an untracked entity to the untracked update path")]
    [Trait("Category", "Unit")]
    public async Task ProcessSyncRoutesD365AlternateKeyToUntrackedUpdatePath()
    {
        var mediator = Substitute.For<IMediator>();
        IRequest dispatchedRequest = null;
        mediator.TrySend<ProcessUntrackedEntitiesResponse>(
                Arg.Do<IRequest<ProcessUntrackedEntitiesResponse>>(request => dispatchedRequest = request),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(new ProcessUntrackedEntitiesResponse { SyncResults = [] }));
        var sourceId = Guid.NewGuid();
        var entity = new D365UntrackedAccount
        {
            id = Guid.NewGuid().ToString(),
            alternateKeys = [D365AccountKey(sourceId)]
        };
        var handler = new ProcessSyncRequestHandler<D365UntrackedAccount, DataverseAccount>(D365Options(), mediator);

        await handler.HandleAsync(new ProcessSyncRequest<D365UntrackedAccount, DataverseAccount>([entity]), CancellationToken.None);

        var updateRequest = Assert.IsType<ProcessUntrackedEntitiesRequest<D365UntrackedAccount, DataverseAccount>>(dispatchedRequest);
        Assert.Same(entity, Assert.Single(updateRequest.EntitiesToUpdate));
    }

    [Fact(DisplayName = "D365 sync blacklist and whitelist entries control synchronization")]
    [Trait("Category", "Unit")]
    public async Task SyncEntitiesUsesD365ForSyncBlacklistAndWhitelist()
    {
        var blacklistedId = Guid.NewGuid().ToString();
        var whitelistedId = Guid.NewGuid().ToString();
        var blacklistedEntity = JObject.FromObject(new D365TrackedAccount
        {
            id = blacklistedId,
            syncBlacklist = [D365AlternateKeyTestData.NormalizedDataSource]
        });
        blacklistedEntity[nameof(DataHubEntity.noSync)] = false;
        var whitelistedEntity = JObject.FromObject(new D365TrackedAccount
        {
            id = whitelistedId,
            noSync = true,
            syncWhitelist = [D365AlternateKeyTestData.NormalizedDataSource]
        });
        var dataHubClient = Substitute.For<IDataHubClient>();
        var mediator = Substitute.For<IMediator>();
        var processingLockService = Substitute.For<IProcessingLockService>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        ProcessSyncRequest<D365TrackedAccount, DataverseAccount> dispatchedRequest = null;
        var processingLocks = new List<ProcessingLock> { new() };

        dataHubClient.PostRequestAsync<GetDataHubEntitiesByIdRequest, GetDataHubEntitiesByIdResponse>(
                Arg.Any<GetDataHubEntitiesByIdRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new GetDataHubEntitiesByIdResponse { Results = [blacklistedEntity, whitelistedEntity] });
        processingLockService.WaitForLocksAsync(
                Arg.Any<List<string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<TimeSpan?>())
            .Returns(new Response<List<ProcessingLock>>(true, processingLocks, null));
        processingLockService.ReleaseLocksAsync(
                Arg.Any<IList<ProcessingLock>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Response<List<Response<Null>>>(true, [], null));
        mediator.TrySend<ProcessSyncResponse>(
                Arg.Do<IRequest<ProcessSyncResponse>>(request =>
                    dispatchedRequest = Assert.IsType<ProcessSyncRequest<D365TrackedAccount, DataverseAccount>>(request)),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(new ProcessSyncResponse()));
        var handler = new SyncEntitiesRequestHandler<D365TrackedAccount, DataverseAccount>(
            D365Options(), dataHubClient, mediator, Substitute.For<IMapper>(), processingLockService, serviceProvider);

        await handler.HandleAsync(new SyncEntitiesRequest<D365TrackedAccount, DataverseAccount>
        {
            CorrelationId = "d365-filtering",
            EntityIds = [blacklistedId, whitelistedId]
        }, CancellationToken.None);

        var entityToSync = Assert.Single(dispatchedRequest.DataHubEntities);
        Assert.Equal(whitelistedId, entityToSync.id);
    }

    [Fact(DisplayName = "New entity creation emits and registers a D365 alternate key")]
    [Trait("Category", "Unit")]
    public async Task ProcessNewEntitiesEmitsAndRegistersD365AlternateKey()
    {
        var sourceId = Guid.NewGuid();
        var entity = new D365TrackedAccount { id = Guid.NewGuid().ToString(), Name = "Created" };
        var mappedEntity = new DataverseAccount { Id = sourceId, Name = entity.Name };
        var dataHubClient = Substitute.For<IDataHubClient>();
        var dataHubEntityCache = Substitute.For<IDataHubEntityCache>();
        var mapper = Substitute.For<IMapper>();
        var mediator = Substitute.For<IMediator>();
        RegisterAlternateKeysRequest registrationRequest = null;
        UpdateEntitiesRequest trackingUpdateRequest = null;

        mapper.MapAsync<DataverseAccount>(
                Arg.Any<D365TrackedAccount>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Dictionary<string, object>>())
            .Returns(mappedEntity);
        mediator.TrySend<EnsureReferencedEntitiesAreSyncdResponse<D365TrackedAccount, DataverseAccount>>(
                Arg.Is<IRequest<EnsureReferencedEntitiesAreSyncdResponse<D365TrackedAccount, DataverseAccount>>>(request => request is EnsureReferencedEntitiesAreSyncdRequest<D365TrackedAccount, DataverseAccount>),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(EmptyReferenceResponse<D365TrackedAccount>()));
        mediator.TrySend<CreateDataverseRecordsResponse<DataverseAccount>>(
                Arg.Is<IRequest<CreateDataverseRecordsResponse<DataverseAccount>>>(request => request is CreateDataverseRecordsCommand<DataverseAccount>),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(new CreateDataverseRecordsResponse<DataverseAccount>
            {
                Results = new Dictionary<string, CreateResult<DataverseAccount>>
                {
                    [entity.id] = new()
                    {
                        EntityId = sourceId,
                        ResultingEntity = mappedEntity,
                        Success = true
                    }
                }
            }));
        dataHubClient.PostRequestAsync<RegisterAlternateKeysRequest, RegisterAlternateKeysResponse>(
                Arg.Do<RegisterAlternateKeysRequest>(request => registrationRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new RegisterAlternateKeysResponse { Responses = [] });
        dataHubClient.PostRequestAsync<UpdateEntitiesRequest, UpdateEntitiesResponse>(
                Arg.Do<UpdateEntitiesRequest>(request => trackingUpdateRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new UpdateEntitiesResponse());
        var handler = new ProcessNewEntitiesRequestHandler<D365TrackedAccount, DataverseAccount>(
            D365Options(), dataHubClient, dataHubEntityCache, mapper, mediator);

        var response = await handler.HandleAsync(new ProcessNewEntitiesRequest<D365TrackedAccount, DataverseAccount>
        {
            CorrelationId = "d365-create",
            EntitiesToCreate = [entity]
        }, CancellationToken.None);

        var emittedKey = Assert.Single(entity.alternateKeys);
        Assert.Equal(D365AlternateKeyTestData.AccountKey, emittedKey.Key);
        Assert.Equal(sourceId.ToString(), emittedKey.Value);
        var registeredKey = Assert.Single(Assert.IsType<RegisterAlternateKeysRequest>(registrationRequest).Requests);
        Assert.Equal(D365AlternateKeyTestData.AccountKey, registeredKey.Key);
        Assert.Equal(sourceId.ToString(), registeredKey.SourceEntityId);
        Assert.Equal(D365AlternateKeyTestData.DataSource, Assert.Single(trackingUpdateRequest.Requests).DataSource);
        Assert.Equal(sourceId.ToString(), Assert.Single(response.SyncResults).SourceEntityId);
    }

    [Fact(DisplayName = "Referenced entity with a D365 alternate key is already synchronized")]
    [Trait("Category", "Unit")]
    public async Task EnsureReferencedEntitiesRecognizesD365AlternateKey()
    {
        var referencedId = Guid.NewGuid().ToString();
        var cache = Substitute.For<IDataHubEntityCache>();
        var mediator = Substitute.For<IMediator>();
        var referencedEntity = JObject.FromObject(new D365TrackedAccount
        {
            id = referencedId,
            alternateKeys = [D365AccountKey(Guid.NewGuid())]
        });
        cache.GetDataHubEntities(
                nameof(D365TrackedAccount),
                Arg.Is<List<string>>(ids => ids.SequenceEqual(new[] { referencedId })),
                Arg.Any<CancellationToken>(),
                Arg.Any<bool?>())
            .Returns([referencedEntity]);
        var handler = new EnsureReferencedEntitiesAreSyncdRequestHandler<D365TrackedAccount, DataverseAccount>(
            D365Options(), cache, mediator);

        var response = await handler.HandleAsync(new EnsureReferencedEntitiesAreSyncdRequest<D365TrackedAccount, DataverseAccount>
        {
            Entities =
            [
                new D365TrackedAccount
                {
                    id = Guid.NewGuid().ToString(),
                    ParentAccount = new DataHubEntityReference
                    {
                        EntityType = nameof(D365TrackedAccount),
                        EntityId = referencedId
                    }
                }
            ],
            DependencyTree = [],
            ResolutionPromises = []
        }, CancellationToken.None);

        Assert.Same(referencedEntity, Assert.Single(response.CachedEntities));
        Assert.Empty(response.ResolutionPromises);
        await mediator.DidNotReceiveWithAnyArgs().SendAsync(default, default);
    }

    [Fact(DisplayName = "Tracked update retrieves the Dataverse record identified by a D365 alternate key")]
    [Trait("Category", "Unit")]
    public async Task ProcessUpdatedEntitiesUsesD365AlternateKeyValue()
    {
        var sourceId = Guid.NewGuid();
        var entity = TrackedEntityWithD365Key(sourceId);
        var dataHubClient = Substitute.For<IDataHubClient>();
        var mapper = Substitute.For<IMapper>();
        var mediator = Substitute.For<IMediator>();
        var timeService = Substitute.For<ITimeService>();
        GetTrackedEntitiesRequest trackedRequest = null;
        GetSpecificDataverseEntitiesRequest<DataverseAccount> sourceRequest = null;

        dataHubClient.PostRequestAsync<GetTrackedEntitiesRequest, GetTrackedEntitiesResponse>(
                Arg.Do<GetTrackedEntitiesRequest>(request => trackedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new GetTrackedEntitiesResponse { Results = [] });
        mediator.TrySend<GetSpecificDataverseEntitiesResponse<DataverseAccount>>(
                Arg.Is<IRequest<GetSpecificDataverseEntitiesResponse<DataverseAccount>>>(request => request is GetSpecificDataverseEntitiesRequest<DataverseAccount>),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(call =>
            {
                sourceRequest = (GetSpecificDataverseEntitiesRequest<DataverseAccount>)call.Arg<IRequest<GetSpecificDataverseEntitiesResponse<DataverseAccount>>>();
                return TrySuccess(new GetSpecificDataverseEntitiesResponse<DataverseAccount> { Success = true, Results = [] });
            });
        mediator.TrySend<EnsureReferencedEntitiesAreSyncdResponse<D365TrackedAccount, DataverseAccount>>(
                Arg.Is<IRequest<EnsureReferencedEntitiesAreSyncdResponse<D365TrackedAccount, DataverseAccount>>>(request => request is EnsureReferencedEntitiesAreSyncdRequest<D365TrackedAccount, DataverseAccount>),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(EmptyReferenceResponse<D365TrackedAccount>()));
        mapper.MapAsync<DataverseAccount>(
                Arg.Any<D365TrackedAccount>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Dictionary<string, object>>())
            .Returns(new DataverseAccount { Id = sourceId });
        var handler = new ProcessUpdatedEntitiesRequestHandler<D365TrackedAccount, DataverseAccount>(
            D365Options(), dataHubClient, mapper, mediator, timeService);

        var response = await handler.HandleAsync(new ProcessUpdatedEntitiesRequest<D365TrackedAccount, DataverseAccount>
        {
            EntitiesToUpdate = [entity]
        }, CancellationToken.None);

        Assert.Equal(D365AlternateKeyTestData.DataSource, trackedRequest.DataSource);
        Assert.Equal(sourceId.ToString(), Assert.Single(trackedRequest.EntityIds));
        Assert.Equal(sourceId, Assert.Single(sourceRequest.EntityIds));
        Assert.Equal(sourceId.ToString(), Assert.Single(response.SyncResults).SourceEntityId);
    }

    [Fact(DisplayName = "Duplicate merge failures retain a failed sync result")]
    [Trait("Category", "Unit")]
    public async Task ProcessUpdatedEntitiesHandlesDuplicateMergeFailures()
    {
        var sourceId = Guid.NewGuid();
        var entity = TrackedEntityWithD365Key(sourceId);
        var dataHubClient = Substitute.For<IDataHubClient>();
        var mediator = Substitute.For<IMediator>();

        dataHubClient.PostRequestAsync<GetTrackedEntitiesRequest, GetTrackedEntitiesResponse>(
                Arg.Any<GetTrackedEntitiesRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new GetTrackedEntitiesResponse
            {
                Results =
                [
                    new()
                    {
                        Success = true,
                        EntityId = sourceId.ToString(),
                        Data = null
                    }
                ]
            });
        mediator.TrySend<GetSpecificDataverseEntitiesResponse<DataverseAccount>>(
                Arg.Any<IRequest<GetSpecificDataverseEntitiesResponse<DataverseAccount>>>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(new GetSpecificDataverseEntitiesResponse<DataverseAccount>
            {
                Success = true,
                Results = [new DataverseAccount { Id = sourceId }]
            }));
        mediator.TrySend<EnsureReferencedEntitiesAreSyncdResponse<D365TrackedAccount, DataverseAccount>>(
                Arg.Any<IRequest<EnsureReferencedEntitiesAreSyncdResponse<D365TrackedAccount, DataverseAccount>>>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(EmptyReferenceResponse<D365TrackedAccount>()));
        mediator.TrySend<ProcessMergeResponse>(
                Arg.Any<IRequest<ProcessMergeResponse>>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(new ProcessMergeResponse
            {
                Results =
                [
                    new MergeEntityResult
                    {
                        SourceEntityId = sourceId.ToString(),
                        MergeOutcome = MergeOutcomes.MergeFailed,
                        FailureReason = "first merge failure"
                    },
                    new MergeEntityResult
                    {
                        SourceEntityId = sourceId.ToString(),
                        MergeOutcome = MergeOutcomes.MergeFailed,
                        FailureReason = "second merge failure"
                    }
                ]
            }));
        var handler = new ProcessUpdatedEntitiesRequestHandler<D365TrackedAccount, DataverseAccount>(
            D365Options(),
            dataHubClient,
            Substitute.For<IMapper>(),
            mediator,
            Substitute.For<ITimeService>());

        var response = await handler.HandleAsync(new ProcessUpdatedEntitiesRequest<D365TrackedAccount, DataverseAccount>
        {
            EntitiesToUpdate = [entity]
        }, CancellationToken.None);

        var syncResult = Assert.Single(response.SyncResults);
        Assert.Equal(SyncOutcomes.SyncFailed, syncResult.SyncOutcome);
        Assert.EndsWith("second merge failure", syncResult.FailureReason);
    }

    [Fact(DisplayName = "Untracked update and optimistic tracking use the D365 alternate key source")]
    [Trait("Category", "Unit")]
    public async Task ProcessUntrackedEntitiesUsesD365AlternateKeyValueAndDataSource()
    {
        var sourceId = Guid.NewGuid();
        var entity = new D365UntrackedAccount
        {
            id = Guid.NewGuid().ToString(),
            Name = "After",
            alternateKeys = [D365AccountKey(sourceId)]
        };
        var currentEntity = new DataverseAccount { Id = sourceId, Name = "Before", RowVersion = "1" };
        var mappedEntity = new DataverseAccount { Id = sourceId, Name = entity.Name };
        var dataHubClient = Substitute.For<IDataHubClient>();
        var mapper = Substitute.For<IMapper>();
        var mediator = Substitute.For<IMediator>();
        GetSpecificDataverseEntitiesRequest<DataverseAccount> sourceRequest = null;
        UpdateEntitiesRequest trackingUpdateRequest = null;

        mediator.TrySend<GetSpecificDataverseEntitiesResponse<DataverseAccount>>(
                Arg.Is<IRequest<GetSpecificDataverseEntitiesResponse<DataverseAccount>>>(request => request is GetSpecificDataverseEntitiesRequest<DataverseAccount>),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(call =>
            {
                sourceRequest = (GetSpecificDataverseEntitiesRequest<DataverseAccount>)call.Arg<IRequest<GetSpecificDataverseEntitiesResponse<DataverseAccount>>>();
                return TrySuccess(new GetSpecificDataverseEntitiesResponse<DataverseAccount>
                {
                    Success = true,
                    Results = [currentEntity]
                });
            });
        mediator.TrySend<EnsureReferencedEntitiesAreSyncdResponse<D365UntrackedAccount, DataverseAccount>>(
                Arg.Is<IRequest<EnsureReferencedEntitiesAreSyncdResponse<D365UntrackedAccount, DataverseAccount>>>(request => request is EnsureReferencedEntitiesAreSyncdRequest<D365UntrackedAccount, DataverseAccount>),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(EmptyReferenceResponse<D365UntrackedAccount>()));
        mediator.TrySend<UpdateDataverseRecordsResponse<DataverseAccount>>(
                Arg.Is<IRequest<UpdateDataverseRecordsResponse<DataverseAccount>>>(request => request is UpdateDataverseRecordsCommand<DataverseAccount>),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(new UpdateDataverseRecordsResponse<DataverseAccount>()));
        mapper.MapAsync<D365UntrackedAccount, DataverseAccount>(
                Arg.Any<D365UntrackedAccount>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Dictionary<string, object>>())
            .Returns(mappedEntity);
        dataHubClient.PostRequestAsync<UpdateEntitiesRequest, UpdateEntitiesResponse>(
                Arg.Do<UpdateEntitiesRequest>(request => trackingUpdateRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new UpdateEntitiesResponse());
        var handler = new ProcessUntrackedEntitiesRequestHandler<D365UntrackedAccount, DataverseAccount>(
            D365Options(), dataHubClient, mapper, mediator);

        var response = await handler.HandleAsync(new ProcessUntrackedEntitiesRequest<D365UntrackedAccount, DataverseAccount>
        {
            CorrelationId = "d365-untracked-update",
            EntitiesToUpdate = [entity]
        }, CancellationToken.None);

        if (trackingUpdateRequest == null)
        {
            Assert.Fail(Assert.Single(response.SyncResults).FailureReason ?? "No optimistic tracking request was sent.");
        }
        Assert.Equal(sourceId, Assert.Single(sourceRequest.EntityIds));
        Assert.Equal(D365AlternateKeyTestData.DataSource, Assert.Single(trackingUpdateRequest.Requests).DataSource);
    }

    [Fact(DisplayName = "Deletion removes only the configured D365 alternate key and blacklists D365")]
    [Trait("Category", "Unit")]
    public async Task ProcessDeletionRemovesD365AlternateKey()
    {
        var sourceId = Guid.NewGuid();
        var dataHubId = Guid.NewGuid().ToString();
        var dataHubClient = Substitute.For<IDataHubClient>();
        PatchEntitiesRequest patchRequest = null;
        var entity = JObject.FromObject(new D365TrackedAccount
        {
            id = dataHubId,
            alternateKeys =
            [
                D365AccountKey(sourceId),
                new AlternateKey("salesforce.account", "sf-account")
            ],
            syncBlacklist = []
        });
        dataHubClient.PostRequestAsync<ResolveEntityReferencesRequest, ResolveEntityReferencesResponse>(
                Arg.Any<ResolveEntityReferencesRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResolveEntityReferencesResponse
            {
                Results =
                [
                    new ResolvedEntityReference
                    {
                        DataHubEntityReference = new DataHubEntityReference
                        {
                            EntityType = nameof(D365TrackedAccount),
                            EntityId = dataHubId
                        },
                        SourceEntityReference = new ExternalEntityReference
                        {
                            DataSource = D365AlternateKeyTestData.DataSource,
                            SourceEntityType = DataverseAccount.EntityLogicalName,
                            EntityId = sourceId.ToString()
                        }
                    }
                ],
                ResolutionFailures = []
            });
        dataHubClient.PostRequestAsync<GetDataHubEntitiesByIdRequest, GetDataHubEntitiesByIdResponse>(
                Arg.Any<GetDataHubEntitiesByIdRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new GetDataHubEntitiesByIdResponse { Results = [entity] });
        dataHubClient.PostRequestAsync<PatchEntitiesRequest, PatchEntitiesResponse>(
                Arg.Do<PatchEntitiesRequest>(request => patchRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new PatchEntitiesResponse { Success = true });
        var handler = new ProcessDataverseDeletionEventsRequestHandler(dataHubClient, D365Options());

        var response = await handler.HandleAsync(new ProcessDataverseDeletionEventsRequest
        {
            DataHubAssemblyMarker = typeof(D365TrackedAccount),
            Events =
            [
                new DeletionEvent
                {
                    EntityType = DataverseAccount.EntityLogicalName,
                    EntityId = sourceId.ToString()
                }
            ]
        }, CancellationToken.None);

        Assert.True(response.Success);
        var operations = Assert.Single(patchRequest.Requests).Operations;
        var alternateKeys = Assert.IsType<JArray>(operations.Single(operation => operation.Path == nameof(DataHubEntity.alternateKeys)).Value)
            .ToObject<List<AlternateKey>>();
        Assert.DoesNotContain(alternateKeys, key => key.Key == D365AlternateKeyTestData.AccountKey);
        Assert.Contains(alternateKeys, key => key.Key == "salesforce.account");
        var blacklist = Assert.IsType<JArray>(operations.Single(operation => operation.Path == nameof(DataHubEntity.syncBlacklist)).Value);
        Assert.Contains(D365AlternateKeyTestData.NormalizedDataSource, blacklist.Values<string>());
        Assert.DoesNotContain("dataverse", blacklist.Values<string>());
    }

    [Fact(DisplayName = "Resolution promises select the D365 alternate key instead of a Dataverse key")]
    [Trait("Category", "Unit")]
    public async Task ResolveResolutionPromisesSelectsD365AlternateKey()
    {
        var targetId = Guid.NewGuid().ToString();
        var referringDataHubId = Guid.NewGuid().ToString();
        var dataverseId = Guid.NewGuid();
        var d365Id = Guid.NewGuid();
        var cache = Substitute.For<IDataHubEntityCache>();
        var mapper = Substitute.For<IMapper>();
        var mediator = Substitute.For<IMediator>();
        UpdateDataverseRecordsCommand updateRequest = null;
        var target = JObject.FromObject(new D365TrackedAccount { id = targetId });
        var referringEntity = JObject.FromObject(new D365TrackedAccount
        {
            id = referringDataHubId,
            alternateKeys =
            [
                new AlternateKey("dataverse.account", dataverseId.ToString()),
                D365AccountKey(d365Id)
            ]
        });
        cache.GetDataHubEntities(
                nameof(D365TrackedAccount),
                Arg.Is<List<string>>(ids => ids.SequenceEqual(new[] { targetId })),
                Arg.Any<CancellationToken>(),
                Arg.Any<bool?>())
            .Returns([target]);
        cache.GetDataHubEntities(
                nameof(D365TrackedAccount),
                Arg.Is<List<string>>(ids => ids.SequenceEqual(new[] { referringDataHubId })),
                Arg.Any<CancellationToken>(),
                Arg.Any<bool?>())
            .Returns([referringEntity]);
        mapper.MapAsync(
                Arg.Any<object>(),
                Arg.Is<Type>(type => type == typeof(DataverseAccount)),
                Arg.Any<CancellationToken>(),
                Arg.Any<Dictionary<string, object>>())
            .Returns(new DataverseAccount());
        mediator.TrySend<UpdateDataverseRecordsResponse<Microsoft.Xrm.Sdk.Entity>>(
                Arg.Is<IRequest<UpdateDataverseRecordsResponse<Microsoft.Xrm.Sdk.Entity>>>(request => request is UpdateDataverseRecordsCommand),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(call =>
            {
                updateRequest = (UpdateDataverseRecordsCommand)call.Arg<IRequest<UpdateDataverseRecordsResponse<Microsoft.Xrm.Sdk.Entity>>>();
                return TrySuccess(new UpdateDataverseRecordsResponse<Microsoft.Xrm.Sdk.Entity>());
            });
        var promise = new ResolutionPromise
        {
            DataHubEntityId = referringDataHubId,
            DataHubEntityType = nameof(D365TrackedAccount),
            EntityReferencePath = nameof(D365TrackedAccount.ParentAccount),
            ExternalEntityReference = new ExternalEntityReference
            {
                DataSource = "DataHub",
                EntityType = nameof(D365TrackedAccount),
                EntityId = targetId
            }
        };
        var handler = new ResolveResolutionPromisesRequestHandler<D365TrackedAccount, DataverseAccount>(
            cache, mapper, mediator, D365Options());

        var response = await handler.HandleAsync(new ResolveResolutionPromisesRequest<D365TrackedAccount, DataverseAccount>
        {
            EntitiesToResolve = [new D365TrackedAccount { id = targetId }],
            ResolutionPromises = [promise]
        }, CancellationToken.None);

        Assert.Same(promise, Assert.Single(response.ResolvedPromises));
        Assert.Equal(d365Id, Assert.Single(updateRequest.Records).Value.Id);
        Assert.NotEqual(dataverseId, Assert.Single(updateRequest.Records).Value.Id);
    }

    private static IOptions<DataverseAgentOptions> D365Options() => Options.Create(new DataverseAgentOptions
    {
        DataSource = D365AlternateKeyTestData.DataSource
    });

    private static D365TrackedAccount TrackedEntityWithD365Key(Guid sourceId) => new()
    {
        id = Guid.NewGuid().ToString(),
        alternateKeys = [D365AccountKey(sourceId)]
    };

    private static AlternateKey D365AccountKey(Guid sourceId) =>
        new(D365AlternateKeyTestData.AccountKey, sourceId.ToString());

    private static EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, DataverseAccount> EmptyReferenceResponse<TDataHubEntity>()
        where TDataHubEntity : DataHubEntity, new() => new()
    {
        CachedEntities = [],
        Failures = [],
        ResolutionPromises = []
    };

    private static Task<(TResponse, Exception)> TrySuccess<TResponse>(TResponse response) =>
        Task.FromResult((response, (Exception)null));
}

internal static class D365AlternateKeyTestData
{
    public const string DataSource = "D365";
    public const string NormalizedDataSource = "d365";
    public const string AccountKey = "d365.account";
    public const string DataverseAccountType = "DataverseModel.Account, Reimaginate.DataHub.Agent.Dataverse.Tests.Shared";
}

[RelatedEntityType(D365AlternateKeyTestData.DataSource, D365AlternateKeyTestData.DataverseAccountType)]
public sealed class D365TrackedAccount : DataHubEntity
{
    public D365TrackedAccount()
    {
        entityType = nameof(D365TrackedAccount);
    }

    public string Name { get; set; }
    public DataHubEntityReference ParentAccount { get; set; }
}

[DoNotTrack]
[RelatedEntityType(D365AlternateKeyTestData.DataSource, D365AlternateKeyTestData.DataverseAccountType)]
public sealed class D365UntrackedAccount : DataHubEntity
{
    public D365UntrackedAccount()
    {
        entityType = nameof(D365UntrackedAccount);
    }

    public string Name { get; set; }
}
