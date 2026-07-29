using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.TestFramework;
using Xunit;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Framework;

public class AgentFrameworkTests
{
    [Fact(DisplayName = "Shared test instance names are isolated and agent-scoped")]
    [Trait("Category", "Unit")]
    public void TestInstanceNamesAreIsolatedAndAgentScoped()
    {
        var instance = new AgentTestInstance("Dataverse Agent", new DateTimeOffset(2026, 6, 7, 1, 2, 3, TimeSpan.Zero));

        Assert.StartsWith("Dataverse_Agent_20260607010203000_", instance.Id);
    }

    [Fact(DisplayName = "Shared service builder composes agent services")]
    [Trait("Category", "Unit")]
    public void ServiceBuilderComposesAgentServices()
    {
        var provider = new AgentTestServiceBuilder()
            .Add((services, _) => services.AddSingleton("registered"))
            .Build(new ConfigurationBuilder().Build());

        Assert.Equal("registered", provider.GetRequiredService<string>());
    }

    [Fact(DisplayName = "Dataverse agent options default to Dataverse data source")]
    [Trait("Category", "Unit")]
    public void DataverseAgentOptionsDefaultToDataverseDataSource()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AgentId"] = "TestAgent",
                ["DataverseConnectionString"] = "AuthType=ClientSecret;Url=https://example.crm.dynamics.com;ClientId=00000000-0000-0000-0000-000000000000;ClientSecret=test;",
                ["DataHubTimeZone"] = "E. Australia Standard Time",
                ["ProcessingLockOptions:UseRepository"] = "inmemory"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddDataverseAgent(options => options.WithAppSettingsConfig(config));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DataverseAgentOptions>>().Value;

        Assert.Equal("TestAgent", options.AgentId);
        Assert.Equal("Dataverse", options.DataSource);
        Assert.Contains("AuthType=ClientSecret", options.DataverseConnectionString);
        Assert.Equal("inmemory", options.ProcessingLockOptions.UseRepository);
    }
}
