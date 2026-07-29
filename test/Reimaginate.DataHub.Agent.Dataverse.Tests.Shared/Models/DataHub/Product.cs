using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class Product : DataHubEntity
{
    public Product()
    {
        entityType = nameof(Product);
    }

    public string? ProductNumber { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public EntityReference? DefaultUnitGroup { get; set; }

    public EntityReference? DefaultUnit { get; set; }

    public EntityReference? DefaultPriceList { get; set; }

    public EntityReference? Currency { get; set; }

    public EntityReference? Owner { get; set; }
}
