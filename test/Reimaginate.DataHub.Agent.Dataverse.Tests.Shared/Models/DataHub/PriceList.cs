using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class PriceList : DataHubEntity
{
    public PriceList()
    {
        entityType = nameof(PriceList);
    }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTime? BeginDate { get; set; }

    public DateTime? EndDate { get; set; }

    public EntityReference? Currency { get; set; }

    public EntityReference? Owner { get; set; }
}
