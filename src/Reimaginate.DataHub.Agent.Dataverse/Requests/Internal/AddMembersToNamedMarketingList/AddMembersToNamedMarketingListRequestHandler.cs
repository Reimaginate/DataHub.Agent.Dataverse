using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetMarketingListByName;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.AddMembersToNamedMarketingList;

public class AddMembersToNamedMarketingListRequestHandler(IMediator mediator) : IHandler<AddMembersToNamedMarketingListRequest, AddMembersToNamedMarketingListResponse>
{
    public async Task<AddMembersToNamedMarketingListResponse> HandleAsync(AddMembersToNamedMarketingListRequest request, CancellationToken cancellationToken)
    {
        var getMarketingListResponse = (await mediator.TrySend<List<Entity>>(new GetMarketingListByNameRequest()
        {
            ListName = request.MarketingListName
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        if (getMarketingListResponse.Count == 0) throw new Exception($"Marketing list {request.MarketingListName} not found");
        if (getMarketingListResponse.Count > 1) throw new Exception($"Duplicate marketing lists {request.MarketingListName} found");

        var marketingList = getMarketingListResponse.First();

        var addResponse = (await mediator.TrySend<AddListMembersListResponse>(new DataAccess.Commands.AddMembersToMarketingList.AddMembersToMarketingListCommand()
        {
            MarketingListId = marketingList.Id,
            MemberIds = request.MemberIds
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        if (addResponse.Results.Count > 0)
        {
            return new AddMembersToNamedMarketingListResponse()
            {
                Success = false,
                Errors = addResponse.Results.Select(s => s.Value.ToString()).ToList()
            };
        }

        return new AddMembersToNamedMarketingListResponse()
        {
            Success = true
        };

    }
}