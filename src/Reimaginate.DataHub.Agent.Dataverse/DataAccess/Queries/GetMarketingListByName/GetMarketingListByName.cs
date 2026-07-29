using Microsoft.Xrm.Sdk;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetMarketingListByName;

public class GetMarketingListByNameRequest : IRequest<List<Entity>>
{
    public string ListName { get; set; }
}