using Reimaginate.Mapper;
using DataHubProduct = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Product;
using DataversePriceLevel = DataverseModel.PriceLevel;
using DataverseProduct = DataverseModel.Product;
using DataverseTransactionCurrency = DataverseModel.TransactionCurrency;
using DataverseUoM = DataverseModel.UoM;
using DataverseUoMSchedule = DataverseModel.UoMSchedule;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseProductToDataHubProduct : ITypeMapper<DataverseProduct, DataHubProduct>
{
    public Task<DataHubProduct> MapAsync(
        DataverseProduct from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubProduct
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseProduct.EntityLogicalName, from.Id),
            ProductNumber = from.ProductNumber,
            Name = from.Name,
            Description = from.Description,
            Price = MappingHelpers.ToDecimal(from.Price),
            DefaultUnitGroup = MappingHelpers.ToExternalReference(from.DefaultUoMScheduleId, DataverseUoMSchedule.EntityLogicalName, DataverseUoMSchedule.EntityLogicalName),
            DefaultUnit = MappingHelpers.ToExternalReference(from.DefaultUoMId, DataverseUoM.EntityLogicalName, DataverseUoM.EntityLogicalName),
            DefaultPriceList = MappingHelpers.ToExternalReference<Models.DataHub.PriceList>(from.PriceLevelId, DataversePriceLevel.EntityLogicalName),
            Currency = MappingHelpers.ToExternalReference(from.TransactionCurrencyId, DataverseTransactionCurrency.EntityLogicalName, DataverseTransactionCurrency.EntityLogicalName),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseProduct.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseProduct.Fields.ModifiedOn)
        });
    }
}
