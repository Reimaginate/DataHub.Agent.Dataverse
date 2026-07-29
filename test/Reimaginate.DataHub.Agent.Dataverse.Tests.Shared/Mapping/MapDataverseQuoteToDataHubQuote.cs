using Reimaginate.Mapper;
using DataHubAccount = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account;
using DataHubContact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;
using DataHubOpportunity = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Opportunity;
using DataHubPriceList = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PriceList;
using DataHubQuote = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Quote;
using DataverseAccount = DataverseModel.Account;
using DataverseContact = DataverseModel.Contact;
using DataverseOpportunity = DataverseModel.Opportunity;
using DataversePriceLevel = DataverseModel.PriceLevel;
using DataverseQuote = DataverseModel.Quote;
using DataverseTransactionCurrency = DataverseModel.TransactionCurrency;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseQuoteToDataHubQuote : ITypeMapper<DataverseQuote, DataHubQuote>
{
    public Task<DataHubQuote> MapAsync(
        DataverseQuote from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubQuote
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseQuote.EntityLogicalName, from.Id),
            Name = from.Name,
            QuoteNumber = from.QuoteNumber,
            Description = from.Description,
            EffectiveFrom = from.EffectiveFrom,
            EffectiveTo = from.EffectiveTo,
            CustomerAccount = from.CustomerId?.LogicalName == DataverseAccount.EntityLogicalName
                ? MappingHelpers.ToExternalReference<DataHubAccount>(from.CustomerId, DataverseAccount.EntityLogicalName)
                : null,
            CustomerContact = from.CustomerId?.LogicalName == DataverseContact.EntityLogicalName
                ? MappingHelpers.ToExternalReference<DataHubContact>(from.CustomerId, DataverseContact.EntityLogicalName)
                : null,
            Opportunity = MappingHelpers.ToExternalReference<DataHubOpportunity>(from.OpportunityId, DataverseOpportunity.EntityLogicalName),
            PriceList = MappingHelpers.ToExternalReference<DataHubPriceList>(from.PriceLevelId, DataversePriceLevel.EntityLogicalName),
            Currency = MappingHelpers.ToExternalReference(from.TransactionCurrencyId, DataverseTransactionCurrency.EntityLogicalName, DataverseTransactionCurrency.EntityLogicalName),
            Owner = MappingHelpers.ToOwnerReference(from.OwnerId),
            TotalAmount = MappingHelpers.ToDecimal(from.TotalAmount),
            TotalLineItemAmount = MappingHelpers.ToDecimal(from.TotalLineItemAmount),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseQuote.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseQuote.Fields.ModifiedOn)
        });
    }
}
