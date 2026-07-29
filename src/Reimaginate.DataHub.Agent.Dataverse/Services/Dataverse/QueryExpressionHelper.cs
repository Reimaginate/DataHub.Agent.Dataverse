using Microsoft.Xrm.Sdk.Query;

namespace Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;

public static class QueryExpressionHelper
{
    public static QueryExpression GetQueryExpression(string entityName, ColumnSet columnSet, params ConditionExpression[] conditionExpressions)
    {
        var filter = new FilterExpression();

        if (conditionExpressions.Any())
            filter.Conditions.AddRange(conditionExpressions);

        return GetQueryExpression(entityName, columnSet, filter);
    }
    public static QueryExpression GetQueryExpression(string entityName, ColumnSet columnSet, FilterExpression filter)
    {
        var result = new QueryExpression()
        {
            EntityName = entityName,
            ColumnSet = columnSet,
            Criteria = filter
        };

        return result;
    }
}