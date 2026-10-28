using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mapper;
using Contact = DataverseModel.Contact;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseToD365OwnerContact : ITypeMapper<Contact, D365OwnerContact>
{
    public Task<D365OwnerContact> MapAsync(Contact from, CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null) => Task.FromResult(new D365OwnerContact
    {
        id = from.Id.ToString(),
        alternateKeys = [new AlternateKey("d365.contact", from.Id.ToString())],
        DoNotBulkEmail = from.DoNotBulkEmail,
        Description = from.Description,
        Owner = from.OwnerId is null ? null : new ExternalEntityReference
        {
            DataSource = "D365",
            EntityType = from.OwnerId.LogicalName == "team" ? nameof(Team) : nameof(SystemUser),
            SourceEntityType = from.OwnerId.LogicalName,
            EntityId = from.OwnerId.Id.ToString()
        }
    });
}
