using Microsoft.Xrm.Sdk.Query;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetUpdatedDataverseEntities;

public class GetUpdatedDataverseEntitiesQueryHandler<TDataverseEntity>(IDataverseDataService idataverseDataService) : IHandler<GetUpdatedDataverseEntitiesQuery<TDataverseEntity>, List<TDataverseEntity>>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public async Task<List<TDataverseEntity>> HandleAsync(GetUpdatedDataverseEntitiesQuery<TDataverseEntity> request, CancellationToken cancellationToken)
    {
        var filterExpression = new FilterExpression()
        {
            Conditions = { new ConditionExpression("modifiedon", ConditionOperator.GreaterEqual, request.FromDateTime.DateTime) }
        };

        var ret =  await idataverseDataService.WhereAsync<TDataverseEntity>(filterExpression, new ColumnSet(true), cancellationToken);
        return ret;
    }
}