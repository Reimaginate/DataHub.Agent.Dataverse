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

public sealed class MapDataHubPriceListItemToDataverseProductPriceLevel : ITypeMapper<DataHubPriceListItem, DataverseProductPriceLevel>
{
    public Task<DataverseProductPriceLevel> MapAsync(
        DataHubPriceListItem from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var mapped = new DataverseProductPriceLevel
        {
            PriceLevelId = MappingHelpers.ResolveReference<DataHubPriceList>(from.PriceList, DataversePriceLevel.EntityLogicalName, cache),
            ProductId = MappingHelpers.ResolveReference<DataHubProduct>(from.Product, DataverseProduct.EntityLogicalName, cache),
            UoMId = MappingHelpers.ResolveExternalReference(from.Unit, DataverseUoM.EntityLogicalName),
            TransactionCurrencyId = MappingHelpers.ResolveExternalReference(from.Currency, DataverseTransactionCurrency.EntityLogicalName),
            Amount = MappingHelpers.ToMoney(from.Amount)
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseProductPriceLevel.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.ProductPriceLevelId = dataverseId;
        }

        return Task.FromResult(mapped);
    }
}
