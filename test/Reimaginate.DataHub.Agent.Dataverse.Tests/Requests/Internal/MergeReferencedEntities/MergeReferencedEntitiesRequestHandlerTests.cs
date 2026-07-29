using NSubstitute;
using OneOf;
using Reimaginate.DataHub;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeReferencedEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Constants;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;
using Xunit;
using DataHubAccount = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account;
using DataverseAccount = DataverseModel.Account;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Requests.Internal.MergeReferencedEntities;

public sealed class MergeReferencedEntitiesRequestHandlerTests
{
    [Theory(DisplayName = "Merge referenced entities skips external-only owner references")]
    [Trait("Category", "Unit")]
    [InlineData(DataverseModel.SystemUser.EntityLogicalName)]
    [InlineData(DataverseModel.Team.EntityLogicalName)]
    public async Task ExternalOnlyOwnerReferencesDoNotThrowOrInvokeMerge(string ownerLogicalName)
    {
        var dataHubClient = Substitute.For<IDataHubClient>();
        var mediator = Substitute.For<IMediator>();
        dataHubClient.PostRequestAsync<ResolveEntityReferencesRequest, ResolveEntityReferencesResponse>(
                Arg.Any<ResolveEntityReferencesRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResolveEntityReferencesResponse { Results = [] });

        var handler = new MergeReferencedEntitiesRequestHandler<DataverseAccount, DataHubAccount>(
            dataHubClient,
            mediator);

        var response = await handler.HandleAsync(new MergeReferencedEntitiesRequest<DataverseAccount, DataHubAccount>
        {
            CorrelationId = "owner-reference",
            ReferencedEntities =
            [
                new ExternalEntityReference
                {
                    DataSource = TestDataSources.Dataverse,
                    SourceEntityType = ownerLogicalName,
                    EntityType = ownerLogicalName,
                    EntityId = Guid.NewGuid().ToString()
                }
            ]
        }, CancellationToken.None);

        Assert.Empty(response.ResolvedEntityReferences);
        await mediator.DidNotReceiveWithAnyArgs()
            .SendAsync(default(IRequest)!, default);
    }

    [Fact(DisplayName = "Merge referenced entities still merges references with Dataverse and DataHub types")]
    [Trait("Category", "Unit")]
    public async Task MergeableReferencesStillInvokeMerge()
    {
        var dataHubClient = Substitute.For<IDataHubClient>();
        var mediator = Substitute.For<IMediator>();
        var sourceAccountId = Guid.NewGuid();
        var dataHubAccountId = Guid.NewGuid().ToString("N");
        dataHubClient.PostRequestAsync<ResolveEntityReferencesRequest, ResolveEntityReferencesResponse>(
                Arg.Any<ResolveEntityReferencesRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResolveEntityReferencesResponse { Results = [] });

        var mergeResponse = new ProcessMergeResponse
        {
            Results =
            [
                new MergeEntityResult
                {
                    DataSource = TestDataSources.Dataverse,
                    SourceEntityType = DataverseAccount.EntityLogicalName,
                    SourceEntityId = sourceAccountId.ToString(),
                    DataHubEntityType = typeof(DataHubAccount).Name,
                    DataHubEntityId = dataHubAccountId,
                    MergeOutcome = MergeOutcomes.NewEntityCreated
                }
            ]
        };

        mediator.SendAsync(Arg.Any<IRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(OneOf<object, Exception>.FromT0(mergeResponse)));

        var handler = new MergeReferencedEntitiesRequestHandler<DataverseAccount, DataHubAccount>(
            dataHubClient,
            mediator);

        var response = await handler.HandleAsync(new MergeReferencedEntitiesRequest<DataverseAccount, DataHubAccount>
        {
            CorrelationId = "mergeable-reference",
            ReferencedEntities =
            [
                new ExternalEntityReference
                {
                    DataSource = TestDataSources.Dataverse,
                    SourceEntityType = DataverseAccount.EntityLogicalName,
                    EntityType = typeof(DataHubAccount).Name,
                    EntityId = sourceAccountId.ToString()
                }
            ]
        }, CancellationToken.None);

        await mediator.Received(1).SendAsync(Arg.Any<IRequest>(), Arg.Any<CancellationToken>());
        var resolved = Assert.Single(response.ResolvedEntityReferences);
        Assert.Equal(dataHubAccountId, resolved.DataHubEntityReference.EntityId);
        Assert.Equal(typeof(DataHubAccount).Name, resolved.DataHubEntityReference.EntityType);
        Assert.Equal(sourceAccountId.ToString(), resolved.SourceEntityReference.EntityId);
        Assert.Equal(DataverseAccount.EntityLogicalName, resolved.SourceEntityReference.SourceEntityType);
    }

    [Theory(DisplayName = "Merge referenced entities merges Dataverse owners when DataHub owner models exist")]
    [Trait("Category", "Unit")]
    [InlineData(DataverseModel.SystemUser.EntityLogicalName, "SystemUser")]
    [InlineData(DataverseModel.Team.EntityLogicalName, "Team")]
    public async Task OwnerReferencesWithDataHubModelsInvokeMerge(string ownerLogicalName, string dataHubEntityType)
    {
        var dataHubClient = Substitute.For<IDataHubClient>();
        var mediator = Substitute.For<IMediator>();
        var sourceOwnerId = Guid.NewGuid();
        var dataHubOwnerId = Guid.NewGuid().ToString("N");
        dataHubClient.PostRequestAsync<ResolveEntityReferencesRequest, ResolveEntityReferencesResponse>(
                Arg.Any<ResolveEntityReferencesRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResolveEntityReferencesResponse { Results = [] });

        var mergeResponse = new ProcessMergeResponse
        {
            Results =
            [
                new MergeEntityResult
                {
                    DataSource = TestDataSources.Dataverse,
                    SourceEntityType = ownerLogicalName,
                    SourceEntityId = sourceOwnerId.ToString(),
                    DataHubEntityType = dataHubEntityType,
                    DataHubEntityId = dataHubOwnerId,
                    MergeOutcome = MergeOutcomes.NewEntityCreated
                }
            ]
        };

        mediator.SendAsync(Arg.Any<IRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(OneOf<object, Exception>.FromT0(mergeResponse)));

        var handler = new MergeReferencedEntitiesRequestHandler<DataverseAccount, DataHubAccount>(
            dataHubClient,
            mediator);

        var response = await handler.HandleAsync(new MergeReferencedEntitiesRequest<DataverseAccount, DataHubAccount>
        {
            CorrelationId = "owner-datahub-reference",
            ReferencedEntities =
            [
                new ExternalEntityReference
                {
                    DataSource = TestDataSources.Dataverse,
                    SourceEntityType = ownerLogicalName,
                    EntityType = dataHubEntityType,
                    EntityId = sourceOwnerId.ToString()
                }
            ]
        }, CancellationToken.None);

        await mediator.Received(1).SendAsync(Arg.Any<IRequest>(), Arg.Any<CancellationToken>());
        var resolved = Assert.Single(response.ResolvedEntityReferences);
        Assert.Equal(dataHubOwnerId, resolved.DataHubEntityReference.EntityId);
        Assert.Equal(dataHubEntityType, resolved.DataHubEntityReference.EntityType);
        Assert.Equal(sourceOwnerId.ToString(), resolved.SourceEntityReference.EntityId);
        Assert.Equal(ownerLogicalName, resolved.SourceEntityReference.SourceEntityType);
    }
}
