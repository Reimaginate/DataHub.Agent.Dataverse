using Reimaginate.Mapper;
using DataHubPriceList = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PriceList;
using DataHubProduct = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Product;
using DataversePriceLevel = DataverseModel.PriceLevel;
using DataverseProduct = DataverseModel.Product;
using DataverseTransactionCurrency = DataverseModel.TransactionCurrency;
using DataverseUoM = DataverseModel.UoM;
using DataverseUoMSchedule = DataverseModel.UoMSchedule;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataHubProductToDataverseProduct : ITypeMapper<DataHubProduct, DataverseProduct>
{
    public Task<DataverseProduct> MapAsync(
        DataHubProduct from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var mapped = new DataverseProduct
        {
            ProductNumber = from.ProductNumber,
            Name = from.Name,
            Description = from.Description,
            Price = MappingHelpers.ToMoney(from.Price),
            DefaultUoMScheduleId = MappingHelpers.ResolveExternalReference(from.DefaultUnitGroup, DataverseUoMSchedule.EntityLogicalName),
            DefaultUoMId = MappingHelpers.ResolveExternalReference(from.DefaultUnit, DataverseUoM.EntityLogicalName),
            PriceLevelId = MappingHelpers.ResolveReference<DataHubPriceList>(from.DefaultPriceList, DataversePriceLevel.EntityLogicalName, cache),
            TransactionCurrencyId = MappingHelpers.ResolveExternalReference(from.Currency, DataverseTransactionCurrency.EntityLogicalName),
            QuantityDecimal = 2
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseProduct.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.ProductId = dataverseId;
        }

        return Task.FromResult(mapped);
    }
}
