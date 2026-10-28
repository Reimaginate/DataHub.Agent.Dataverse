using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mapper;
using Contact = DataverseModel.Contact;
using XrmReference = Microsoft.Xrm.Sdk.EntityReference;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

// QPAC-style strict reference resolution and null-owner omission.
public sealed class MapD365OwnerContactToDataverse : ITypeMapper<D365OwnerContact, Contact>
{
    public Task<Contact> MapAsync(D365OwnerContact from, CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var mapped = new Contact { DoNotBulkEmail = from.DoNotBulkEmail, Description = from.Description };
        if (from.Owner is { } owner)
        {
            var logicalName = owner.EntityType == nameof(Team) ? "team" : "systemuser";
            if (owner._tag == nameof(ExternalEntityReference))
            {
                mapped.OwnerId = new XrmReference(logicalName, Guid.Parse(owner.EntityId));
            }
            else
            {
                var entities = cache?.GetValueOrDefault(owner.EntityType) as IEnumerable<JObject>;
                var entity = entities?.FirstOrDefault(e => e.Value<string>(nameof(DataHubEntity.id)) == owner.EntityId);
                var sourceId = entity?[nameof(DataHubEntity.alternateKeys)]?.ToObject<List<AlternateKey>>()?
                    .FirstOrDefault(k => k.Key == $"d365.{logicalName}")?.Value;
                if (sourceId is null)
                    throw new InvalidOperationException($"D365 {(logicalName == "team" ? "Team" : "SystemUser")} Id Could not be determined");
                mapped.OwnerId = new XrmReference(logicalName, Guid.Parse(sourceId));
            }
        }
        return Task.FromResult(mapped);
    }
}
