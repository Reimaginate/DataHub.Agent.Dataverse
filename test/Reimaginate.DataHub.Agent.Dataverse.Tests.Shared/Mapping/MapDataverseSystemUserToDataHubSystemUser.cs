using Reimaginate.Mapper;
using DataHubSystemUser = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.SystemUser;
using DataverseBusinessUnit = DataverseModel.BusinessUnit;
using DataverseSystemUser = DataverseModel.SystemUser;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseSystemUserToDataHubSystemUser : ITypeMapper<DataverseSystemUser, DataHubSystemUser>
{
    public Task<DataHubSystemUser> MapAsync(
        DataverseSystemUser from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubSystemUser
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseSystemUser.EntityLogicalName, from.Id),
            FullName = from.FullName,
            DomainName = from.DomainName,
            InternalEmailAddress = from.GetAttributeValue<string>("internalemailaddress"),
            BusinessUnit = MappingHelpers.ToExternalReference(from.BusinessUnitId, DataverseBusinessUnit.EntityLogicalName, DataverseBusinessUnit.EntityLogicalName),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseSystemUser.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseSystemUser.Fields.ModifiedOn)
        });
    }
}
