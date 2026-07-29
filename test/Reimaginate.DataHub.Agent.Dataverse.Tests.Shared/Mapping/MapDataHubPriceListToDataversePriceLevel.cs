using Reimaginate.Mapper;
using DataHubPriceList = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PriceList;
using DataversePriceLevel = DataverseModel.PriceLevel;
using DataverseTransactionCurrency = DataverseModel.TransactionCurrency;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataHubPriceListToDataversePriceLevel : ITypeMapper<DataHubPriceList, DataversePriceLevel>
{
    public Task<DataversePriceLevel> MapAsync(
        DataHubPriceList from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var mapped = new DataversePriceLevel
        {
            Name = from.Name,
            Description = from.Description,
            BeginDate = from.BeginDate,
            EndDate = from.EndDate,
            TransactionCurrencyId = MappingHelpers.ResolveExternalReference(from.Currency, DataverseTransactionCurrency.EntityLogicalName),
            OwnerId = MappingHelpers.ResolveOwner(from.Owner, cache)
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataversePriceLevel.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.PriceLevelId = dataverseId;
        }

        return Task.FromResult(mapped);
    }
}
