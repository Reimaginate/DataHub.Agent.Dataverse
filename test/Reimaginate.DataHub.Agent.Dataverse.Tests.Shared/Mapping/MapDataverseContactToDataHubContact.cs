using Reimaginate.Mapper;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using DataverseAccount = DataverseModel.Account;
using DataverseContact = DataverseModel.Contact;
using DataHubAccount = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account;
using DataHubContact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseContactToDataHubContact : ITypeMapper<DataverseContact, DataHubContact>
{
    public Task<DataHubContact> MapAsync(
        DataverseContact from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var parentAccount = from.ParentCustomerId?.LogicalName == DataverseAccount.EntityLogicalName
            ? MappingHelpers.ToExternalReference<DataHubAccount>(from.ParentCustomerId, DataverseAccount.EntityLogicalName)
            : null;

        var parentContact = from.ParentCustomerId?.LogicalName == DataverseContact.EntityLogicalName
            ? MappingHelpers.ToExternalReference<DataHubContact>(from.ParentCustomerId, DataverseContact.EntityLogicalName)
            : MappingHelpers.ToExternalReference<DataHubContact>(from.ParentContactId, DataverseContact.EntityLogicalName);

        return Task.FromResult(new DataHubContact
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseContact.EntityLogicalName, from.Id),
            Salutation = from.Salutation,
            FirstName = from.FirstName,
            MiddleName = from.MiddleName,
            LastName = from.LastName,
            Suffix = from.Suffix,
            JobTitle = from.JobTitle,
            Department = from.Department,
            Company = from.Company,
            Website = from.WebsiteUrl,
            Email = from.EmailAddress1,
            SecondaryEmail = from.EmailAddress2,
            TertiaryEmail = from.EmailAddress3,
            BusinessPhone = from.Telephone1,
            HomePhone = from.Telephone2,
            OtherPhone = from.Telephone3,
            MobilePhone = from.MobilePhone,
            Fax = from.Fax,
            AddressType = MapAddressType(from.Address1_AddressTypeCode),
            AddressName = from.Address1_Name,
            AddressLine1 = from.Address1_Line1,
            AddressLine2 = from.Address1_Line2,
            AddressLine3 = from.Address1_Line3,
            City = from.Address1_City,
            County = from.Address1_County,
            StateOrProvince = from.Address1_StateOrProvince,
            PostalCode = from.Address1_PostalCode,
            Country = from.Address1_Country,
            PostOfficeBox = from.Address1_PostofficeBox,
            AddressPhone = from.Address1_Telephone1,
            AddressPhone2 = from.Address1_Telephone2,
            AddressPhone3 = from.Address1_Telephone3,
            AddressFax = from.Address1_Fax,
            Birthdate = from.Birthdate,
            Anniversary = from.Anniversary,
            Gender = MapGender(from.GenderCode),
            SpouseName = from.SpousesName,
            ChildrenNames = from.ChildrensNames,
            AssistantName = from.AssistantName,
            AssistantPhone = from.AssistantPhone,
            ManagerName = from.ManagerName,
            ManagerPhone = from.ManagerPhone,
            PreferredContactMethod = MapPreferredContactMethod(from.PreferredContactMethodCode),
            DoNotEmail = from.DoNotEmail,
            DoNotBulkEmail = from.DoNotBulkEmail,
            DoNotPhone = from.DoNotPhone,
            DoNotFax = from.DoNotFax,
            DoNotPostalMail = from.DoNotPostalMail,
            DoNotBulkPostalMail = from.DoNotBulkPostalMail,
            DoNotSendMarketingMaterial = from.DoNotSendMm,
            Description = from.Description,
            ParentAccount = parentAccount,
            ParentContact = parentContact,
            Owner = MappingHelpers.ToOwnerReference(from.OwnerId),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseContact.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseContact.Fields.ModifiedOn)
        });
    }

    private static ContactAddressType? MapAddressType(DataverseModel.Contact_Address1_AddressTypeCode? value)
    {
        return value switch
        {
            DataverseModel.Contact_Address1_AddressTypeCode.BillTo => ContactAddressType.BillTo,
            DataverseModel.Contact_Address1_AddressTypeCode.ShipTo => ContactAddressType.ShipTo,
            DataverseModel.Contact_Address1_AddressTypeCode.Primary => ContactAddressType.Primary,
            DataverseModel.Contact_Address1_AddressTypeCode.Other => ContactAddressType.Other,
            _ => null
        };
    }

    private static ContactGender? MapGender(DataverseModel.Contact_GenderCode? value)
    {
        return value switch
        {
            DataverseModel.Contact_GenderCode.Male => ContactGender.Male,
            DataverseModel.Contact_GenderCode.Female => ContactGender.Female,
            _ => null
        };
    }

    private static ContactPreferredContactMethod? MapPreferredContactMethod(DataverseModel.Contact_PreferredContactMethodCode? value)
    {
        return value switch
        {
            DataverseModel.Contact_PreferredContactMethodCode.Any => ContactPreferredContactMethod.Any,
            DataverseModel.Contact_PreferredContactMethodCode.Email => ContactPreferredContactMethod.Email,
            DataverseModel.Contact_PreferredContactMethodCode.Phone => ContactPreferredContactMethod.Phone,
            DataverseModel.Contact_PreferredContactMethodCode.Fax => ContactPreferredContactMethod.Fax,
            DataverseModel.Contact_PreferredContactMethodCode.Mail => ContactPreferredContactMethod.Mail,
            _ => null
        };
    }
}
