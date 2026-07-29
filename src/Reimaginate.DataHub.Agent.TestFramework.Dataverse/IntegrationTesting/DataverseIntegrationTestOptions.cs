using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Reimaginate.DataHub.Agent.TestFramework.Dataverse.IntegrationTesting;

public sealed class DataverseIntegrationTestOptions
{
    public string TestInstancePrefix { get; set; } = "DataverseIntegration";

    public string DataHubConfigurationSection { get; set; } = "DataHub";

    public string DataverseAgentConfigurationSection { get; set; } = "DataverseAgentOptions";

    public string DataHubDatabaseName { get; set; } = "DataHub";

    public string DataContainer { get; set; } = "Entities";

    public string TrackingDataContainer { get; set; } = "TrackingData";

    public string SyncMarkersContainer { get; set; } = "SyncMarkers";

    public string LogsContainer { get; set; } = "Logs";

    public string ResolutionPromisesContainer { get; set; } = "ResolutionPromises";

    public string ConfigsContainer { get; set; } = "Configs";

    public string ManagementContainer { get; set; } = "Management";

    public Action<IServiceCollection, IConfiguration>? ConfigureServices { get; set; }
}
