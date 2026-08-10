using Microsoft.Extensions.Options;
using NSubstitute;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.ProcessingLockService;
using Reimaginate.ProcessingLockService.Abstractions;
using Xunit;
using DataverseAccount = DataverseModel.Account;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Requests.Internal.MergeEntities;

public sealed class MergeEntitiesRequestHandlerTests
{
    [Fact(DisplayName = "Merge entities reports only records Dataverse did not find")]
    [Trait("Category", "Unit")]
    public async Task ReportsOnlyIdsReturnedAsNotFound()
    {
        var foundId = Guid.NewGuid();
        var firstMissingId = Guid.NewGuid();
        var secondMissingId = Guid.NewGuid();
        var processingLocks = new List<ProcessingLock> { new() };
        var normalMergeResult = new MergeEntityResult
        {
            SourceEntityType = typeof(DataverseAccount).Name,
            SourceEntityId = foundId.ToString(),
            MergeOutcome = MergeOutcomes.NewEntityCreated
        };
        var mediator = Substitute.For<IMediator>();
        var processingLockService = Substitute.For<IProcessingLockService>();

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
        mediator.TrySend<GetSpecificDataverseEntitiesResponse<DataverseAccount>>(
                Arg.Any<IRequest<GetSpecificDataverseEntitiesResponse<DataverseAccount>>>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(new GetSpecificDataverseEntitiesResponse<DataverseAccount>
            {
                Success = false,
                Results = [new DataverseAccount { Id = foundId }],
                NotFound = [firstMissingId, secondMissingId]
            }));
        mediator.TrySend<ProcessMergeResponse>(
                Arg.Any<IRequest<ProcessMergeResponse>>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Action<Exception>>())
            .Returns(TrySuccess(new ProcessMergeResponse { Results = [normalMergeResult] }));

        var handler = new MergeEntitiesRequestHandler<DataverseAccount, TestDataHubEntity>(
            Options.Create(new DataverseAgentOptions { DataSource = "D365" }),
            mediator,
            processingLockService,
            Substitute.For<IServiceProvider>(),
            Substitute.For<ITimeService>());

        var response = await handler.HandleAsync(new MergeEntitiesRequest<DataverseAccount, TestDataHubEntity>
        {
            CorrelationId = "not-found-test",
            DataverseEntityIds = [foundId, firstMissingId, secondMissingId],
            DependencyTree = []
        }, CancellationToken.None);

        Assert.Contains(normalMergeResult, response.Results);
        var failures = response.Results
            .Where(result => result.MergeOutcome == MergeOutcomes.SourceEntityNotFound)
            .ToList();
        Assert.Equal(2, failures.Count);
        Assert.Equal(
            [firstMissingId.ToString(), secondMissingId.ToString()],
            failures.Select(result => result.SourceEntityId));
        Assert.DoesNotContain(failures, result => result.SourceEntityId == foundId.ToString());
        Assert.All(failures, failure =>
        {
            Assert.Equal(typeof(DataverseAccount).Name, failure.SourceEntityType);
            Assert.False(string.IsNullOrWhiteSpace(failure.FailureReason));
            Assert.Equal(
                $"{typeof(DataverseAccount).Name} '{failure.SourceEntityId}' was not found in Dataverse.",
                failure.FailureReason);
        });
    }

    private static Task<(TResponse, Exception)> TrySuccess<TResponse>(TResponse response) =>
        Task.FromResult((response, (Exception)null));

    private sealed class TestDataHubEntity : DataHubEntity;
}
