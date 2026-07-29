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

public sealed class MapDataverseIncidentToDataHubCase : ITypeMapper<DataverseIncident, DataHubCase>
{
    public Task<DataHubCase> MapAsync(
        DataverseIncident from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubCase
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseIncident.EntityLogicalName, from.Id),
            Title = from.Title,
            TicketNumber = from.TicketNumber,
            Description = from.Description,
            Priority = MapPriority(from.PriorityCode),
            CaseType = MapCaseType(from.CaseTypeCode),
            CaseOrigin = MapCaseOrigin(from.CaseOriginCode),
            CustomerAccount = from.CustomerId?.LogicalName == DataverseAccount.EntityLogicalName
                ? MappingHelpers.ToExternalReference<DataHubAccount>(from.CustomerId, DataverseAccount.EntityLogicalName)
                : null,
            CustomerContact = from.CustomerId?.LogicalName == DataverseContact.EntityLogicalName
                ? MappingHelpers.ToExternalReference<DataHubContact>(from.CustomerId, DataverseContact.EntityLogicalName)
                : null,
            PrimaryContact = MappingHelpers.ToExternalReference<DataHubContact>(from.PrimaryContactId, DataverseContact.EntityLogicalName),
            ResponsibleContact = MappingHelpers.ToExternalReference<DataHubContact>(from.ResponsibleContactId, DataverseContact.EntityLogicalName),
            ParentCase = MappingHelpers.ToExternalReference<DataHubCase>(from.ParentCaseId, DataverseIncident.EntityLogicalName),
            Subject = MappingHelpers.ToExternalReference(from.SubjectId, DataverseSubject.EntityLogicalName, DataverseSubject.EntityLogicalName),
            Entitlement = MappingHelpers.ToExternalReference(from.EntitlementId, DataverseEntitlement.EntityLogicalName, DataverseEntitlement.EntityLogicalName),
            Sla = MappingHelpers.ToExternalReference(from.SLAId, DataverseSla.EntityLogicalName, DataverseSla.EntityLogicalName),
            Product = MappingHelpers.ToExternalReference(from.ProductId, DataverseProduct.EntityLogicalName, DataverseProduct.EntityLogicalName),
            Owner = MappingHelpers.ToOwnerReference(from.OwnerId),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseIncident.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseIncident.Fields.ModifiedOn)
        });
    }

    private static CasePriority? MapPriority(DataverseModel.incident_prioritycode? value)
    {
        return value switch
        {
            DataverseModel.incident_prioritycode.High => CasePriority.High,
            DataverseModel.incident_prioritycode.Normal => CasePriority.Normal,
            DataverseModel.incident_prioritycode.Low => CasePriority.Low,
            _ => null
        };
    }

    private static CaseType? MapCaseType(DataverseModel.incident_casetypecode? value)
    {
        return value switch
        {
            DataverseModel.incident_casetypecode.Question => CaseType.Question,
            DataverseModel.incident_casetypecode.Problem => CaseType.Problem,
            DataverseModel.incident_casetypecode.Request => CaseType.Request,
            _ => null
        };
    }

    private static CaseOrigin? MapCaseOrigin(DataverseModel.incident_caseorigincode? value)
    {
        return value switch
        {
            DataverseModel.incident_caseorigincode.Phone => CaseOrigin.Phone,
            DataverseModel.incident_caseorigincode.Email => CaseOrigin.Email,
            DataverseModel.incident_caseorigincode.Web => CaseOrigin.Web,
            _ => null
        };
    }
}
