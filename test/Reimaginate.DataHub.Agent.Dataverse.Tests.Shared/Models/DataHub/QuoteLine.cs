using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class QuoteLine : DataHubEntity
{
    public QuoteLine()
    {
        entityType = nameof(QuoteLine);
    }

    public EntityReference? Quote { get; set; }

    public EntityReference? Product { get; set; }

    public EntityReference? Unit { get; set; }

    public EntityReference? Currency { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? PricePerUnit { get; set; }

    public decimal? ManualDiscountAmount { get; set; }

    public decimal? BaseAmount { get; set; }

    public decimal? ExtendedAmount { get; set; }

    public string? Description { get; set; }
}
