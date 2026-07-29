using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class Contact : DataHubEntity
{
    public Contact()
    {
        entityType = nameof(Contact);
    }

    public string? Salutation { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public string? Suffix { get; set; }

    public string? JobTitle { get; set; }

    public string? Department { get; set; }

    public string? Company { get; set; }

    public string? Website { get; set; }

    public string? Email { get; set; }

    public string? SecondaryEmail { get; set; }

    public string? TertiaryEmail { get; set; }

    public string? BusinessPhone { get; set; }

    public string? HomePhone { get; set; }

    public string? OtherPhone { get; set; }

    public string? MobilePhone { get; set; }

    public string? Fax { get; set; }

    public ContactAddressType? AddressType { get; set; }

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

    public DateTime? Birthdate { get; set; }

    public DateTime? Anniversary { get; set; }

    public ContactGender? Gender { get; set; }

    public string? SpouseName { get; set; }

    public string? ChildrenNames { get; set; }

    public string? AssistantName { get; set; }

    public string? AssistantPhone { get; set; }

    public string? ManagerName { get; set; }

    public string? ManagerPhone { get; set; }

    public ContactPreferredContactMethod? PreferredContactMethod { get; set; }

    public bool? DoNotEmail { get; set; }

    public bool? DoNotBulkEmail { get; set; }

    public bool? DoNotPhone { get; set; }

    public bool? DoNotFax { get; set; }

    public bool? DoNotPostalMail { get; set; }

    public bool? DoNotBulkPostalMail { get; set; }

    public bool? DoNotSendMarketingMaterial { get; set; }

    public string? Description { get; set; }

    public EntityReference? ParentAccount { get; set; }

    public EntityReference? ParentContact { get; set; }

    public EntityReference? Owner { get; set; }
}

public enum ContactPreferredContactMethod
{
    Any,
    Email,
    Phone,
    Fax,
    Mail
}

public enum ContactGender
{
    Male,
    Female
}

public enum ContactAddressType
{
    BillTo,
    ShipTo,
    Primary,
    Other
}
