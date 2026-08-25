using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using NSubstitute;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncSpecificDataHubEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;
using Reimaginate.DataHub.Agent.TestFramework.Dataverse;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.Test.Framework;
using Xunit;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Framework;

public sealed class DataverseAgentSyncExpectationTests
{
    [Fact(DisplayName = "Dataverse test agent dispatches an expected-no-result sync without changing scenario state")]
    [Trait("Category", "Unit")]
    public async Task DispatchesExpectedNoResultSyncAndPreservesScenarioState()
    {
        var mediator = Substitute.For<IMediator>();
        SyncSpecificDataHubEntitiesRequest<TestDataHubEntity, TestDataverseEntity> dispatchedRequest = null!;
        mediator.TrySend<ProcessSyncResponse>(
                Arg.Do<IRequest<ProcessSyncResponse>>(request =>
                    dispatchedRequest = Assert.IsType<SyncSpecificDataHubEntitiesRequest<TestDataHubEntity, TestDataverseEntity>>(request)),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(new ProcessSyncResponse()));

        using var agentServices = new ServiceCollection()
            .AddSingleton(mediator)
            .BuildServiceProvider();
        var agent = new DataverseAgent { AgentServices = agentServices };
        using var hostServices = new ServiceCollection()
            .AddSingleton(agent)
            .BuildServiceProvider();
        var scenario = new ScenarioBuilder(TimeSpan.Zero) { HostServices = hostServices };
        var source = new TestDataHubEntity { id = "entity-1" };

        scenario.UsingAgent<DataverseAgent>()
            .SyncRecordExpectingNoResult<TestDataHubEntity, TestDataverseEntity>(source);

        var result = await scenario.Scenario.Run("current");

        Assert.Equal("current", result.CurrentObject);
        Assert.NotNull(dispatchedRequest);
        Assert.Equal(["entity-1"], dispatchedRequest.EntityIds);
    }

    [Fact(DisplayName = "Dataverse test agent accepts an empty sync response when no result is expected")]
    [Trait("Category", "Unit")]
    public void AcceptsEmptySyncResponse()
    {
        DataverseAgent.EnsureNoSyncResult(new ProcessSyncResponse(), "entity-1");
    }

    [Fact(DisplayName = "Dataverse test agent rejects a successful sync result when no result is expected")]
    [Trait("Category", "Unit")]
    public void RejectsSuccessfulSyncResponse()
    {
        var response = new ProcessSyncResponse
        {
            Results =
            [
                new SyncEntityResult
                {
                    DataHubEntityId = "entity-1",
                    SyncOutcome = SyncOutcomes.NewSourceEntityCreated
                }
            ]
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => DataverseAgent.EnsureNoSyncResult(response, "entity-1"));

        Assert.Equal(
            "Expected Dataverse sync for DataHub entity 'entity-1' to return no result, but received 1.",
            exception.Message);
    }

    [Fact(DisplayName = "Dataverse test agent surfaces sync failures when no result is expected")]
    [Trait("Category", "Unit")]
    public void SurfacesSyncFailure()
    {
        var response = new ProcessSyncResponse
        {
            Results =
            [
                new SyncEntityResult
                {
                    DataHubEntityId = "entity-1",
                    SyncOutcome = SyncOutcomes.SyncFailed,
                    FailureReason = "Dataverse unavailable."
                }
            ]
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => DataverseAgent.EnsureNoSyncResult(response, "entity-1"));

        Assert.Equal("Dataverse unavailable.", exception.Message);
    }

    private static Task<(TResponse, Exception)> TrySuccess<TResponse>(TResponse response) =>
        Task.FromResult<(TResponse, Exception)>((response, null!));

    private sealed class TestDataHubEntity : DataHubEntity;

    private sealed class TestDataverseEntity : Entity;
}
