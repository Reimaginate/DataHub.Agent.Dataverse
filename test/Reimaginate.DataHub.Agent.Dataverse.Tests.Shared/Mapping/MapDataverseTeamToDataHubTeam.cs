using Reimaginate.Mapper;
using DataHubSystemUser = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.SystemUser;
using DataHubTeam = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Team;
using DataverseBusinessUnit = DataverseModel.BusinessUnit;
using DataverseSystemUser = DataverseModel.SystemUser;
using DataverseTeam = DataverseModel.Team;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseTeamToDataHubTeam : ITypeMapper<DataverseTeam, DataHubTeam>
{
    public Task<DataHubTeam> MapAsync(
        DataverseTeam from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubTeam
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseTeam.EntityLogicalName, from.Id),
            Name = from.Name,
            TeamType = from.TeamType?.ToString(),
            BusinessUnit = MappingHelpers.ToExternalReference(from.BusinessUnitId, DataverseBusinessUnit.EntityLogicalName, DataverseBusinessUnit.EntityLogicalName),
            Administrator = MappingHelpers.ToExternalReference<DataHubSystemUser>(from.AdministratorId, DataverseSystemUser.EntityLogicalName),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseTeam.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseTeam.Fields.ModifiedOn)
        });
    }
}
