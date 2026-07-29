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

public sealed class MapDataHubQuoteToDataverseQuote : ITypeMapper<DataHubQuote, DataverseQuote>
{
    public Task<DataverseQuote> MapAsync(
        DataHubQuote from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var customerAccount = MappingHelpers.ResolveReference<DataHubAccount>(from.CustomerAccount, DataverseAccount.EntityLogicalName, cache);
        var customerContact = MappingHelpers.ResolveReference<DataHubContact>(from.CustomerContact, DataverseContact.EntityLogicalName, cache);
        var mapped = new DataverseQuote
        {
            Name = from.Name,
            Description = from.Description,
            EffectiveFrom = from.EffectiveFrom,
            EffectiveTo = from.EffectiveTo,
            CustomerId = customerAccount ?? customerContact,
            OpportunityId = MappingHelpers.ResolveReference<DataHubOpportunity>(from.Opportunity, DataverseOpportunity.EntityLogicalName, cache),
            PriceLevelId = MappingHelpers.ResolveReference<DataHubPriceList>(from.PriceList, DataversePriceLevel.EntityLogicalName, cache),
            TransactionCurrencyId = MappingHelpers.ResolveExternalReference(from.Currency, DataverseTransactionCurrency.EntityLogicalName),
            OwnerId = MappingHelpers.ResolveOwner(from.Owner, cache)
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseQuote.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.QuoteId = dataverseId;
        }

        return Task.FromResult(mapped);
    }
}
