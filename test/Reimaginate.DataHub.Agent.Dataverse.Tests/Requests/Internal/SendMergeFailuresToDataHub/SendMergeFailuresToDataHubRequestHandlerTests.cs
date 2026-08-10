using Microsoft.Extensions.Options;
using NSubstitute;
using Reimaginate.DataHub;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendMergeFailuresToDataHub;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;
using Xunit;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Requests.Internal.SendMergeFailuresToDataHub;

public sealed class SendMergeFailuresToDataHubRequestHandlerTests
{
    private const string MissingFailureReason = "The merge operation did not provide a failure reason.";

    [Fact(DisplayName = "Merge failure reporting preserves the supplied type and reason")]
    [Trait("Category", "Unit")]
    public async Task PreservesSuppliedFailureTypeAndReason()
    {
        const string failureReason = "The source record could not be merged.";
        var input = new MergeEntityResult
        {
            DataHubEntityType = "Account",
            DataHubEntityId = "datahub-account",
            SourceEntityType = "account",
            SourceEntityId = Guid.NewGuid().ToString(),
            MergeOutcome = MergeOutcomes.SourceEntityNotFound,
            FailureReason = failureReason
        };

        var submitted = await SubmitAsync(input);

        var failure = Assert.Single(submitted.MergeFailures);
        Assert.Equal(MergeOutcomes.SourceEntityNotFound, failure.FailureType);
        Assert.Equal(failureReason, failure.FailureReason);
        Assert.Equal(failureReason, failure.Description);
        Assert.False(string.IsNullOrWhiteSpace(failure.FailureType));
        Assert.False(string.IsNullOrWhiteSpace(failure.FailureReason));
        Assert.False(string.IsNullOrWhiteSpace(failure.Description));
    }

    [Theory(DisplayName = "Merge failure reporting replaces blank failure types")]
    [Trait("Category", "Unit")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReplacesBlankFailureType(string mergeOutcome)
    {
        const string failureReason = "The upstream merge failed.";
        var input = new MergeEntityResult
        {
            MergeOutcome = mergeOutcome,
            FailureReason = failureReason
        };

        var submitted = await SubmitAsync(input);

        var failure = Assert.Single(submitted.MergeFailures);
        Assert.Equal(MergeOutcomes.MergeFailed, failure.FailureType);
        Assert.Equal(failureReason, failure.FailureReason);
        Assert.Equal(failureReason, failure.Description);
    }

    [Theory(DisplayName = "Merge failure reporting replaces blank failure reasons")]
    [Trait("Category", "Unit")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReplacesBlankFailureReason(string failureReason)
    {
        var input = new MergeEntityResult
        {
            MergeOutcome = MergeOutcomes.SourceEntityNotFound,
            FailureReason = failureReason
        };

        var submitted = await SubmitAsync(input);

        var failure = Assert.Single(submitted.MergeFailures);
        Assert.Equal(MergeOutcomes.SourceEntityNotFound, failure.FailureType);
        Assert.Equal(MissingFailureReason, failure.FailureReason);
        Assert.Equal(MissingFailureReason, failure.Description);
        Assert.False(string.IsNullOrWhiteSpace(failure.FailureType));
        Assert.False(string.IsNullOrWhiteSpace(failure.FailureReason));
        Assert.False(string.IsNullOrWhiteSpace(failure.Description));
    }

    [Fact(DisplayName = "Merge failure reporting supplies required fields for every submitted failure")]
    [Trait("Category", "Unit")]
    public async Task SuppliesRequiredFieldsForEverySubmittedFailure()
    {
        var submitted = await SubmitAsync(
            new MergeEntityResult
            {
                MergeOutcome = null,
                FailureReason = "The first merge failed."
            },
            new MergeEntityResult
            {
                MergeOutcome = MergeOutcomes.SourceEntityNotFound,
                FailureReason = "   "
            },
            new MergeEntityResult
            {
                MergeOutcome = "",
                FailureReason = null
            });

        Assert.Equal(3, submitted.MergeFailures.Count);
        Assert.All(submitted.MergeFailures, failure =>
        {
            Assert.False(string.IsNullOrWhiteSpace(failure.FailureType));
            Assert.False(string.IsNullOrWhiteSpace(failure.FailureReason));
            Assert.False(string.IsNullOrWhiteSpace(failure.Description));
        });
    }

    private static async Task<RegisterMergeFailuresRequest> SubmitAsync(params MergeEntityResult[] inputs)
    {
        var dataHubClient = Substitute.For<IDataHubClient>();
        var timeService = Substitute.For<ITimeService>();
        RegisterMergeFailuresRequest submitted = null;

        dataHubClient.PostRequestAsync<RegisterMergeFailuresRequest, NullResponse>(
                Arg.Do<RegisterMergeFailuresRequest>(request => submitted = request),
                Arg.Any<CancellationToken>())
            .Returns(new NullResponse());
        timeService.Now().Returns(new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));

        var handler = new SendMergeFailuresToDataHubRequestHandler(
            dataHubClient,
            Options.Create(new DataverseAgentOptions
            {
                AgentId = "agent",
                DataSource = "D365"
            }),
            timeService);

        await handler.HandleAsync(new SendMergeFailuresToDataHubRequest(inputs.ToList()), CancellationToken.None);

        return Assert.IsType<RegisterMergeFailuresRequest>(submitted);
    }
}
