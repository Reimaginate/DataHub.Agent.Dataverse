using Reimaginate.Mapper;
using DataHubPriceList = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PriceList;
using DataversePriceLevel = DataverseModel.PriceLevel;
using DataverseTransactionCurrency = DataverseModel.TransactionCurrency;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataversePriceLevelToDataHubPriceList : ITypeMapper<DataversePriceLevel, DataHubPriceList>
{
    public Task<DataHubPriceList> MapAsync(
        DataversePriceLevel from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubPriceList
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataversePriceLevel.EntityLogicalName, from.Id),
            Name = from.Name,
            Description = from.Description,
            BeginDate = from.BeginDate,
            EndDate = from.EndDate,
            Currency = MappingHelpers.ToExternalReference(from.TransactionCurrencyId, DataverseTransactionCurrency.EntityLogicalName, DataverseTransactionCurrency.EntityLogicalName),
            Owner = MappingHelpers.ToOwnerReference(from.OwnerId),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataversePriceLevel.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataversePriceLevel.Fields.ModifiedOn)
        });
    }
}
