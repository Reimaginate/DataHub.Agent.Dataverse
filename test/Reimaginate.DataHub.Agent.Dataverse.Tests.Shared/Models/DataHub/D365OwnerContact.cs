using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

[RelatedEntityType("D365", "DataverseModel.Contact, Reimaginate.DataHub.Agent.Dataverse.Tests.Shared",
    mappedPropertiesIn: new[] { "ownerid", "donotbulkemail", "description" },
    mappedPropertiesOut: new[] { "ownerid", "donotbulkemail", "description" })]
public sealed class D365OwnerContact : DataHubEntity
{
    public D365OwnerContact() { entityType = nameof(D365OwnerContact); }
    public EntityReference? Owner { get; set; }
    public bool? DoNotBulkEmail { get; set; }
    public string? Description { get; set; }
}
