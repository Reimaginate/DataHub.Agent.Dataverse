using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class Account : DataHubEntity
{
    public Account()
    {
        entityType = nameof(Account);
    }

    public string? Name { get; set; }

    public string? AccountNumber { get; set; }

    public string? Website { get; set; }

    public string? Description { get; set; }

    public string? Email { get; set; }

    public string? SecondaryEmail { get; set; }

    public string? TertiaryEmail { get; set; }

    public string? MainPhone { get; set; }

    public string? OtherPhone { get; set; }

    public string? Telephone3 { get; set; }

    public string? Fax { get; set; }

    public AccountAddressType? AddressType { get; set; }

    public string? AddressName { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? AddressLine3 { get; set; }

    public string? City { get; set; }

    public string? County { get; set; }

    public string? StateOrProvince { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? PostOfficeBox { get; set; }

    public string? AddressPhone { get; set; }

    public string? AddressPhone2 { get; set; }

    public string? AddressPhone3 { get; set; }

    public string? AddressFax { get; set; }

    public string? TickerSymbol { get; set; }

    public string? Sic { get; set; }

    public int? NumberOfEmployees { get; set; }

    public decimal? Revenue { get; set; }

    public decimal? CreditLimit { get; set; }

    public bool? CreditOnHold { get; set; }

    public AccountPreferredContactMethod? PreferredContactMethod { get; set; }

    public bool? DoNotEmail { get; set; }

    public bool? DoNotBulkEmail { get; set; }

    public bool? DoNotPhone { get; set; }

    public bool? DoNotFax { get; set; }

    public bool? DoNotPostalMail { get; set; }

    public bool? DoNotBulkPostalMail { get; set; }

    public bool? DoNotSendMarketingMaterial { get; set; }

    public EntityReference? ParentAccount { get; set; }

    public EntityReference? PrimaryContact { get; set; }

    public EntityReference? Owner { get; set; }
}

public enum AccountPreferredContactMethod
{
    Any,
    Email,
    Phone,
    Fax,
    Mail
}

public enum AccountAddressType
{
    BillTo,
    ShipTo,
    Primary,
    Other
}
