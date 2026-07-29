using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class Quote : DataHubEntity
{
    public Quote()
    {
        entityType = nameof(Quote);
    }

    public string? Name { get; set; }

    public string? QuoteNumber { get; set; }

    public string? Description { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public EntityReference? CustomerAccount { get; set; }

    public EntityReference? CustomerContact { get; set; }

    public EntityReference? Opportunity { get; set; }

    public EntityReference? PriceList { get; set; }

    public EntityReference? Currency { get; set; }

    public EntityReference? Owner { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? TotalLineItemAmount { get; set; }
}
