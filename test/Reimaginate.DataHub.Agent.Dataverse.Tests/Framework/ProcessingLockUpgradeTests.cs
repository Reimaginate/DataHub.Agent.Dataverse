using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeUpdatedDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncUpdatedDataHubEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.GetDataverseAgentMergeMarker;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.GetDataverseAgentSyncMarker;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SyncEntities;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using Reimaginate.ProcessingLockService;
using Xunit;
using Account = DataverseModel.Account;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Framework;

public sealed class ProcessingLockUpgradeTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task AgentRegistrationSupportsAcquireRenewReleaseAndContention()
    {
        using var provider = BuildAgentServices();
        var locks = provider.GetRequiredService<IProcessingLockService>();
        var acquired = await locks.WaitForLockAsync("upgrade-test", "first-owner", duration: TimeSpan.FromMinutes(1));
        Assert.True(acquired.Success);
        Assert.False((await locks.GetLockAsync("upgrade-test", "second-owner")).Success);
        Assert.True((await locks.RenewLockAsync(acquired.Result)).Success);
        Assert.True((await locks.ReleaseLockAsync(acquired.Result)).Success);
        Assert.True((await locks.GetLockAsync("upgrade-test", "second-owner")).Success);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("sync-entities")]
    [InlineData("merge-entities")]
    [InlineData("sync-updated")]
    [InlineData("merge-updated")]
    public async Task CancelledHandlersReleaseTheirLocks(string operation)
    {
        using var provider = BuildAgentServices();
        var locks = provider.GetRequiredService<IProcessingLockService>();
        var options = Options.Create(new DataverseAgentOptions { DataSource = "D365" });
        var mediator = Substitute.For<IMediator>();
        var time = Substitute.For<ITimeService>();
        var client = Substitute.For<IDataHubClient>();
        using var cancellation = new CancellationTokenSource();
        var id = Guid.NewGuid();
        Func<Task> execute;
        string lockKey;

        void CancelWork()
        {
            cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        switch (operation)
        {
            case "sync-entities":
                client.PostRequestAsync<GetDataHubEntitiesByIdRequest, GetDataHubEntitiesByIdResponse>(
                        Arg.Any<GetDataHubEntitiesByIdRequest>(), Arg.Any<CancellationToken>())
                    .Returns(_ => { CancelWork(); return Task.FromResult(new GetDataHubEntitiesByIdResponse()); });
                execute = () => new SyncEntitiesRequestHandler<CalendarAccount, Account>(options, client, mediator,
                        Substitute.For<IMapper>(), locks, provider)
                    .HandleAsync(new SyncEntitiesRequest<CalendarAccount, Account>
                    { EntityIds = [id.ToString()], CorrelationId = "first-owner" }, cancellation.Token);
                lockKey = $"entities/D365/{nameof(CalendarAccount)}/{id}";
                break;
            case "merge-entities":
                mediator.TrySend<GetSpecificDataverseEntitiesResponse<Account>>(Arg.Any<IRequest<GetSpecificDataverseEntitiesResponse<Account>>>(),
                        Arg.Any<CancellationToken>(), Arg.Any<Action<Exception>>())
                    .Returns(_ => { CancelWork(); return Task.FromResult((new GetSpecificDataverseEntitiesResponse<Account>(), (Exception)null)); });
                execute = () => new MergeEntitiesRequestHandler<Account, CalendarAccount>(options, mediator, locks, provider, time)
                    .HandleAsync(new MergeEntitiesRequest<Account, CalendarAccount>
                    { DataverseEntityIds = [id], CorrelationId = "first-owner" }, cancellation.Token);
                lockKey = $"entities/DataHub/{DataSources.DataHub}/{id}";
                break;
            case "sync-updated":
                mediator.TrySend<GetDataverseAgentSyncMarkerResponse>(Arg.Any<IRequest<GetDataverseAgentSyncMarkerResponse>>(),
                        Arg.Any<CancellationToken>(), Arg.Any<Action<Exception>>())
                    .Returns(_ => { CancelWork(); return Task.FromResult((new GetDataverseAgentSyncMarkerResponse(), (Exception)null)); });
                execute = () => new SyncUpdatedDataHubEntitiesRequestHandler<CalendarAccount, Account>(options, mediator, time, locks)
                    .HandleAsync(new SyncUpdatedDataHubEntitiesRequest<CalendarAccount, Account>
                    { CorrelationId = "first-owner" }, cancellation.Token);
                lockKey = $"dataverse/sync/{nameof(CalendarAccount)}";
                break;
            default:
                mediator.TrySend<GetDataverseAgentMergeMarkerResponse>(Arg.Any<IRequest<GetDataverseAgentMergeMarkerResponse>>(),
                        Arg.Any<CancellationToken>(), Arg.Any<Action<Exception>>())
                    .Returns(_ => { CancelWork(); return Task.FromResult((new GetDataverseAgentMergeMarkerResponse(), (Exception)null)); });
                execute = () => new MergeUpdatedDataverseEntitiesRequestHandler<Account, CalendarAccount>(options,
                        Substitute.For<IDataverseDataService>(), mediator, time, locks)
                    .HandleAsync(new MergeUpdatedDataverseEntitiesRequest<Account, CalendarAccount>
                    { CorrelationId = "first-owner" }, cancellation.Token);
                lockKey = $"D365/sync/{nameof(CalendarAccount)}";
                break;
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(execute);

        var nextAttempt = await locks.GetLockAsync(lockKey, "next-owner");
        Assert.True(nextAttempt.Success, "Cancellation left the previous handler's lock held until lease expiry.");
    }

    private static ServiceProvider BuildAgentServices()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["AgentId"] = "LockUpgradeTest",
            ["ProcessingLockOptions:UseRepository"] = "inmemory"
        }).Build();
        services.AddDataverseAgent(options => options.WithAppSettingsConfig(config));
        return services.BuildServiceProvider();
    }
}
