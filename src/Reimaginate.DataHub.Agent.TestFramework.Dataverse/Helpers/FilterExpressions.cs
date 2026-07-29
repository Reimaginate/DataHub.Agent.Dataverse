using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Reimaginate.DataHub.Agent.TestFramework.Dataverse.Helpers;

public class FilterExpressions
{
    public static FilterExpression GetEntityByName<TDataverseEntity>(string nameValue) where TDataverseEntity : Entity
    {
        var primaryIdAttribute = typeof(TDataverseEntity).GetField("PrimaryIdAttribute")?.GetValue(typeof(TDataverseEntity))?.ToString();
        var primaryNameAttribute = typeof(TDataverseEntity).GetField("PrimaryNameAttribute")?.GetValue(typeof(TDataverseEntity))?.ToString();

        return new FilterExpression(LogicalOperator.Or)
        {
            Filters =
            {
                new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression(primaryNameAttribute, ConditionOperator.Equal, nameValue)
                    }
                }
            }
        };
    }


    public static Func<string, string, FilterExpression> Equals = (fieldName, value) => new FilterExpression(LogicalOperator.Or)
    {
        Filters =
        {
            new FilterExpression(LogicalOperator.And)
            {
                Conditions =
                {
                    new ConditionExpression(fieldName, ConditionOperator.Equal, value)
                }
            }
        }
    };


    public static Func<string, IEnumerable<string>, FilterExpression> In = (fieldName, values) =>
    {
        var condition = new ConditionExpression(fieldName, ConditionOperator.In);
        condition.Values.AddRange(values);

        return new FilterExpression(LogicalOperator.Or)
        {
            Filters =
            {
                new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        condition
                    }
                }
            }
        };
    };
}