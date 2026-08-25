using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using Reimaginate.DataHub.Agent.TestFramework.Dataverse;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Framework;

public sealed class DataverseAgentAlternateKeyTests
{
    [Fact(DisplayName = "Dataverse test agent resolves the configured D365 alternate key")]
    [Trait("Category", "Unit")]
    public void ResolvesConfiguredD365AlternateKey()
    {
        var entity = EntityWithAlternateKeys(new AlternateKey("d365.logical_entity", "d365-id"));

        var result = ResolveAlternateKey("D365", entity);

        Assert.Equal("d365-id", result);
    }

    [Theory(DisplayName = "Dataverse test agent resolves configured logical and CLR type alternate keys")]
    [Trait("Category", "Unit")]
    [InlineData("custom.logical_entity")]
    [InlineData("CUSTOM.TESTDATAVERSEENTITY")]
    public void ResolvesConfiguredLogicalAndClrTypeAlternateKeys(string alternateKeyName)
    {
        var entity = EntityWithAlternateKeys(new AlternateKey(alternateKeyName, "custom-id"));

        var result = ResolveAlternateKey("  Custom  ", entity);

        Assert.Equal("custom-id", result);
    }

    [Fact(DisplayName = "Dataverse test agent accepts D365 as a legacy alternate-key alias")]
    [Trait("Category", "Unit")]
    public void ResolvesLegacyD365AlternateKey()
    {
        var entity = EntityWithAlternateKeys(new AlternateKey("d365.logical_entity", "legacy-id"));

        var result = ResolveAlternateKey("Dataverse", entity);

        Assert.Equal("legacy-id", result);
    }

    [Fact(DisplayName = "Dataverse test agent prefers the configured alternate-key source")]
    [Trait("Category", "Unit")]
    public void PrefersConfiguredAlternateKeyOverLegacyAlias()
    {
        var entity = EntityWithAlternateKeys(
            new AlternateKey("d365.logical_entity", "legacy-id"),
            new AlternateKey("qpac.logical_entity", "configured-id"));

        var result = ResolveAlternateKey("QPAC", entity);

        Assert.Equal("configured-id", result);
    }

    [Fact(DisplayName = "Dataverse test agent reports the configured source when no alternate key matches")]
    [Trait("Category", "Unit")]
    public void MissingAlternateKeyIdentifiesConfiguredDataSource()
    {
        var entity = EntityWithAlternateKeys(new AlternateKey("other.logical_entity", "other-id"));

        var exception = Assert.Throws<InvalidOperationException>(() => ResolveAlternateKey("QPAC", entity));

        Assert.Contains("configured data source 'QPAC'", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(TestDataverseEntity), exception.Message, StringComparison.Ordinal);
    }

    private static string ResolveAlternateKey(string dataSource, Account entity)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOptions<DataverseAgentOptions>>(Options.Create(new DataverseAgentOptions
        {
            DataSource = dataSource
        }));

        using var serviceProvider = services.BuildServiceProvider();
        var agent = new DataverseAgent(serviceProvider);
        return agent.GetDataverseAlternateKey<TestDataverseEntity>(entity);
    }

    private static Account EntityWithAlternateKeys(params AlternateKey[] alternateKeys) => new()
    {
        id = Guid.NewGuid().ToString(),
        alternateKeys = alternateKeys.ToList()
    };

    private sealed class TestDataverseEntity : Entity
    {
        public const string EntityLogicalName = "logical_entity";
    }
}
