using Reimaginate.DataHub.SharedModels.Attributes;

namespace Reimaginate.DataHub.Agent.Dataverse.Helpers;

public static class TypeExtensions
{
    public static RelatedEntityTypeAttribute GetRelatedEntityTypeAttribute(this Type type, string dataSource)
    {
        return (RelatedEntityTypeAttribute)type.GetCustomAttributes(typeof(RelatedEntityTypeAttribute), true).FirstOrDefault(f => ((RelatedEntityTypeAttribute)f).DataSource == dataSource);
    }
}