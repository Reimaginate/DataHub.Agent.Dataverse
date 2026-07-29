using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class CustomerAddress : DataHubEntity
{
    public CustomerAddress()
    {
        entityType = nameof(CustomerAddress);
    }

    public int? AddressNumber { get; set; }

    public CustomerAddressAddressType? AddressType { get; set; }

    public string? Name { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? AddressLine3 { get; set; }

    public string? City { get; set; }

    public string? County { get; set; }

    public string? StateOrProvince { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? PostOfficeBox { get; set; }

    public string? PrimaryContactName { get; set; }

    public string? Telephone1 { get; set; }

    public string? Telephone2 { get; set; }

    public string? Telephone3 { get; set; }

    public string? Fax { get; set; }

    public CustomerAddressFreightTerms? FreightTerms { get; set; }

    public CustomerAddressShippingMethod? ShippingMethod { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? UpsZone { get; set; }

    public int? UtcOffset { get; set; }

    public EntityReference? ParentAccount { get; set; }

    public EntityReference? ParentContact { get; set; }
}

public enum CustomerAddressAddressType
{
    BillTo,
    ShipTo,
    Primary,
    Other
}

public enum CustomerAddressFreightTerms
{
    Fob,
    NoCharge
}

public enum CustomerAddressShippingMethod
{
    Airborne,
    Dhl,
    FedEx,
    Ups,
    PostalMail,
    FullLoad,
    WillCall
}
