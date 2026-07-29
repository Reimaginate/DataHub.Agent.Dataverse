using Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeUpdatedDataverseEntities;
using Xunit;
using Contact = DataverseModel.Contact;
using DataHub_Contact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Requests.Pipelines.MergeDataverseEntitiesToDataHub;

public class S0200_MergeUpdatedDataverseEntities_BulkMerges
{
    [Fact(DisplayName = "S0201: Bulk merge request carries explicit batch and max limits")]
    [Trait("Category", "Unit")]
    public void S0201()
    {
        var request = new MergeUpdatedDataverseEntitiesRequest<Contact, DataHub_Contact>
        {
            BatchSize = 1000,
            Max = 5000,
            CorrelationId = "bulk-merge-correlation"
        };

        Assert.Equal(1000, request.BatchSize);
        Assert.Equal(5000, request.Max);
        Assert.Equal("bulk-merge-correlation", request.CorrelationId);
    }
}
