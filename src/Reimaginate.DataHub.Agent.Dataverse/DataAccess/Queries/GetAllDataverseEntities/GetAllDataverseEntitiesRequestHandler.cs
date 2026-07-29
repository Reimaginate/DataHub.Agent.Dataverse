using Microsoft.Xrm.Sdk.Query;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetAllDataverseEntities;

public class GetAllDataverseEntitiesRequestHandler<TDataverseEntity>(IDataverseDataService idataverseDataService) : IHandler<GetAllDataverseEntitiesRequest<TDataverseEntity>, List<TDataverseEntity>>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public async Task<List<TDataverseEntity>> HandleAsync(GetAllDataverseEntitiesRequest<TDataverseEntity> request, CancellationToken cancellationToken)
    {
        var page = 1;
        var results = new List<TDataverseEntity>();
        
        var response = await idataverseDataService.PagedWhereAsync<TDataverseEntity>(request.FilterExpression, page, 5000, new ColumnSet(true), null, null, cancellationToken);
        results.AddRange(response.Results);

        while (response.MoreResultsAvailable)
        {
            page++;
            response = await idataverseDataService.PagedWhereAsync<TDataverseEntity>(request.FilterExpression, page, 5000, new ColumnSet(true), null, response.ContinuationToken, cancellationToken);
            results.AddRange(response.Results);
        }

        return results;
    }
}