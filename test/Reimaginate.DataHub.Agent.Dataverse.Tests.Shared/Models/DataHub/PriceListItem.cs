using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class PriceListItem : DataHubEntity
{
    public PriceListItem()
    {
        entityType = nameof(PriceListItem);
    }

    public EntityReference? PriceList { get; set; }

    public EntityReference? Product { get; set; }

    public EntityReference? Unit { get; set; }

    public EntityReference? Currency { get; set; }

    public decimal? Amount { get; set; }
}
