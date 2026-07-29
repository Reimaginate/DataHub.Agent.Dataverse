namespace Reimaginate.DataHub.Agent.TestFramework.Dataverse.IntegrationTesting;

public sealed class DataHubTestSkippedException(string message, Exception? innerException = null)
    : Exception(message, innerException);
