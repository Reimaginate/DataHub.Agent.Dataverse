using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecords;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.EnsureReferencedEntitiesAreSyncd;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUpdatedEntities;
using Reimaginate.DataHub.Agent.Dataverse.Services.DataHubEntityCache;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using Xunit;
using Contact = DataverseModel.Contact;
using TestMapper = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapper;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Requests.Internal;

public sealed class ReconciledReferenceTests
{
    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("introduced", true)]
    [InlineData("reconciled-change", true)]
    [InlineData("reconciled-clear", true)]
    [InlineData("clear", true)]
    [InlineData("clear", false)]
    [InlineData("change", true)]
    [InlineData("change", false)]
    [InlineData("keep", true)]
    [InlineData("keep", false)]
    [InlineData("unset", true)]
    [InlineData("unset", false)]
    [InlineData("external", true)]
    [InlineData("external", false)]
    [InlineData("team", true)]
    [InlineData("team", false)]
    public async Task RefreshPreservesOwnerAndBusinessUpdateSemantics(string operation, bool reconcile)
    {
        using var scenario = new Scenario();
        var item = await scenario.AddContact(operation, reconcile);

        var response = await scenario.Run();

        Assert.Equal(SyncOutcomes.SourceEntityUpdated, Assert.Single(response.SyncResults).SyncOutcome);
        var update = Assert.Single(scenario.Updates).Value;
        Assert.True(update.DoNotBulkEmail);
        Assert.Equal("123", update.RowVersion);
        if (operation == "clear")
        {
            Assert.True(update.Contains("ownerid"));
            Assert.Null(update.OwnerId);
            Assert.Null(item.Reloaded.Owner);
        }
        else if (operation == "change")
        {
            Assert.Equal(scenario.SecondOwnerId, update.OwnerId.Id);
        }
        else
        {
            // The existing D365 owner must not be reassigned or cleared for a business-only update.
            Assert.False(update.Contains("ownerid"));
        }
        if (operation == "introduced")
        {
            Assert.Equal(scenario.FirstUser.id, item.Reloaded.Owner.EntityId);
            Assert.Equal(scenario.FirstOwnerId, item.Current.OwnerId.Id);
        }
        var resultingContact = Assert.Single(response.SyncResults).ResultingDataHubEntity.ToObject<D365OwnerContact>();
        if (operation is "clear" or "unset" or "reconciled-clear") Assert.Null(resultingContact.Owner);
        if (operation == "reconciled-change")
        {
            Assert.Equal(scenario.SecondOwnerId, item.Current.OwnerId.Id);
            Assert.Equal(item.Reloaded.Owner.EntityId, resultingContact.Owner.EntityId);
        }
        if (!reconcile) Assert.Single(scenario.ReferenceRequests);
        Assert.All(scenario.ReferenceRequests, request =>
        {
            Assert.Same(scenario.Request.DependencyTree, request.DependencyTree);
            Assert.Same(scenario.Request.ResolutionPromises, request.ResolutionPromises);
        });
        // An already linked User must never be provisioned again.
        await scenario.Mediator.DidNotReceiveWithAnyArgs().SendAsync(default, default);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RefreshPreservesOtherContactsAndCachedLookupsAndReplacesStaleIdentity()
    {
        using var scenario = new Scenario();
        var introduced = await scenario.AddContact("introduced", true);
        var existing = await scenario.AddContact("keep", false, useSecondOwner: true);
        var staleUser = JObject.FromObject(scenario.FirstUser);
        staleUser[nameof(DataHubEntity.alternateKeys)] = JArray.FromObject(new[] { new AlternateKey("d365.systemuser", Guid.NewGuid().ToString()) });
        var unrelatedUser = JObject.FromObject(new SystemUser { id = Guid.NewGuid().ToString() });
        var lookup = new object();
        // Insert after the first dependency load, as if supplied by another mapping stage.
        scenario.AfterInitialReferences = () =>
        {
            var users = Assert.IsType<List<JObject>>(scenario.Request.Cache[nameof(SystemUser)]);
            users.Add(staleUser);
            users.Add(unrelatedUser);
            scenario.Request.Cache["customLookup"] = lookup;
        };

        var response = await scenario.Run();

        Assert.All(response.SyncResults, result => Assert.Equal(SyncOutcomes.SourceEntityUpdated, result.SyncOutcome));
        Assert.Equal(2, scenario.Updates.Count);
        Assert.All(scenario.Updates.Values, update =>
        {
            Assert.True(update.DoNotBulkEmail);
            Assert.False(update.Contains("ownerid"));
        });
        var usersAfter = Assert.IsType<List<JObject>>(scenario.Request.Cache[nameof(SystemUser)]);
        Assert.Equal(3, usersAfter.Count);
        Assert.Contains(unrelatedUser, usersAfter);
        Assert.Equal(scenario.FirstOwnerId.ToString(), usersAfter.Single(u => u.Value<string>("id") == scenario.FirstUser.id)
            ["alternateKeys"][0]["Value"].Value<string>());
        Assert.Same(lookup, scenario.Request.Cache["customLookup"]);
        Assert.Equal(new[] { introduced.Initial.id, existing.Initial.id }.Order(),
            scenario.ReferenceRequests.Last().Entities.Select(e => e.id).Order());
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnresolvedIntroducedOwnerFailsOnlyItsContact(bool explicitDependencyFailure)
    {
        using var scenario = new Scenario();
        var failed = await scenario.AddContact("introduced", true);
        var valid = await scenario.AddContact("keep", false, useSecondOwner: true);
        scenario.Users.Remove(scenario.FirstUser.id);
        if (explicitDependencyFailure) scenario.FailRefreshedEntityId = failed.Initial.id;

        var response = await scenario.Run();

        var failure = response.SyncResults.Single(r => r.DataHubEntityId == failed.Initial.id);
        Assert.Equal(SyncOutcomes.SyncFailed, failure.SyncOutcome);
        Assert.Equal(explicitDependencyFailure ? "Dependency failed" : "D365 SystemUser Id Could not be determined", failure.FailureReason);
        Assert.Equal(SyncOutcomes.SourceEntityUpdated, response.SyncResults.Single(r => r.DataHubEntityId == valid.Initial.id).SyncOutcome);
        Assert.Equal(valid.SourceId.ToString(), Assert.Single(scenario.Updates).Key);
        Assert.Equal(scenario.FirstUser.id, failed.Reloaded.Owner.EntityId);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnchangedContactDoesNotSendAnUpdate(bool reconcile)
    {
        using var scenario = new Scenario();
        var item = await scenario.AddContact("keep", reconcile);
        item.Initial.DoNotBulkEmail = false;
        item.Reloaded.DoNotBulkEmail = false;

        var response = await scenario.Run();

        Assert.Equal(SyncOutcomes.NoSourceEntityUpdateToProcess, Assert.Single(response.SyncResults).SyncOutcome);
        Assert.Empty(scenario.Updates);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("lookup")]
    [InlineData("sync")]
    public async Task ThrownDependencyErrorFailsOnlyTheAffectedContact(string failureStage)
    {
        using var scenario = new Scenario();
        var failed = await scenario.AddContact("introduced", true);
        var valid = await scenario.AddContact("keep", false, useSecondOwner: true);
        var expectedError = new InvalidOperationException($"Owner dependency {failureStage} failed");
        if (failureStage == "lookup")
        {
            scenario.LookupError = expectedError;
        }
        else
        {
            // A newly referenced entity has no source key, so the real helper must dispatch
            // its dependency sync. Use a shared model with a registered Dataverse type.
            var dependency = new D365OwnerContact { id = Guid.NewGuid().ToString() };
            failed.Reloaded.Owner = new EntityReference { EntityType = dependency.entityType, EntityId = dependency.id };
            scenario.Users[dependency.id] = JObject.FromObject(dependency);
            scenario.Mediator.SendAsync(Arg.Any<IRequest>(), Arg.Any<CancellationToken>())
                .Returns(_ => throw expectedError);
        }

        var response = await scenario.Run();

        var failure = response.SyncResults.Single(r => r.DataHubEntityId == failed.Initial.id);
        Assert.Equal(SyncOutcomes.SyncFailed, failure.SyncOutcome);
        Assert.Equal(expectedError.Message, failure.FailureReason);
        Assert.Equal(SyncOutcomes.SourceEntityUpdated, response.SyncResults.Single(r => r.DataHubEntityId == valid.Initial.id).SyncOutcome);
        Assert.Equal(valid.SourceId.ToString(), Assert.Single(scenario.Updates).Key);
        Assert.NotNull(failed.Reloaded.Owner);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CancelledReferenceRefreshStopsBeforeOutboundUpdates()
    {
        using var scenario = new Scenario();
        await scenario.AddContact("introduced", true);
        await scenario.AddContact("keep", false, useSecondOwner: true);
        using var cancellation = new CancellationTokenSource();
        scenario.AfterInitialReferences = cancellation.Cancel;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scenario.Run(cancellation.Token));

        Assert.Empty(scenario.Updates);
    }

    private sealed record ContactCase(Guid SourceId, D365OwnerContact Initial, D365OwnerContact Tracked,
        D365OwnerContact Reloaded, Contact Current);

    private sealed class Scenario : IDisposable
    {
        private readonly IDataHubClient client = Substitute.For<IDataHubClient>();
        private readonly IDataHubEntityCache entityCache = Substitute.For<IDataHubEntityCache>();
        private readonly ServiceProvider services;
        private readonly IMapper mapper;
        private readonly List<ContactCase> contacts = [];
        private readonly IOptions<DataverseAgentOptions> options = Options.Create(new DataverseAgentOptions { DataSource = "D365" });
        public IMediator Mediator { get; } = Substitute.For<IMediator>();
        public Guid FirstOwnerId { get; } = Guid.NewGuid();
        public Guid SecondOwnerId { get; } = Guid.NewGuid();
        public SystemUser FirstUser { get; }
        private SystemUser SecondUser { get; }
        public Dictionary<string, JObject> Users { get; } = [];
        public ProcessUpdatedEntitiesRequest<D365OwnerContact, Contact> Request { get; } = new() { EntitiesToUpdate = [] };
        public List<EnsureReferencedEntitiesAreSyncdRequest<D365OwnerContact, Contact>> ReferenceRequests { get; } = [];
        public Dictionary<string, Contact> Updates { get; } = [];
        public Action AfterInitialReferences { get; set; }
        public string FailRefreshedEntityId { get; set; }
        public Exception LookupError { get; set; }

        public Scenario()
        {
            FirstUser = new SystemUser { id = Guid.NewGuid().ToString(), alternateKeys = [new AlternateKey("d365.systemuser", FirstOwnerId.ToString())] };
            SecondUser = new SystemUser { id = Guid.NewGuid().ToString(), alternateKeys = [new AlternateKey("d365.systemuser", SecondOwnerId.ToString())] };
            Users.Add(FirstUser.id, JObject.FromObject(FirstUser));
            Users.Add(SecondUser.id, JObject.FromObject(SecondUser));
            var collection = new ServiceCollection();
            collection.AddTransient<IMapper, TestMapper>();
            TestMapper.RegisterMaps(collection);
            collection.AddTransient<ITypeMapper<D365OwnerContact, Contact>, MapD365OwnerContactToDataverse>();
            collection.AddTransient<ITypeMapper<Contact, D365OwnerContact>, MapDataverseToD365OwnerContact>();
            services = collection.BuildServiceProvider();
            mapper = services.GetRequiredService<IMapper>();

            client.PostRequestAsync<GetTrackedEntitiesRequest, GetTrackedEntitiesResponse>(Arg.Any<GetTrackedEntitiesRequest>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    var response = new GetTrackedEntitiesResponse { Results = [] };
                    foreach (var contact in contacts)
                        response.Results.Add(new() { Success = true, EntityId = contact.SourceId.ToString(), Data = JObject.FromObject(contact.Tracked) });
                    return response;
                });
            Mediator.TrySend<GetSpecificDataverseEntitiesResponse<Contact>>(Arg.Any<IRequest<GetSpecificDataverseEntitiesResponse<Contact>>>(), Arg.Any<CancellationToken>(), Arg.Any<Action<Exception>>())
                .Returns(_ => Success(new GetSpecificDataverseEntitiesResponse<Contact> { Success = true, Results = contacts.Select(c => c.Current).ToList() }));
            entityCache.GetDataHubEntities(Arg.Any<string>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>(), Arg.Any<bool?>())
                .Returns(call =>
                {
                    call.Arg<CancellationToken>().ThrowIfCancellationRequested();
                    if (LookupError is not null && call.Arg<List<string>>().Contains(FirstUser.id))
                        return Task.FromException<List<JObject>>(LookupError);
                    return Task.FromResult(call.Arg<List<string>>().Where(Users.ContainsKey).Select(id => Users[id]).ToList());
                });
            var dependencyHandler = new EnsureReferencedEntitiesAreSyncdRequestHandler<D365OwnerContact, Contact>(options, entityCache, Mediator);
            Mediator.TrySend<EnsureReferencedEntitiesAreSyncdResponse<D365OwnerContact, Contact>>(Arg.Any<IRequest<EnsureReferencedEntitiesAreSyncdResponse<D365OwnerContact, Contact>>>(), Arg.Any<CancellationToken>(), Arg.Any<Action<Exception>>())
                .Returns(async call =>
                {
                    var request = (EnsureReferencedEntitiesAreSyncdRequest<D365OwnerContact, Contact>)call.Arg<IRequest<EnsureReferencedEntitiesAreSyncdResponse<D365OwnerContact, Contact>>>();
                    ReferenceRequests.Add(request);
                    var response = await dependencyHandler.HandleAsync(request, call.Arg<CancellationToken>());
                    if (ReferenceRequests.Count > 1 && FailRefreshedEntityId is not null)
                        response.Failures.Add(new ReferenceEntitySyncFailure
                        { Entity = request.Entities.Single(e => e.id == FailRefreshedEntityId), Exception = new Exception("Dependency failed") });
                    return (response, (Exception)null);
                });
            Mediator.TrySend<ProcessMergeResponse>(Arg.Any<IRequest<ProcessMergeResponse>>(), Arg.Any<CancellationToken>(), Arg.Any<Action<Exception>>())
                .Returns(call =>
                {
                    AfterInitialReferences?.Invoke();
                    var request = (MergeSpecificDataverseEntitiesRequest<Contact, D365OwnerContact>)call.Arg<IRequest<ProcessMergeResponse>>();
                    return Success(new ProcessMergeResponse
                    {
                        Results = contacts.Where(c => request.EntityIds.Contains(c.SourceId)).Select(c => new MergeEntityResult
                        { SourceEntityId = c.SourceId.ToString(), DataHubEntityId = c.Initial.id, MergeOutcome = MergeOutcomes.NewEntityCreated }).ToList()
                    });
                });
            client.PostRequestAsync<GetDataHubEntitiesByIdRequest, GetDataHubEntitiesByIdResponse>(Arg.Any<GetDataHubEntitiesByIdRequest>(), Arg.Any<CancellationToken>())
                .Returns(call => new GetDataHubEntitiesByIdResponse
                { Results = contacts.Where(c => call.Arg<GetDataHubEntitiesByIdRequest>().EntityIds.Contains(c.Initial.id)).Select(c => JObject.FromObject(c.Reloaded)).ToList() });
            Mediator.TrySend<UpdateDataverseRecordsResponse<Contact>>(Arg.Any<IRequest<UpdateDataverseRecordsResponse<Contact>>>(), Arg.Any<CancellationToken>(), Arg.Any<Action<Exception>>())
                .Returns(call =>
                {
                    var request = (UpdateDataverseRecordsCommand<Contact>)call.Arg<IRequest<UpdateDataverseRecordsResponse<Contact>>>();
                    var response = new UpdateDataverseRecordsResponse<Contact> { Results = new() };
                    foreach (var (id, contact) in request.Records)
                    {
                        Updates.Add(id, contact);
                        response.Results[id] = new() { Success = true, EntityId = contact.Id,
                            ResultingEntity = new Contact { Id = contact.Id, ["modifiedon"] = DateTime.UtcNow } };
                    }
                    return Success(response);
                });
            client.PostRequestAsync<UpdateEntitiesRequest, UpdateEntitiesResponse>(Arg.Any<UpdateEntitiesRequest>(), Arg.Any<CancellationToken>())
                .Returns(new UpdateEntitiesResponse());
        }

        public async Task<ContactCase> AddContact(string operation, bool reconcile, bool useSecondOwner = false)
        {
            var sourceId = Guid.NewGuid();
            var user = useSecondOwner ? SecondUser : FirstUser;
            EntityReference owner = new() { EntityType = nameof(SystemUser), EntityId = user.id };
            if (operation == "external") owner = new ExternalEntityReference
            { DataSource = "D365", EntityType = nameof(SystemUser), SourceEntityType = "systemuser", EntityId = FirstOwnerId.ToString() };
            if (operation == "team")
            {
                var team = new Team { id = Guid.NewGuid().ToString(), alternateKeys = [new AlternateKey("d365.team", FirstOwnerId.ToString())] };
                Users.Add(team.id, JObject.FromObject(team));
                owner = new EntityReference { EntityType = nameof(Team), EntityId = team.id };
            }
            var tracked = new D365OwnerContact { id = sourceId.ToString(), Owner = operation is "introduced" or "unset" ? null : owner,
                DoNotBulkEmail = false, Description = "Before" };
            var initial = new D365OwnerContact { id = Guid.NewGuid().ToString(), alternateKeys = [new AlternateKey("d365.contact", sourceId.ToString())],
                Owner = operation is "introduced" or "clear" or "unset" ? null : operation == "change"
                    ? new EntityReference { EntityType = nameof(SystemUser), EntityId = SecondUser.id } : owner,
                DoNotBulkEmail = true, Description = "Before" };
            var reloaded = JObject.FromObject(initial).ToObject<D365OwnerContact>();
            reloaded.Description = reconcile ? "D365 change" : "Before";
            if (operation == "introduced") reloaded.Owner = owner;
            if (operation == "reconciled-change") reloaded.Owner = new EntityReference { EntityType = nameof(SystemUser), EntityId = SecondUser.id };
            if (operation == "reconciled-clear") reloaded.Owner = null;
            var currentData = new D365OwnerContact { Owner = operation is "introduced" or "reconciled-change" or "reconciled-clear" ? reloaded.Owner : tracked.Owner,
                DoNotBulkEmail = false, Description = reloaded.Description };
            var cache = Users.Values.GroupBy(u => u.Value<string>("entityType")).ToDictionary(g => g.Key, g => (object)g.ToList());
            var current = await mapper.MapAsync<D365OwnerContact, Contact>(currentData, CancellationToken.None, cache);
            current.Id = sourceId;
            current.RowVersion = "123";
            var item = new ContactCase(sourceId, initial, tracked, reloaded, current);
            contacts.Add(item);
            Request.EntitiesToUpdate.Add(initial);
            return item;
        }

        public Task<ProcessUpdatedEntitiesResponse> Run(CancellationToken cancellationToken = default) =>
            new ProcessUpdatedEntitiesRequestHandler<D365OwnerContact, Contact>(options, client, mapper, Mediator, Substitute.For<ITimeService>())
                .HandleAsync(Request, cancellationToken);

        public void Dispose() => services.Dispose();
    }

    private static Task<(T, Exception)> Success<T>(T response) => Task.FromResult((response, (Exception)null));
}
