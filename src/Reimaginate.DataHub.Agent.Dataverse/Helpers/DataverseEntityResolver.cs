using System.Reflection;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Reimaginate.DataHub.Agent.Dataverse.Helpers;

public class DataverseEntityResolver : DefaultContractResolver
{
    private List<string> _ignoreProps;
    private AttributeCollection _attributeCollection;

    public DataverseEntityResolver()
    { }

    public DataverseEntityResolver(AttributeCollection attributeCollection)
    {
        _attributeCollection = attributeCollection;
    }

    public DataverseEntityResolver(List<string> ignoreProps)
    {
        _ignoreProps = ignoreProps;
    }

    public DataverseEntityResolver(AttributeCollection attributeCollection, List<string> ignoreProps)
    {
        _attributeCollection = attributeCollection;
        _ignoreProps = ignoreProps;
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, MemberSerialization.OptOut);
        if ((_ignoreProps?.Select(s => s.ToLower()).Contains(prop.PropertyName?.ToLower()) ?? false) || (!_attributeCollection?.ContainsKey(prop.PropertyName?.ToLower()) ?? false))
        {
            prop.ShouldSerialize = o => false;
        }

        return prop;
    }

    public void Reset(AttributeCollection attributeCollection, List<string> ignoreProps)
    {
        _attributeCollection = attributeCollection;
        _ignoreProps = ignoreProps;
    }
}