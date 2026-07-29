using System.Diagnostics;

namespace Reimaginate.DataHub.Agent.TestFramework.Dataverse;

public static partial class DiagnosticConfig
{
    public static class DataverseAgent
    {
        public const string ServiceName = "Dataverse";
        public const string ApplicationName = "Dataverse";
        public const string ApplicationVersion = "1.0.0";
        public static ActivitySource ActivitySource = new(ApplicationName, ApplicationVersion);
    }

    public static class DataHubAgent
    {
        public const string ServiceName = "DataHub";
        public const string ApplicationName = "DataHub";
        public const string ApplicationVersion = "1.0.0";
        public static ActivitySource ActivitySource = new(ApplicationName, ApplicationVersion);
    }
}
