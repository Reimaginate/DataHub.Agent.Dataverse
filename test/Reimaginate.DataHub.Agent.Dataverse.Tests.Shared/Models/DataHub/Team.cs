using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class Team : DataHubEntity
{
    public Team()
    {
        entityType = nameof(Team);
    }

    public string? Name { get; set; }

    public string? TeamType { get; set; }

    public EntityReference? BusinessUnit { get; set; }

    public EntityReference? Administrator { get; set; }
}
