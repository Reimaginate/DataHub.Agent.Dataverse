using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mapper;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using DataverseAccount = DataverseModel.Account;
using DataverseContact = DataverseModel.Contact;
using DataHubAccount = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account;
using DataHubContact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseAccountToDataHubAccount : ITypeMapper<DataverseAccount, DataHubAccount>
{
    public Task<DataHubAccount> MapAsync(
        DataverseAccount from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubAccount
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseAccount.EntityLogicalName, from.Id),
            Name = from.Name,
            AccountNumber = from.AccountNumber,
            Website = from.WebsiteUrl,
            Description = from.Description,
            Email = from.EmailAddress1,
            SecondaryEmail = from.EmailAddress2,
            TertiaryEmail = from.EmailAddress3,
            MainPhone = from.Telephone1,
            OtherPhone = from.Telephone2,
            Telephone3 = from.Telephone3,
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
            TickerSymbol = from.TickerSymbol,
            Sic = from.Sic,
            NumberOfEmployees = from.NumberOfEmployees,
            Revenue = MappingHelpers.ToDecimal(from.Revenue),
            CreditLimit = MappingHelpers.ToDecimal(from.CreditLimit),
            CreditOnHold = from.CreditOnHold,
            PreferredContactMethod = MapPreferredContactMethod(from.PreferredContactMethodCode),
            DoNotEmail = from.DoNotEmail,
            DoNotBulkEmail = from.DoNotBulkEmail,
            DoNotPhone = from.DoNotPhone,
            DoNotFax = from.DoNotFax,
            DoNotPostalMail = from.DoNotPostalMail,
            DoNotBulkPostalMail = from.DoNotBulkPostalMail,
            DoNotSendMarketingMaterial = from.DoNotSendMm,
            ParentAccount = MappingHelpers.ToExternalReference<DataHubAccount>(from.ParentAccountId, DataverseAccount.EntityLogicalName),
            PrimaryContact = MappingHelpers.ToExternalReference<DataHubContact>(from.PrimaryContactId, DataverseContact.EntityLogicalName),
            Owner = MappingHelpers.ToOwnerReference(from.OwnerId),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseAccount.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseAccount.Fields.ModifiedOn)
        });
    }

    private static AccountAddressType? MapAddressType(DataverseModel.Account_Address1_AddressTypeCode? value)
    {
        return value switch
        {
            DataverseModel.Account_Address1_AddressTypeCode.BillTo => AccountAddressType.BillTo,
            DataverseModel.Account_Address1_AddressTypeCode.ShipTo => AccountAddressType.ShipTo,
            DataverseModel.Account_Address1_AddressTypeCode.Primary => AccountAddressType.Primary,
            DataverseModel.Account_Address1_AddressTypeCode.Other => AccountAddressType.Other,
            _ => null
        };
    }

    private static AccountPreferredContactMethod? MapPreferredContactMethod(DataverseModel.Account_PreferredContactMethodCode? value)
    {
        return value switch
        {
            DataverseModel.Account_PreferredContactMethodCode.Any => AccountPreferredContactMethod.Any,
            DataverseModel.Account_PreferredContactMethodCode.Email => AccountPreferredContactMethod.Email,
            DataverseModel.Account_PreferredContactMethodCode.Phone => AccountPreferredContactMethod.Phone,
            DataverseModel.Account_PreferredContactMethodCode.Fax => AccountPreferredContactMethod.Fax,
            DataverseModel.Account_PreferredContactMethodCode.Mail => AccountPreferredContactMethod.Mail,
            _ => null
        };
    }
}
