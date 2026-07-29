using Reimaginate.Mapper;
using DataHubPriceList = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PriceList;
using DataHubPriceListItem = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PriceListItem;
using DataHubProduct = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Product;
using DataversePriceLevel = DataverseModel.PriceLevel;
using DataverseProduct = DataverseModel.Product;
using DataverseProductPriceLevel = DataverseModel.ProductPriceLevel;
using DataverseTransactionCurrency = DataverseModel.TransactionCurrency;
using DataverseUoM = DataverseModel.UoM;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseProductPriceLevelToDataHubPriceListItem : ITypeMapper<DataverseProductPriceLevel, DataHubPriceListItem>
{
    public Task<DataHubPriceListItem> MapAsync(
        DataverseProductPriceLevel from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubPriceListItem
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseProductPriceLevel.EntityLogicalName, from.Id),
            PriceList = MappingHelpers.ToExternalReference<DataHubPriceList>(from.PriceLevelId, DataversePriceLevel.EntityLogicalName),
            Product = MappingHelpers.ToExternalReference<DataHubProduct>(from.ProductId, DataverseProduct.EntityLogicalName),
            Unit = MappingHelpers.ToExternalReference(from.UoMId, DataverseUoM.EntityLogicalName, DataverseUoM.EntityLogicalName),
            Currency = MappingHelpers.ToExternalReference(from.TransactionCurrencyId, DataverseTransactionCurrency.EntityLogicalName, DataverseTransactionCurrency.EntityLogicalName),
            Amount = MappingHelpers.ToDecimal(from.Amount),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseProductPriceLevel.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseProductPriceLevel.Fields.ModifiedOn)
        });
    }
}
