using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

[RelatedEntityType("D365", "DataverseModel.Account, Reimaginate.DataHub.Agent.Dataverse.Tests.Shared",
    mappedPropertiesIn: new[] { "name", "new_start", "new_end" },
    mappedPropertiesOut: new[] { "name", "new_start", "new_end" })]
public sealed class CalendarAccount : DataHubEntity
{
    public CalendarAccount() { entityType = nameof(CalendarAccount); }
    public string Name { get; set; }
    public DateTime? Date { get; set; }
}
