using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetMarketingListByName;

public class GetMarketingListByNameRequestHandler(IDataverseDataService idataverseDataService) : IHandler<GetMarketingListByNameRequest, List<Entity>>
{
    public async Task<List<Entity>> HandleAsync(GetMarketingListByNameRequest request, CancellationToken cancellationToken)
    {
        var filterExpression = new FilterExpression
        {
            FilterOperator = LogicalOperator.And,
            Conditions =
            {
                new ConditionExpression("listname", ConditionOperator.Equal, request.ListName)
            }
        };

        var response = await idataverseDataService.WhereAsync("list", filterExpression, new ColumnSet(true), cancellationToken);
        return response;
    }
}