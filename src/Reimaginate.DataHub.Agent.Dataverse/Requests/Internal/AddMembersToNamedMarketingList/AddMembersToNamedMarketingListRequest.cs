using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.AddMembersToNamedMarketingList;

public class AddMembersToNamedMarketingListRequest : IRequest<AddMembersToNamedMarketingListResponse>
{
    public string MarketingListName { get; set; }
    public List<Guid> MemberIds { get; set; }
}