using Microsoft.Crm.Sdk.Messages;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.RemoveMemberFromMarketingList;

public class RemoveMemberFromMarketingListCommand : IRequest<RemoveMemberListResponse>
{
    public Guid MarketingListId { get; set; }
    public Guid MemberId { get; set; }
}