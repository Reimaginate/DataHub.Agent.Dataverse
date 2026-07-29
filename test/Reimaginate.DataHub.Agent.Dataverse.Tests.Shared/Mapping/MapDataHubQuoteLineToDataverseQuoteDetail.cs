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

public sealed class MapDataHubQuoteLineToDataverseQuoteDetail : ITypeMapper<DataHubQuoteLine, DataverseQuoteDetail>
{
    public Task<DataverseQuoteDetail> MapAsync(
        DataHubQuoteLine from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var mapped = new DataverseQuoteDetail
        {
            QuoteId = MappingHelpers.ResolveReference<DataHubQuote>(from.Quote, DataverseQuote.EntityLogicalName, cache),
            ProductId = MappingHelpers.ResolveReference<DataHubProduct>(from.Product, DataverseProduct.EntityLogicalName, cache),
            UoMId = MappingHelpers.ResolveExternalReference(from.Unit, DataverseUoM.EntityLogicalName),
            TransactionCurrencyId = MappingHelpers.ResolveExternalReference(from.Currency, DataverseTransactionCurrency.EntityLogicalName),
            Quantity = from.Quantity,
            PricePerUnit = MappingHelpers.ToMoney(from.PricePerUnit),
            ManualDiscountAmount = MappingHelpers.ToMoney(from.ManualDiscountAmount),
            Description = from.Description,
            IsPriceOverridden = true,
            IsProductOverridden = false
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseQuoteDetail.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.QuoteDetailId = dataverseId;
        }

        return Task.FromResult(mapped);
    }
}
