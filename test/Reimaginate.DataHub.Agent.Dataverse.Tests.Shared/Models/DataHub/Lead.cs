using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class Lead : DataHubEntity
{
    public Lead()
    {
        entityType = nameof(Lead);
    }

    public string? Subject { get; set; }

    public string? Salutation { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public string? CompanyName { get; set; }

    public string? JobTitle { get; set; }

    public string? Website { get; set; }

    public string? Email { get; set; }

    public string? SecondaryEmail { get; set; }

    public string? TertiaryEmail { get; set; }

    public string? BusinessPhone { get; set; }

    public string? HomePhone { get; set; }

    public string? OtherPhone { get; set; }

    public string? MobilePhone { get; set; }

    public string? Fax { get; set; }

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

    public decimal? BudgetAmount { get; set; }

    public LeadBudgetStatus? BudgetStatus { get; set; }

    public decimal? EstimatedAmount { get; set; }

    public DateTime? EstimatedCloseDate { get; set; }

    public LeadQuality? LeadQuality { get; set; }

    public LeadNeed? Need { get; set; }

    public LeadPreferredContactMethod? PreferredContactMethod { get; set; }

    public LeadPurchaseProcess? PurchaseProcess { get; set; }

    public LeadPurchaseTimeFrame? PurchaseTimeFrame { get; set; }

    public bool? DoNotEmail { get; set; }

    public bool? DoNotBulkEmail { get; set; }

    public bool? DoNotPhone { get; set; }

    public bool? DoNotFax { get; set; }

    public bool? DoNotPostalMail { get; set; }

    public bool? DoNotSendMarketingMaterial { get; set; }

    public string? Description { get; set; }

    public EntityReference? Owner { get; set; }
}

public enum LeadBudgetStatus
{
    NoCommittedBudget,
    MayBuy,
    CanBuy,
    WillBuy
}

public enum LeadQuality
{
    Hot,
    Warm,
    Cold
}

public enum LeadNeed
{
    MustHave,
    ShouldHave,
    GoodToHave,
    NoNeed
}

public enum LeadPreferredContactMethod
{
    Any,
    Email,
    Phone,
    Fax,
    Mail
}

public enum LeadPurchaseProcess
{
    Individual,
    Committee,
    Unknown
}

public enum LeadPurchaseTimeFrame
{
    Immediate,
    ThisQuarter,
    NextQuarter,
    ThisYear,
    Unknown
}
