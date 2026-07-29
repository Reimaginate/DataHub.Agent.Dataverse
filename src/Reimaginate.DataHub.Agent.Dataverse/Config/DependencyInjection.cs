using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.DataHub.Agent.Dataverse.Services.DataHubEntityCache;
using Reimaginate.ProcessingLockService;
using Reimaginate.DataHub.Client.Config;
using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;

namespace Reimaginate.DataHub.Agent.Dataverse.Config;

public static class DependencyInjection
{
    public static IServiceCollection AddDataverseAgent(this IServiceCollection services, Action<AddDataverseAgentOptions> options = null)
    {
        var addDataverseAgentOptions = new AddDataverseAgentOptions();
        options?.Invoke(addDataverseAgentOptions);

        services.Configure<DataverseAgentOptions>(agentOptions =>
        {
            addDataverseAgentOptions.Config.Bind(agentOptions);
            agentOptions.AgentId = addDataverseAgentOptions.DataverseAgentOptions.AgentId;
            agentOptions.DataSource = string.IsNullOrWhiteSpace(addDataverseAgentOptions.DataverseAgentOptions.DataSource)
                ? "Dataverse"
                : addDataverseAgentOptions.DataverseAgentOptions.DataSource;
            agentOptions.DataverseConnectionString = addDataverseAgentOptions.DataverseAgentOptions.DataverseConnectionString;
            agentOptions.DataHubTimeZone = addDataverseAgentOptions.DataverseAgentOptions.DataHubTimeZone;
            agentOptions.ProcessingLockOptions = addDataverseAgentOptions.DataverseAgentOptions.ProcessingLockOptions;
        });
        services.AddHttpClient();
        services.AddOptions();

        if (addDataverseAgentOptions.DataverseAgentOptions.ProcessingLockOptions == null)
        {
            throw new Exception("APP_SETTING_MISSING: ProcessingLockOptions");
        }

        if (string.IsNullOrEmpty(addDataverseAgentOptions.DataverseAgentOptions.ProcessingLockOptions.UseRepository))
        {
            throw new Exception("APP_SETTING_MISSING: ProcessingLockOptions.UseRepository");
        }


        var processingLockOptions = addDataverseAgentOptions.DataverseAgentOptions.ProcessingLockOptions;
        ; switch (processingLockOptions?.UseRepository?.ToLower())
        {
            case "inmemory":
                services.AddProcessingLockService(cfg => cfg.WithInMemoryRepository());
                break;


            case "redis":
                var redisClientOptions = processingLockOptions.RedisClientOptions;

                if (processingLockOptions.RedisClientOptions == null)
                {
                    throw new Exception("APP_SETTING_MISSING: ProcessingLockOptions.RedisClientOptions");
                }

                if (string.IsNullOrEmpty(redisClientOptions.ConnString))
                {
                    throw new Exception("APP_SETTING_MISSING: ProcessingLockOptions.RedisClientOptions.ConnString");
                }

                services.AddProcessingLockService(cfg =>
                {
                    cfg.WithRedisRepository(r =>
                    {
                        r.ConnString = redisClientOptions.ConnString;
                        r.ConnectTimeout = redisClientOptions.ConnectTimeout;
                        r.Protocol = redisClientOptions.Protocol;
                        r.SyncTimeout = redisClientOptions.SyncTimeout;
                    });
                });
                break;

        }

        services.AddDataHubClient(cfg => cfg.WithAppSettingsConfig(addDataverseAgentOptions.Config, "DataHubClientOptions"));

        services.AddSingleton<ITimeService>(sp =>
        {
            var agentOptions = sp.GetRequiredService<IOptions<DataverseAgentOptions>>();
            var timeZoneId = agentOptions.Value.DataHubTimeZone;

            if (string.IsNullOrEmpty(timeZoneId))
            {
                throw new Exception("MISSING_APP_SETTING: DataHubTimeZone");
            }

            return new TimeService(timeZoneId);
        });

        services.AddSingleton(sp =>
        {
            var agentOptions = sp.GetRequiredService<IOptions<DataverseAgentOptions>>();
            var connectionString = agentOptions.Value.DataverseConnectionString;
            var serviceClient = new ServiceClient(connectionString); ;
            return serviceClient;
        });

        services.AddTransient<IDataverseDataService, DataverseDataService>();
        services.AddTransient<IDataHubEntityCache, DataHubEntityCache>();

        return services;
    }
}
