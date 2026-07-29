using Microsoft.Crm.Sdk.Messages;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.RemoveMemberFromMarketingList;

public class RemoveMemberFromMarketingListCommandHandler(IDataverseDataService idataverseDataService) : IHandler<RemoveMemberFromMarketingListCommand, RemoveMemberListResponse>
{
    public async Task<RemoveMemberListResponse> HandleAsync(RemoveMemberFromMarketingListCommand command, CancellationToken cancellationToken)
    {
        var removeMemberListRequest = new RemoveMemberListRequest
        {
            ListId = command.MarketingListId,
            EntityId = command.MemberId
        };

        var removeMemberListResponse = await idataverseDataService.ExecuteAsync<RemoveMemberListRequest, RemoveMemberListResponse>(removeMemberListRequest, cancellationToken);
        return removeMemberListResponse;
    }
}