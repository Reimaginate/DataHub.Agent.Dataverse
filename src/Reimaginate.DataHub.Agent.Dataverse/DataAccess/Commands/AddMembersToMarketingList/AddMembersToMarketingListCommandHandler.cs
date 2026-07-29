using Microsoft.Crm.Sdk.Messages;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.AddMembersToMarketingList;

public class AddMembersToMarketingListCommandHandler(IDataverseDataService idataverseDataService) : IHandler<AddMembersToMarketingListCommand, AddListMembersListResponse>
{
    public async Task<AddListMembersListResponse> HandleAsync(AddMembersToMarketingListCommand command, CancellationToken cancellationToken)
    {
        var addMemberRequest = new AddListMembersListRequest
        {
            ListId = command.MarketingListId,
            MemberIds = command.MemberIds.ToArray()
        };

        var addMemberResponse = await idataverseDataService.ExecuteAsync<AddListMembersListRequest, AddListMembersListResponse>(addMemberRequest, cancellationToken);
        return addMemberResponse;
    }
}