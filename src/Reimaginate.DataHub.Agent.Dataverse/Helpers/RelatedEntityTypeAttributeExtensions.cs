using Reimaginate.DataHub.SharedModels.Attributes;

namespace Reimaginate.DataHub.Agent.Dataverse.Helpers;

public static class RelatedEntityTypeAttributeExtensions
{
    public static List<string> GetColumnSet(this RelatedEntityTypeAttribute att)
    {
        if (att == null) return null;

        var ret = new List<string>();
        if (att.MappedPropertiesIn != null)
        {
            ret.AddRange(att.MappedPropertiesIn.Select(s => s.Trim()));
        }

        if (att.MappedPropertiesOut != null)
        {
            ret.AddRange(att.MappedPropertiesOut.Select(s => s.Trim()));
        }

        ret = ret.Distinct().ToList();
        return !ret.Any() ? null : ret;
    }

    public static List<string> GetMappedPropertiesIn(this RelatedEntityTypeAttribute att)
    {
        return att.MappedPropertiesIn?.Select(s => s.Trim()).Distinct().ToList();
    }

    public static List<string> GetMappedPropertiesOut(this RelatedEntityTypeAttribute att)
    {
        return att?.MappedPropertiesOut?.Select(s => s.Trim()).Distinct().ToList();
    }
}