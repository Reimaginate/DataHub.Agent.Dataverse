using Reimaginate.Mapper;
using DataHubProduct = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Product;
using DataHubQuote = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Quote;
using DataHubQuoteLine = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.QuoteLine;
using DataverseProduct = DataverseModel.Product;
using DataverseQuote = DataverseModel.Quote;
using DataverseQuoteDetail = DataverseModel.QuoteDetail;
using DataverseTransactionCurrency = DataverseModel.TransactionCurrency;
using DataverseUoM = DataverseModel.UoM;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseQuoteDetailToDataHubQuoteLine : ITypeMapper<DataverseQuoteDetail, DataHubQuoteLine>
{
    public Task<DataHubQuoteLine> MapAsync(
        DataverseQuoteDetail from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubQuoteLine
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseQuoteDetail.EntityLogicalName, from.Id),
            Quote = MappingHelpers.ToExternalReference<DataHubQuote>(from.QuoteId, DataverseQuote.EntityLogicalName),
            Product = MappingHelpers.ToExternalReference<DataHubProduct>(from.ProductId, DataverseProduct.EntityLogicalName),
            Unit = MappingHelpers.ToExternalReference(from.UoMId, DataverseUoM.EntityLogicalName, DataverseUoM.EntityLogicalName),
            Currency = MappingHelpers.ToExternalReference(from.TransactionCurrencyId, DataverseTransactionCurrency.EntityLogicalName, DataverseTransactionCurrency.EntityLogicalName),
            Quantity = from.Quantity,
            PricePerUnit = MappingHelpers.ToDecimal(from.PricePerUnit),
            ManualDiscountAmount = MappingHelpers.ToDecimal(from.ManualDiscountAmount),
            BaseAmount = MappingHelpers.ToDecimal(from.BaseAmount),
            ExtendedAmount = MappingHelpers.ToDecimal(from.ExtendedAmount),
            Description = from.Description,
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseQuoteDetail.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseQuoteDetail.Fields.ModifiedOn)
        });
    }
}
