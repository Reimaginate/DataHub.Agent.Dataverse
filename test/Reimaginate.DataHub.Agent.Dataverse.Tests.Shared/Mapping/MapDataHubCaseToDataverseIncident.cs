using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using Reimaginate.Mapper;
using DataverseAccount = DataverseModel.Account;
using DataverseContact = DataverseModel.Contact;
using DataverseEntitlement = DataverseModel.Entitlement;
using DataverseIncident = DataverseModel.Incident;
using DataverseProduct = DataverseModel.Product;
using DataverseSla = DataverseModel.SLA;
using DataverseSubject = DataverseModel.Subject;
using DataHubAccount = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account;
using DataHubCase = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Case;
using DataHubContact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataHubCaseToDataverseIncident : ITypeMapper<DataHubCase, DataverseIncident>
{
    public Task<DataverseIncident> MapAsync(
        DataHubCase from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var customerAccount = MappingHelpers.ResolveReference<DataHubAccount>(from.CustomerAccount, DataverseAccount.EntityLogicalName, cache);
        var customerContact = MappingHelpers.ResolveReference<DataHubContact>(from.CustomerContact, DataverseContact.EntityLogicalName, cache);

        var mapped = new DataverseIncident
        {
            Title = from.Title,
            Description = from.Description,
            PriorityCode = MapPriority(from.Priority),
            CaseTypeCode = MapCaseType(from.CaseType),
            CaseOriginCode = MapCaseOrigin(from.CaseOrigin),
            CustomerId = customerAccount ?? customerContact,
            PrimaryContactId = MappingHelpers.ResolveReference<DataHubContact>(from.PrimaryContact, DataverseContact.EntityLogicalName, cache),
            ResponsibleContactId = MappingHelpers.ResolveReference<DataHubContact>(from.ResponsibleContact, DataverseContact.EntityLogicalName, cache),
            ParentCaseId = MappingHelpers.ResolveReference<DataHubCase>(from.ParentCase, DataverseIncident.EntityLogicalName, cache),
            SubjectId = MappingHelpers.ResolveExternalReference(from.Subject, DataverseSubject.EntityLogicalName),
            EntitlementId = MappingHelpers.ResolveExternalReference(from.Entitlement, DataverseEntitlement.EntityLogicalName),
            SLAId = MappingHelpers.ResolveExternalReference(from.Sla, DataverseSla.EntityLogicalName),
            ProductId = MappingHelpers.ResolveExternalReference(from.Product, DataverseProduct.EntityLogicalName),
            OwnerId = MappingHelpers.ResolveOwner(from.Owner, cache)
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseIncident.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.IncidentId = dataverseId;
        }

        return Task.FromResult(mapped);
    }

    private static DataverseModel.incident_prioritycode? MapPriority(CasePriority? value)
    {
        return value switch
        {
            CasePriority.High => DataverseModel.incident_prioritycode.High,
            CasePriority.Normal => DataverseModel.incident_prioritycode.Normal,
            CasePriority.Low => DataverseModel.incident_prioritycode.Low,
            _ => null
        };
    }

    private static DataverseModel.incident_casetypecode? MapCaseType(CaseType? value)
    {
        return value switch
        {
            CaseType.Question => DataverseModel.incident_casetypecode.Question,
            CaseType.Problem => DataverseModel.incident_casetypecode.Problem,
            CaseType.Request => DataverseModel.incident_casetypecode.Request,
            _ => null
        };
    }

    private static DataverseModel.incident_caseorigincode? MapCaseOrigin(CaseOrigin? value)
    {
        return value switch
        {
            CaseOrigin.Phone => DataverseModel.incident_caseorigincode.Phone,
            CaseOrigin.Email => DataverseModel.incident_caseorigincode.Email,
            CaseOrigin.Web => DataverseModel.incident_caseorigincode.Web,
            _ => null
        };
    }
}
