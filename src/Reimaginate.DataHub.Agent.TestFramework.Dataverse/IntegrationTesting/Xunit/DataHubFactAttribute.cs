using System.Runtime.CompilerServices;
using Xunit;

namespace Reimaginate.DataHub.Agent.TestFramework.Dataverse.IntegrationTesting.Xunit;

public sealed class DataHubFactAttribute : FactAttribute
{
    public DataHubFactAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        SkipExceptions = [typeof(DataHubTestSkippedException)];
    }
}
