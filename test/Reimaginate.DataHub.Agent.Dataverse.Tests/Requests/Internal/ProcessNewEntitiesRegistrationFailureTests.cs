using Microsoft.Extensions.Options;
using NSubstitute;
using Reimaginate.DataHub;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.CreateDataverseRecords;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.EnsureReferencedEntitiesAreSyncd;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessNewEntities;
using Reimaginate.DataHub.Agent.Dataverse.Services.DataHubEntityCache;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using Xunit;
using DataverseAccount = DataverseModel.Account;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Requests.Internal;

public sealed class ProcessNewEntitiesRegistrationFailureTests
{
    [Fact(DisplayName = "Alternate-key registration failure is applied to the corresponding created entity")]
    [Trait("Category", "Unit")]
    public async Task RegistrationFailureIsAppliedToCorrespondingCreatedEntity()
    {
        var fixture = CreateFixture(
        [
            new RegisterAlternateKeyResponse { Success = true },
            new RegisterAlternateKeyResponse { Success = false, FailureReason = "second registration failed" }
        ],
        [
            new RegisterAlternateKeyResponse { Success = false, FailureReason = "second registration failed" }
        ],
        [
            new RegisterAlternateKeyResponse { Success = false, FailureReason = "second registration failed" }
        ]);

        var response = await fixture.Execute();

        Assert.Collection(fixture.RegistrationRequests[0].Requests,
            request => Assert.Equal(fixture.FirstEntity.id, request.DataHubEntityId),
            request => Assert.Equal(fixture.SecondEntity.id, request.DataHubEntityId));
        Assert.All(
            fixture.RegistrationRequests.Skip(1),
            request => Assert.Equal(fixture.SecondEntity.id, Assert.Single(request.Requests).DataHubEntityId));
        var firstResult = response.SyncResults.Single(result => result.DataHubEntityId == fixture.FirstEntity.id);
        var secondResult = response.SyncResults.Single(result => result.DataHubEntityId == fixture.SecondEntity.id);
        Assert.False(SyncOutcomes.IsFailure(firstResult.SyncOutcome));
        Assert.Equal(SyncOutcomes.SyncFailed, secondResult.SyncOutcome);
        Assert.Contains("second registration failed", secondResult.FailureReason);
    }

    [Fact(DisplayName = "Entity whose alternate-key registration failed is not written to source tracking")]
    [Trait("Category", "Unit")]
    public async Task RegistrationFailurePreventsSourceTrackingUpdate()
    {
        var fixture = CreateFixture(
        [
            new RegisterAlternateKeyResponse { Success = true },
            new RegisterAlternateKeyResponse { Success = false, FailureReason = "second registration failed" }
        ],
        [
            new RegisterAlternateKeyResponse { Success = false, FailureReason = "second registration failed" }
        ],
        [
            new RegisterAlternateKeyResponse { Success = false, FailureReason = "second registration failed" }
        ]);

        await fixture.Execute();

        var trackingRequest = Assert.IsType<UpdateEntitiesRequest>(fixture.TrackingUpdateRequest);
        var trackedEntity = Assert.Single(trackingRequest.Requests);
        Assert.Equal(fixture.FirstSourceId.ToString(), trackedEntity.EntityId);
    }

    [Fact(DisplayName = "Missing alternate-key registration response is treated as a failure")]
    [Trait("Category", "Unit")]
    public async Task MissingRegistrationResponseFailsEntityAndPreventsTrackingUpdate()
    {
        var fixture = CreateFixture(
        [
            new RegisterAlternateKeyResponse { Success = true }
        ],
        [],
        []);

        var response = await fixture.Execute();

        var secondResult = response.SyncResults.Single(result => result.DataHubEntityId == fixture.SecondEntity.id);
        Assert.Equal(SyncOutcomes.SyncFailed, secondResult.SyncOutcome);
        Assert.Contains("register alternate key", secondResult.FailureReason, StringComparison.OrdinalIgnoreCase);
        var trackingRequest = Assert.IsType<UpdateEntitiesRequest>(fixture.TrackingUpdateRequest);
        Assert.Equal(fixture.FirstSourceId.ToString(), Assert.Single(trackingRequest.Requests).EntityId);
    }

    [Fact(DisplayName = "Transient alternate-key registration failure retries only the failed entity")]
    [Trait("Category", "Unit")]
    public async Task TransientRegistrationFailureRetriesOnlyFailedEntity()
    {
        var fixture = CreateFixture(
        [
            new RegisterAlternateKeyResponse { Success = true },
            new RegisterAlternateKeyResponse { Success = false, FailureReason = "transient failure" }
        ],
        [
            new RegisterAlternateKeyResponse { Success = true }
        ]);

        var response = await fixture.Execute();

        Assert.Equal(2, fixture.RegistrationRequests.Count);
        Assert.Equal(fixture.SecondEntity.id, Assert.Single(fixture.RegistrationRequests[1].Requests).DataHubEntityId);
        Assert.All(response.SyncResults, result => Assert.False(SyncOutcomes.IsFailure(result.SyncOutcome)));
        var trackingRequest = Assert.IsType<UpdateEntitiesRequest>(fixture.TrackingUpdateRequest);
        Assert.Equal(
            new[] { fixture.FirstSourceId.ToString(), fixture.SecondSourceId.ToString() }.Order().ToList(),
            trackingRequest.Requests.Select(request => request.EntityId).Order().ToList());
    }

    private static TestFixture CreateFixture(params List<RegisterAlternateKeyResponse>[] registrationResponsesByAttempt)
    {
        var firstEntity = new D365TrackedAccount { id = Guid.NewGuid().ToString(), Name = "First" };
        var secondEntity = new D365TrackedAccount { id = Guid.NewGuid().ToString(), Name = "Second" };
        var firstSourceId = Guid.NewGuid();
        var secondSourceId = Guid.NewGuid();
        var firstMappedEntity = new DataverseAccount { Id = firstSourceId, Name = firstEntity.Name };
        var secondMappedEntity = new DataverseAccount { Id = secondSourceId, Name = secondEntity.Name };
        var mappedEntities = new Dictionary<string, DataverseAccount>
        {
            [firstEntity.id] = firstMappedEntity,
            [secondEntity.id] = secondMappedEntity
        };
        var dataHubClient = Substitute.For<IDataHubClient>();
        var mapper = Substitute.For<IMapper>();
        var mediator = Substitute.For<IMediator>();
        var fixture = new TestFixture(firstEntity, secondEntity, firstSourceId, secondSourceId);
        var registrationAttempt = 0;

        mapper.MapAsync<DataverseAccount>(
                Arg.Any<D365TrackedAccount>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Dictionary<string, object>>())
            .Returns(call => mappedEntities[call.ArgAt<D365TrackedAccount>(0).id]);
        mediator.TrySend<EnsureReferencedEntitiesAreSyncdResponse<D365TrackedAccount, DataverseAccount>>(
                Arg.Is<IRequest<EnsureReferencedEntitiesAreSyncdResponse<D365TrackedAccount, DataverseAccount>>>(request => request is EnsureReferencedEntitiesAreSyncdRequest<D365TrackedAccount, DataverseAccount>),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(new EnsureReferencedEntitiesAreSyncdResponse<D365TrackedAccount, DataverseAccount>
            {
                CachedEntities = [],
                Failures = [],
                ResolutionPromises = []
            }));
        mediator.TrySend<CreateDataverseRecordsResponse<DataverseAccount>>(
                Arg.Is<IRequest<CreateDataverseRecordsResponse<DataverseAccount>>>(request => request is CreateDataverseRecordsCommand<DataverseAccount>),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(new CreateDataverseRecordsResponse<DataverseAccount>
            {
                Results = new Dictionary<string, CreateResult<DataverseAccount>>
                {
                    [firstEntity.id] = new()
                    {
                        EntityId = firstSourceId,
                        ResultingEntity = firstMappedEntity,
                        Success = true
                    },
                    [secondEntity.id] = new()
                    {
                        EntityId = secondSourceId,
                        ResultingEntity = secondMappedEntity,
                        Success = true
                    }
                }
            }));
        dataHubClient.PostRequestAsync<RegisterAlternateKeysRequest, RegisterAlternateKeysResponse>(
                Arg.Do<RegisterAlternateKeysRequest>(request => fixture.RegistrationRequests.Add(request)),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var responses = registrationResponsesByAttempt[
                    Math.Min(registrationAttempt, registrationResponsesByAttempt.Length - 1)];
                registrationAttempt++;
                return new RegisterAlternateKeysResponse
                {
                    Success = responses.All(response => response.Success),
                    Responses = responses
                };
            });
        dataHubClient.PostRequestAsync<UpdateEntitiesRequest, UpdateEntitiesResponse>(
                Arg.Do<UpdateEntitiesRequest>(request => fixture.TrackingUpdateRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new UpdateEntitiesResponse());
        fixture.Handler = new ProcessNewEntitiesRequestHandler<D365TrackedAccount, DataverseAccount>(
            Options.Create(new DataverseAgentOptions
            {
                DataSource = D365AlternateKeyTestData.DataSource,
                AlternateKeyRegistrationRetryDelay = TimeSpan.Zero
            }),
            dataHubClient,
            Substitute.For<IDataHubEntityCache>(),
            mapper,
            mediator);

        return fixture;
    }

    private static Task<(TResponse, Exception)> TrySuccess<TResponse>(TResponse response) =>
        Task.FromResult((response, (Exception)null));

    private sealed class TestFixture(
        D365TrackedAccount firstEntity,
        D365TrackedAccount secondEntity,
        Guid firstSourceId,
        Guid secondSourceId)
    {
        public D365TrackedAccount FirstEntity { get; } = firstEntity;
        public D365TrackedAccount SecondEntity { get; } = secondEntity;
        public Guid FirstSourceId { get; } = firstSourceId;
        public Guid SecondSourceId { get; } = secondSourceId;
        public ProcessNewEntitiesRequestHandler<D365TrackedAccount, DataverseAccount> Handler { get; set; }
        public List<RegisterAlternateKeysRequest> RegistrationRequests { get; } = [];
        public UpdateEntitiesRequest TrackingUpdateRequest { get; set; }

        public Task<ProcessNewEntitiesResponse> Execute() => Handler.HandleAsync(
            new ProcessNewEntitiesRequest<D365TrackedAccount, DataverseAccount>
            {
                CorrelationId = "registration-failure",
                EntitiesToCreate = [FirstEntity, SecondEntity]
            },
            CancellationToken.None);
    }
}
