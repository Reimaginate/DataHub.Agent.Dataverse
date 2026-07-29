using Reimaginate.DataHub.Agent.TestFramework.Dataverse.IntegrationTesting.Containers;
using Xunit;

namespace Reimaginate.DataHub.Agent.TestFramework.Dataverse.IntegrationTesting.Xunit;

[CollectionDefinition(Name)]
public sealed class DataverseIntegrationTestCollection :
    ICollectionFixture<DataHubCosmosDbEmulator>,
    ICollectionFixture<DataHubRedisContainer>
{
    public const string Name = "Dataverse Agent Integration Tests";
}
