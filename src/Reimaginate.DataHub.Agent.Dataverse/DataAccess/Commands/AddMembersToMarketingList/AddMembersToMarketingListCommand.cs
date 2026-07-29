using Microsoft.Crm.Sdk.Messages;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.AddMembersToMarketingList;

public class AddMembersToMarketingListCommand : IRequest<AddListMembersListResponse>
{
    public Guid MarketingListId { get; set; }
    public List<Guid> MemberIds { get; set; }
}