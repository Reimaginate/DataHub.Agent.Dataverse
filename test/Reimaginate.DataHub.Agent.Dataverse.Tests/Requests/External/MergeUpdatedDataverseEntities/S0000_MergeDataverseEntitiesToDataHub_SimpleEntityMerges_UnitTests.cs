using Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeUpdatedDataverseEntities;
using Xunit;
using Contact = DataverseModel.Contact;
using DataHub_Contact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Requests.External.MergeUpdatedDataverseEntities;

public class S0000MergeDataverseEntitiesToDataHubSimpleEntityMergesUnitTests
{
    [Fact(DisplayName = "S0001: Merge specific Dataverse entities uses Dataverse request surface")]
    [Trait("Category", "Unit")]
    public void S0001()
    {
        var id = Guid.NewGuid();

        var request = new MergeSpecificDataverseEntitiesRequest<Contact, DataHub_Contact>([id], "merge-correlation");

        Assert.Equal([id], request.EntityIds);
        Assert.Equal("merge-correlation", request.CorrelationId);
        Assert.False(request.ForceUpdate);
        Assert.Contains("MergeSpecificDataverseEntities", request.GetType().FullName);
        Assert.Contains("Reimaginate.DataHub.Agent.Dataverse", request.GetType().FullName);
    }

    [Fact(DisplayName = "S0002: Merge updated Dataverse entities has deterministic batch defaults")]
    [Trait("Category", "Unit")]
    public void S0002()
    {
        var request = new MergeUpdatedDataverseEntitiesRequest<Contact, DataHub_Contact>();

        Assert.Equal(500, request.BatchSize);
        Assert.Equal(-1, request.Max);
        Assert.Null(request.FromDateTime);
        Assert.Null(request.JobLock);
    }
}
