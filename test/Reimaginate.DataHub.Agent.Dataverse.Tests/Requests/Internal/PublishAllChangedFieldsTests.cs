using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Reimaginate.DataHub;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecords;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.EnsureReferencedEntitiesAreSyncd;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUpdatedEntities;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using Xunit;
using Account = DataverseModel.Account;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Requests.Internal;

public sealed class PublishAllChangedFieldsTests
{
    [Theory(DisplayName = "Publishing an account preserves every requested date and name change")]
    [Trait("Category", "Unit")]
    [InlineData("replace")]
    [InlineData("clear")]
    [InlineData("add")]
    [InlineData("unchanged")]
    public async Task PublishIncludesEveryChangedField(string operation)
    {
        // Repeated real handler executions guard the former lost-List.Add race.
        // The offline package probe independently reproduces it at the comparison boundary.
        var attempts = operation == "replace" ? 500 : 1;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var sourceId = Guid.NewGuid();
            var initial = operation == "add" ? (DateTime?)null : new DateTime(2000, 2, 28, 14, 0, 0, DateTimeKind.Utc);
            var requested = operation == "clear" ? null : operation == "unchanged" ? initial : new DateTime(2024, 2, 28, 14, 0, 0, DateTimeKind.Utc);
            var oldName = "Original account";
            var newName = operation == "unchanged" ? oldName : "Renamed account";
            var original = Map(initial, oldName, sourceId);
            original.RowVersion = "123";
            var entity = new CalendarAccount
            {
                id = Guid.NewGuid().ToString(),
                alternateKeys = [new AlternateKey("d365.account", sourceId.ToString())],
                Date = requested,
                Name = newName
            };
            var client = Substitute.For<IDataHubClient>();
            var mapper = Substitute.For<IMapper>();
            var mediator = Substitute.For<IMediator>();
            var emitted = new List<UpdateDataverseRecordsCommand<Account>>();
            client.PostRequestAsync<GetTrackedEntitiesRequest, GetTrackedEntitiesResponse>(
                    Arg.Any<GetTrackedEntitiesRequest>(), Arg.Any<CancellationToken>())
                .Returns(new GetTrackedEntitiesResponse
                {
                    Results = [new() { Success = true, EntityId = sourceId.ToString(), Data = JObject.FromObject(new CalendarAccount { id = sourceId.ToString(), Date = initial, Name = oldName }) }]
                });
            mediator.TrySend<GetSpecificDataverseEntitiesResponse<Account>>(
                    Arg.Any<IRequest<GetSpecificDataverseEntitiesResponse<Account>>>(), Arg.Any<CancellationToken>(), Arg.Any<Action<Exception>>())
                .Returns(Success(new GetSpecificDataverseEntitiesResponse<Account> { Success = true, Results = [original] }));
            mediator.TrySend<EnsureReferencedEntitiesAreSyncdResponse<CalendarAccount, Account>>(
                    Arg.Any<IRequest<EnsureReferencedEntitiesAreSyncdResponse<CalendarAccount, Account>>>(), Arg.Any<CancellationToken>(), Arg.Any<Action<Exception>>())
                .Returns(Success(new EnsureReferencedEntitiesAreSyncdResponse<CalendarAccount, Account> { CachedEntities = [], Failures = [], ResolutionPromises = [] }));
            mapper.MapAsync<CalendarAccount, Account>(Arg.Any<CalendarAccount>(), Arg.Any<CancellationToken>(), Arg.Any<Dictionary<string, object>>())
                .Returns(call => Map(call.Arg<CalendarAccount>().Date, call.Arg<CalendarAccount>().Name, sourceId));
            // Mapping is a substituted boundary, not the comparison/update operation under test.
            mapper.MapAsync<CalendarAccount>(Arg.Any<object>(), Arg.Any<CancellationToken>(), Arg.Any<Dictionary<string, object>>())
                .Returns(call => new CalendarAccount
                {
                    Date = ((Account)call.ArgAt<object>(0)).GetAttributeValue<DateTime?>("new_start"),
                    Name = ((Account)call.ArgAt<object>(0)).Name
                });
            client.PostRequestAsync<UpdateEntitiesRequest, UpdateEntitiesResponse>(Arg.Any<UpdateEntitiesRequest>(), Arg.Any<CancellationToken>())
                .Returns(new UpdateEntitiesResponse());
            mediator.TrySend<UpdateDataverseRecordsResponse<Account>>(
                    Arg.Any<IRequest<UpdateDataverseRecordsResponse<Account>>>(), Arg.Any<CancellationToken>(), Arg.Any<Action<Exception>>())
                .Returns(call =>
                {
                    var command = (UpdateDataverseRecordsCommand<Account>)call.Arg<IRequest<UpdateDataverseRecordsResponse<Account>>>();
                    emitted.Add(command);
                    return Success(new UpdateDataverseRecordsResponse<Account>
                    {
                        Results = new() { [sourceId.ToString()] = new() { Success = true, EntityId = sourceId, ResultingEntity = new Account { Id = sourceId, ["modifiedon"] = DateTime.UtcNow } } }
                    });
                });
            var handler = new ProcessUpdatedEntitiesRequestHandler<CalendarAccount, Account>(
                Options.Create(new DataverseAgentOptions { DataSource = "D365" }), client, mapper, mediator, Substitute.For<ITimeService>());

            var response = await handler.HandleAsync(new ProcessUpdatedEntitiesRequest<CalendarAccount, Account> { EntitiesToUpdate = [entity] }, CancellationToken.None);

            var result = Assert.Single(response.SyncResults);
            if (operation == "unchanged")
            {
                Assert.Empty(emitted);
                Assert.Equal(SyncOutcomes.NoSourceEntityUpdateToProcess, result.SyncOutcome);
                continue;
            }
            Assert.Equal(SyncOutcomes.SourceEntityUpdated, result.SyncOutcome);
            var update = Assert.Single(Assert.Single(emitted).Records).Value;
            Assert.Equal(sourceId, update.Id);
            Assert.Equal("123", update.RowVersion);
            foreach (var field in new[] { "new_start", "new_end" })
            {
                Assert.True(update.Contains(field), $"{operation}, attempt {attempt}: outbound update omitted {field}.");
                Assert.Equal(requested, update.GetAttributeValue<DateTime?>(field));
            }
            Assert.Equal(newName, update.Name);
            // The early-bound SDK also stores Id in the accountid attribute.
            Assert.Equal(new[] { "accountid", "name", "new_end", "new_start" }, update.Attributes.Keys.Order().ToArray());
        }
    }

    private static Account Map(DateTime? date, string name, Guid id) => new()
    {
        Id = id, Name = name, ["new_start"] = date, ["new_end"] = date
    };

    private static Task<(T, Exception)> Success<T>(T response) => Task.FromResult((response, (Exception)null));
}
