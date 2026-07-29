using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class SystemUser : DataHubEntity
{
    public SystemUser()
    {
        entityType = nameof(SystemUser);
    }

    public string? FullName { get; set; }

    public string? DomainName { get; set; }

    public string? InternalEmailAddress { get; set; }

    public EntityReference? BusinessUnit { get; set; }
}
