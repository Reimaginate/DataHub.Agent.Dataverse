using Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncSpecificDataHubEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncUpdatedDataHubEntities;
using Xunit;
using Contact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;
using Dataverse_Contact = DataverseModel.Contact;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Requests.External;

public class SyncUpdatedDataHubEntities_UnitTests
{
    [Fact(DisplayName = "S0001: Sync specific DataHub entities uses Dataverse request surface")]
    [Trait("Category", "Unit")]
    public void S0001()
    {
        var ids = new List<string> { Guid.NewGuid().ToString() };

        var request = new SyncSpecificDataHubEntitiesRequest<Contact, Dataverse_Contact>(ids, "sync-correlation");

        Assert.Equal(ids, request.EntityIds);
        Assert.Equal("sync-correlation", request.CorrelationId);
        Assert.Contains("SyncSpecificDataHubEntities", request.GetType().FullName);
        Assert.Contains("Reimaginate.DataHub.Agent.Dataverse", request.GetType().FullName);
    }

    [Fact(DisplayName = "S0002: Sync updated DataHub entities has deterministic defaults")]
    [Trait("Category", "Unit")]
    public void S0002()
    {
        var request = new SyncUpdatedDataHubEntitiesRequest<Contact, Dataverse_Contact>();

        Assert.Equal(-1, request.Max);
        Assert.Null(request.BatchSize);
        Assert.Null(request.FromDateTime);
        Assert.Null(request.JobLock);
    }
}
