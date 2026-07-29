using System.Reflection;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Reimaginate.DataHub.Agent.Dataverse.Helpers;

public class DataverseOptionSetValueResolver : DefaultContractResolver
{
    private readonly List<string> _includeProps = new List<string>() { nameof(OptionSetValue.Value) };


    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, MemberSerialization.OptOut);
        if (!(_includeProps?.Select(s => s.ToLower()).Contains(prop.PropertyName.ToLower()) ?? false))
        {
            prop.ShouldSerialize = o => false;
        }

        return prop;
    }
}