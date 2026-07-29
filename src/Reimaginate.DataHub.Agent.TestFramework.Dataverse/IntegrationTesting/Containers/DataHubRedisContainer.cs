using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Reimaginate.DataHub.Agent.TestFramework.Dataverse.IntegrationTesting.Containers;

public sealed class DataHubRedisContainer : IAsyncLifetime
{
    private const int RedisPort = 6379;
    private IContainer? _container;

    public static DataHubRedisContainer? Current { get; private set; }

    public string? ConnectionString { get; private set; }

    public string? SkipReason { get; private set; }

    public DataHubRedisContainer()
    {
        Current = this;
    }

    public async ValueTask InitializeAsync()
    {
        try
        {
            _container = new ContainerBuilder("redis:7-alpine")
                .WithPortBinding(RedisPort, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(RedisPort))
                .Build();

            await _container.StartAsync();
            var mappedPort = _container.GetMappedPublicPort(RedisPort);
            ConnectionString = $"{_container.Hostname}:{mappedPort},ssl=False,abortConnect=False,protocol=resp2";
        }
        catch (Exception ex)
        {
            SkipReason = $"Redis-backed Dataverse integration tests require Docker/Testcontainers with Redis available. {ex.Message}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
