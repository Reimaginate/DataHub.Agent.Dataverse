using Reimaginate.Mapper;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using DataverseAccount = DataverseModel.Account;
using DataverseContact = DataverseModel.Contact;
using DataHubAccount = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account;
using DataHubContact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataHubAccountToDataverseAccount : ITypeMapper<DataHubAccount, DataverseAccount>
{
    public Task<DataverseAccount> MapAsync(
        DataHubAccount from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var mapped = new DataverseAccount
        {
            Name = from.Name,
            AccountNumber = from.AccountNumber,
            WebsiteUrl = from.Website,
            Description = from.Description,
            EmailAddress1 = from.Email,
            EmailAddress2 = from.SecondaryEmail,
            EmailAddress3 = from.TertiaryEmail,
            Telephone1 = from.MainPhone,
            Telephone2 = from.OtherPhone,
            Telephone3 = from.Telephone3,
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
            TickerSymbol = from.TickerSymbol,
            Sic = from.Sic,
            NumberOfEmployees = from.NumberOfEmployees,
            Revenue = MappingHelpers.ToMoney(from.Revenue),
            CreditLimit = MappingHelpers.ToMoney(from.CreditLimit),
            CreditOnHold = from.CreditOnHold,
            PreferredContactMethodCode = MapPreferredContactMethod(from.PreferredContactMethod),
            DoNotEmail = from.DoNotEmail,
            DoNotBulkEmail = from.DoNotBulkEmail,
            DoNotPhone = from.DoNotPhone,
            DoNotFax = from.DoNotFax,
            DoNotPostalMail = from.DoNotPostalMail,
            DoNotBulkPostalMail = from.DoNotBulkPostalMail,
            DoNotSendMm = from.DoNotSendMarketingMaterial,
            ParentAccountId = MappingHelpers.ResolveReference<DataHubAccount>(from.ParentAccount, DataverseAccount.EntityLogicalName, cache),
            PrimaryContactId = MappingHelpers.ResolveReference<DataHubContact>(from.PrimaryContact, DataverseContact.EntityLogicalName, cache),
            OwnerId = MappingHelpers.ResolveOwner(from.Owner, cache)
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseAccount.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.AccountId = dataverseId;
        }

        return Task.FromResult(mapped);
    }

    private static DataverseModel.Account_Address1_AddressTypeCode? MapAddressType(AccountAddressType? value)
    {
        return value switch
        {
            AccountAddressType.BillTo => DataverseModel.Account_Address1_AddressTypeCode.BillTo,
            AccountAddressType.ShipTo => DataverseModel.Account_Address1_AddressTypeCode.ShipTo,
            AccountAddressType.Primary => DataverseModel.Account_Address1_AddressTypeCode.Primary,
            AccountAddressType.Other => DataverseModel.Account_Address1_AddressTypeCode.Other,
            _ => null
        };
    }

    private static DataverseModel.Account_PreferredContactMethodCode? MapPreferredContactMethod(AccountPreferredContactMethod? value)
    {
        return value switch
        {
            AccountPreferredContactMethod.Any => DataverseModel.Account_PreferredContactMethodCode.Any,
            AccountPreferredContactMethod.Email => DataverseModel.Account_PreferredContactMethodCode.Email,
            AccountPreferredContactMethod.Phone => DataverseModel.Account_PreferredContactMethodCode.Phone,
            AccountPreferredContactMethod.Fax => DataverseModel.Account_PreferredContactMethodCode.Fax,
            AccountPreferredContactMethod.Mail => DataverseModel.Account_PreferredContactMethodCode.Mail,
            _ => null
        };
    }
}
