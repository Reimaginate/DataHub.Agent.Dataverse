using Reimaginate.Mapper;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using DataverseAccount = DataverseModel.Account;
using DataverseContact = DataverseModel.Contact;
using DataHubAccount = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account;
using DataHubContact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataHubContactToDataverseContact : ITypeMapper<DataHubContact, DataverseContact>
{
    public Task<DataverseContact> MapAsync(
        DataHubContact from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var parentAccount = MappingHelpers.ResolveReference<DataHubAccount>(from.ParentAccount, DataverseAccount.EntityLogicalName, cache);
        var parentContact = MappingHelpers.ResolveReference<DataHubContact>(from.ParentContact, DataverseContact.EntityLogicalName, cache);

        var mapped = new DataverseContact
        {
            Salutation = from.Salutation,
            FirstName = from.FirstName,
            MiddleName = from.MiddleName,
            LastName = from.LastName,
            Suffix = from.Suffix,
            JobTitle = from.JobTitle,
            Department = from.Department,
            Company = from.Company,
            WebsiteUrl = from.Website,
            EmailAddress1 = from.Email,
            EmailAddress2 = from.SecondaryEmail,
            EmailAddress3 = from.TertiaryEmail,
            Telephone1 = from.BusinessPhone,
            Telephone2 = from.HomePhone,
            Telephone3 = from.OtherPhone,
            MobilePhone = from.MobilePhone,
            Fax = from.Fax,
            Address1_AddressTypeCode = MapAddressType(from.AddressType),
            Address1_Name = from.AddressName,
            Address1_Line1 = from.AddressLine1,
            Address1_Line2 = from.AddressLine2,
            Address1_Line3 = from.AddressLine3,
            Address1_City = from.City,
            Address1_County = from.County,
            Address1_StateOrProvince = from.StateOrProvince,
            Address1_PostalCode = from.PostalCode,
            Address1_Country = from.Country,
            Address1_PostofficeBox = from.PostOfficeBox,
            Address1_Telephone1 = from.AddressPhone,
            Address1_Telephone2 = from.AddressPhone2,
            Address1_Telephone3 = from.AddressPhone3,
            Address1_Fax = from.AddressFax,
            Birthdate = from.Birthdate,
            Anniversary = from.Anniversary,
            GenderCode = MapGender(from.Gender),
            SpousesName = from.SpouseName,
            ChildrensNames = from.ChildrenNames,
            AssistantName = from.AssistantName,
            AssistantPhone = from.AssistantPhone,
            ManagerName = from.ManagerName,
            ManagerPhone = from.ManagerPhone,
            PreferredContactMethodCode = MapPreferredContactMethod(from.PreferredContactMethod),
            DoNotEmail = from.DoNotEmail,
            DoNotBulkEmail = from.DoNotBulkEmail,
            DoNotPhone = from.DoNotPhone,
            DoNotFax = from.DoNotFax,
            DoNotPostalMail = from.DoNotPostalMail,
            DoNotBulkPostalMail = from.DoNotBulkPostalMail,
            DoNotSendMm = from.DoNotSendMarketingMaterial,
            Description = from.Description,
            ParentCustomerId = parentAccount ?? parentContact,
            OwnerId = MappingHelpers.ResolveOwner(from.Owner, cache)
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseContact.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.ContactId = dataverseId;
        }

        return Task.FromResult(mapped);
    }

    private static DataverseModel.Contact_Address1_AddressTypeCode? MapAddressType(ContactAddressType? value)
    {
        return value switch
        {
            ContactAddressType.BillTo => DataverseModel.Contact_Address1_AddressTypeCode.BillTo,
            ContactAddressType.ShipTo => DataverseModel.Contact_Address1_AddressTypeCode.ShipTo,
            ContactAddressType.Primary => DataverseModel.Contact_Address1_AddressTypeCode.Primary,
            ContactAddressType.Other => DataverseModel.Contact_Address1_AddressTypeCode.Other,
            _ => null
        };
    }

    private static DataverseModel.Contact_GenderCode? MapGender(ContactGender? value)
    {
        return value switch
        {
            ContactGender.Male => DataverseModel.Contact_GenderCode.Male,
            ContactGender.Female => DataverseModel.Contact_GenderCode.Female,
            _ => null
        };
    }

    private static DataverseModel.Contact_PreferredContactMethodCode? MapPreferredContactMethod(ContactPreferredContactMethod? value)
    {
        return value switch
        {
            ContactPreferredContactMethod.Any => DataverseModel.Contact_PreferredContactMethodCode.Any,
            ContactPreferredContactMethod.Email => DataverseModel.Contact_PreferredContactMethodCode.Email,
            ContactPreferredContactMethod.Phone => DataverseModel.Contact_PreferredContactMethodCode.Phone,
            ContactPreferredContactMethod.Fax => DataverseModel.Contact_PreferredContactMethodCode.Fax,
            ContactPreferredContactMethod.Mail => DataverseModel.Contact_PreferredContactMethodCode.Mail,
            _ => null
        };
    }
}
