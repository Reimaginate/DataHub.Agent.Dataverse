// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.Dataverse.Config;

public class DataverseAgentOptions
{
    public string AgentId { get; set; }
    public string DataSource { get; set; } = "Dataverse";
    public string DataverseConnectionString { get; set; }
    public string DataHubTimeZone { get; set; }
    public ProcessingLockOptions ProcessingLockOptions { get; set; }
}
