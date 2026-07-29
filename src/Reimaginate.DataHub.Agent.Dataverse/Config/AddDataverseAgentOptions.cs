using Microsoft.Extensions.Configuration;

namespace Reimaginate.DataHub.Agent.Dataverse.Config;

public class AddDataverseAgentOptions
{
    internal IConfiguration Config { get; set; } = new ConfigurationBuilder().Build();
    internal DataverseAgentOptions DataverseAgentOptions { get; set; } = new();

    public AddDataverseAgentOptions WithConnectionString(string connectionString)
    {
        DataverseAgentOptions.DataverseConnectionString = connectionString;
        return this;
    }

    public AddDataverseAgentOptions WithAgentId(string agentId)
    {
        DataverseAgentOptions.AgentId = agentId;
        return this;
    }

    public AddDataverseAgentOptions WithDataSourceId(string dataSourceId)
    {
        DataverseAgentOptions.DataSource = dataSourceId;
        return this;
    }

    public AddDataverseAgentOptions WithDataSource(string dataSource)
    {
        DataverseAgentOptions.DataSource = dataSource;
        return this;
    }

    public AddDataverseAgentOptions WithAppSettingsConfig(IConfiguration config, string key = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        if (!string.IsNullOrEmpty(key))
        {
            Config = Config.GetSection(key);
        }

        Config.Bind(DataverseAgentOptions);
        return this;
    }

}
