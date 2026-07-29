using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Testcontainers.CosmosDb;
using Xunit;

namespace Reimaginate.DataHub.Agent.TestFramework.Dataverse.IntegrationTesting.Containers;

public sealed class DataHubCosmosDbEmulator : IAsyncLifetime
{
    private static readonly TimeSpan InitializationTimeout = TimeSpan.FromMinutes(3);
    private bool _reuseContainers;

    public static DataHubCosmosDbEmulator? Current { get; private set; }

    public DataHubCosmosDbEmulator()
    {
        Current = this;
    }

    public CosmosDbContainer? CosmosDbContainer { get; private set; }

    public string? ConnectionString { get; private set; }

    public string DatabaseName { get; } = "DataHub";

    public string? SkipReason { get; private set; }

    public async ValueTask InitializeAsync()
    {
        var configuration = LoadConfiguration();
        _reuseContainers = configuration.GetValue<bool>("TestFixtures:ReuseContainers");

        if (!configuration.GetValue<bool>("TestFixtures:UseLocalCosmosDb"))
        {
            SkipReason = "Dataverse integration tests require TestFixtures:UseLocalCosmosDb=true so the Cosmos fixture provides a connection string.";
            return;
        }

        try
        {
            await StartContainersAsync();
            await CreateDatabaseAsync();
        }
        catch (Exception ex)
        {
            SkipReason = $"Dataverse integration tests require Docker/Testcontainers with the Cosmos DB emulator available. {ex.Message}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_reuseContainers && CosmosDbContainer is not null)
        {
            await CosmosDbContainer.DisposeAsync();
        }
    }

    private static IConfiguration LoadConfiguration()
    {
        try
        {
            return CreateConfigurationBuilder(includeUserSecrets: true).Build();
        }
        catch (UnauthorizedAccessException)
        {
            return CreateConfigurationBuilder(includeUserSecrets: false).Build();
        }
        catch (IOException)
        {
            return CreateConfigurationBuilder(includeUserSecrets: false).Build();
        }
    }

    private static IConfigurationBuilder CreateConfigurationBuilder(bool includeUserSecrets)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables();

        if (!includeUserSecrets)
        {
            return builder;
        }

        var assembliesWithSecrets = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.GetCustomAttribute<UserSecretsIdAttribute>() != null)
            .Distinct();

        foreach (var assembly in assembliesWithSecrets)
        {
            builder.AddUserSecrets(assembly, optional: true);
        }

        return builder;
    }

    private async Task StartContainersAsync()
    {
        CosmosDbContainer = new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview")
            .WithCleanUp(true)
            .WithReuse(_reuseContainers)
            .Build();

        await CosmosDbContainer.StartAsync();
        ConnectionString = CosmosDbContainer.GetConnectionString();
    }

    private CosmosClient CreateCosmosClient()
    {
        ArgumentNullException.ThrowIfNull(CosmosDbContainer);

        return new CosmosClient(
            CosmosDbContainer.GetConnectionString(),
            new CosmosClientOptions
            {
                HttpClientFactory = () => CosmosDbContainer.HttpClient,
                ConnectionMode = ConnectionMode.Gateway,
                AllowBulkExecution = true,
                RequestTimeout = TimeSpan.FromMinutes(2),
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
                MaxRetryAttemptsOnRateLimitedRequests = 10
            });
    }

    private async Task CreateDatabaseAsync()
    {
        if (CosmosDbContainer is null)
        {
            return;
        }

        using var cosmosClient = CreateCosmosClient();

        await ExecuteWithRetryAsync(
            async cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await cosmosClient.ReadAccountAsync();
                return true;
            },
            "connect to the Cosmos emulator");

        var database = await ExecuteWithRetryAsync(
            cancellationToken => cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName, cancellationToken: cancellationToken),
            $"create the {DatabaseName} database");

        await EnsureContainerExistsAsync(database.Database, "Entities", ["/entityType", "/id"]);
        await EnsureContainerExistsAsync(database.Database, "TrackingData", ["/DataSource", "/EntityType", "/EntityId"]);
        await EnsureContainerExistsAsync(database.Database, "SyncMarkers", ["/id"]);
        await EnsureContainerExistsAsync(database.Database, "Logs", ["/Type", "/id"]);
        await EnsureContainerExistsAsync(database.Database, "ResolutionPromises", ["/_id"]);
        await EnsureContainerExistsAsync(database.Database, "Configs", ["/id"]);
        await EnsureContainerExistsAsync(database.Database, "Management", ["/_dt", "/id"]);
    }

    private static async Task EnsureContainerExistsAsync(Database database, string containerName, IReadOnlyList<string> partitionKeyPaths)
    {
        var properties = new ContainerProperties
        {
            Id = containerName,
            PartitionKeyPaths = [.. partitionKeyPaths]
        };

        await ExecuteWithRetryAsync(
            cancellationToken => database.CreateContainerIfNotExistsAsync(properties, cancellationToken: cancellationToken),
            $"create the {containerName} container");
    }

    private static async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string description)
    {
        var deadline = DateTimeOffset.UtcNow.Add(InitializationTimeout);
        var attempt = 0;
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            attempt++;
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(45));

            try
            {
                return await operation(cancellationTokenSource.Token);
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                lastException = ex;
                var delay = TimeSpan.FromSeconds(Math.Min(5 * attempt, 15));

                if (DateTimeOffset.UtcNow.Add(delay) >= deadline)
                {
                    break;
                }

                await Task.Delay(delay, CancellationToken.None);
            }
        }

        throw new TimeoutException($"Timed out waiting to {description} for the Cosmos emulator.", lastException);
    }

    private static bool IsTransient(Exception exception)
    {
        return exception switch
        {
            CosmosException cosmosException when cosmosException.StatusCode is System.Net.HttpStatusCode.RequestTimeout
                or System.Net.HttpStatusCode.ServiceUnavailable
                or System.Net.HttpStatusCode.TooManyRequests => true,
            TimeoutException => true,
            TaskCanceledException => true,
            OperationCanceledException => true,
            System.Net.Http.HttpRequestException => true,
            IOException => true,
            _ when exception.InnerException is not null => IsTransient(exception.InnerException),
            _ => false
        };
    }
}
