using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.Test.Framework;
using Xunit;

namespace Reimaginate.DataHub.Agent.TestFramework.Dataverse.IntegrationTesting.Xunit;

[Collection(DataverseIntegrationTestCollection.Name)]
public abstract class DataverseIntegrationTestBase : IDisposable
{
    protected DataverseIntegrationTestBase()
        : this(new DataverseIntegrationTestOptions())
    {
    }

    protected DataverseIntegrationTestBase(DataverseIntegrationTestOptions options)
    {
        Host = DataverseIntegrationTestHost.Create(GetType(), options);
    }

    protected DataverseIntegrationTestHost Host { get; }

    public IConfigurationRoot Configuration => Host.Configuration;

    public IServiceProvider ServiceProvider => Host.ServiceProvider;

    protected string TestInstanceId => Host.TestInstanceId;

    protected string TestPrefix => Host.TestPrefix;

    protected IMediator Mediator => Host.Mediator;

    protected IDataHubClient DataHubClient => Host.DataHubClient;

    protected IDataverseDataService DataverseDataService => Host.DataverseDataService;

    protected string? TestDisplayName([System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
    {
        var memberInfo = GetType().GetMember(memberName).FirstOrDefault();
        var factAttribute = memberInfo?.GetCustomAttributes(typeof(FactAttribute), true).OfType<FactAttribute>().FirstOrDefault();
        return factAttribute?.DisplayName;
    }

    protected static Task<ScenarioActionResult> ActionResult(object? currentObject, Dictionary<string, object?> stash)
    {
        return Task.FromResult(new ScenarioActionResult { CurrentObject = currentObject, Outputs = stash });
    }

    protected Task DeleteDataverseRecordAsync(string logicalName, Guid id)
    {
        return Host.DeleteDataverseRecordAsync(logicalName, id);
    }

    protected Task DeleteDataHubEntityAsync(string entityType, string entityId)
    {
        return Host.DeleteDataHubEntityAsync(entityType, entityId);
    }

    protected Task<List<JObject>> GetStoredDataHubRecordsAsync()
    {
        return Host.GetStoredDataHubRecordsAsync();
    }

    public void Dispose()
    {
        Host.Dispose();
    }
}
