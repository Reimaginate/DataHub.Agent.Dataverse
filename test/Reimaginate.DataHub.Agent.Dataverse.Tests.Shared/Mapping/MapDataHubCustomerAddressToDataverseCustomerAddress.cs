using Reimaginate.Mapper;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using DataverseAccount = DataverseModel.Account;
using DataverseContact = DataverseModel.Contact;
using DataverseCustomerAddress = DataverseModel.CustomerAddress;
using DataHubAccount = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account;
using DataHubContact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;
using DataHubCustomerAddress = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.CustomerAddress;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataHubCustomerAddressToDataverseCustomerAddress : ITypeMapper<DataHubCustomerAddress, DataverseCustomerAddress>
{
    public Task<DataverseCustomerAddress> MapAsync(
        DataHubCustomerAddress from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var parentAccount = MappingHelpers.ResolveReference<DataHubAccount>(from.ParentAccount, DataverseAccount.EntityLogicalName, cache);
        var parentContact = MappingHelpers.ResolveReference<DataHubContact>(from.ParentContact, DataverseContact.EntityLogicalName, cache);

        var mapped = new DataverseCustomerAddress
        {
            AddressNumber = from.AddressNumber,
            AddressTypeCode = MapAddressType(from.AddressType),
            Name = from.Name,
            Line1 = from.AddressLine1,
            Line2 = from.AddressLine2,
            Line3 = from.AddressLine3,
            City = from.City,
            County = from.County,
            StateOrProvince = from.StateOrProvince,
            PostalCode = from.PostalCode,
            Country = from.Country,
            PostofficeBox = from.PostOfficeBox,
            PrimaryContactName = from.PrimaryContactName,
            Telephone1 = from.Telephone1,
            Telephone2 = from.Telephone2,
            Telephone3 = from.Telephone3,
            Fax = from.Fax,
            FreightTermsCode = MapFreightTerms(from.FreightTerms),
            ShippingMethodCode = MapShippingMethod(from.ShippingMethod),
            Latitude = from.Latitude,
            Longitude = from.Longitude,
            UpsZone = from.UpsZone,
            UtcOffset = from.UtcOffset,
            ParentId = parentAccount ?? parentContact
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseCustomerAddress.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.CustomerAddressId = dataverseId;
        }

        return Task.FromResult(mapped);
    }

    private static DataverseModel.CustomerAddress_AddressTypeCode? MapAddressType(CustomerAddressAddressType? value)
    {
        return value switch
        {
            CustomerAddressAddressType.BillTo => DataverseModel.CustomerAddress_AddressTypeCode.BillTo,
            CustomerAddressAddressType.ShipTo => DataverseModel.CustomerAddress_AddressTypeCode.ShipTo,
            CustomerAddressAddressType.Primary => DataverseModel.CustomerAddress_AddressTypeCode.Primary,
            CustomerAddressAddressType.Other => DataverseModel.CustomerAddress_AddressTypeCode.Other,
            _ => null
        };
    }

    private static DataverseModel.CustomerAddress_FreightTermsCode? MapFreightTerms(CustomerAddressFreightTerms? value)
    {
        return value switch
        {
            CustomerAddressFreightTerms.Fob => DataverseModel.CustomerAddress_FreightTermsCode.Fob,
            CustomerAddressFreightTerms.NoCharge => DataverseModel.CustomerAddress_FreightTermsCode.NoCharge,
            _ => null
        };
    }

    private static DataverseModel.CustomerAddress_ShippingMethodCode? MapShippingMethod(CustomerAddressShippingMethod? value)
    {
        return value switch
        {
            CustomerAddressShippingMethod.Airborne => DataverseModel.CustomerAddress_ShippingMethodCode.Airborne,
            CustomerAddressShippingMethod.Dhl => DataverseModel.CustomerAddress_ShippingMethodCode.Dhl,
            CustomerAddressShippingMethod.FedEx => DataverseModel.CustomerAddress_ShippingMethodCode.FedEx,
            CustomerAddressShippingMethod.Ups => DataverseModel.CustomerAddress_ShippingMethodCode.Ups,
            CustomerAddressShippingMethod.PostalMail => DataverseModel.CustomerAddress_ShippingMethodCode.PostalMail,
            CustomerAddressShippingMethod.FullLoad => DataverseModel.CustomerAddress_ShippingMethodCode.FullLoad,
            CustomerAddressShippingMethod.WillCall => DataverseModel.CustomerAddress_ShippingMethodCode.WillCall,
            _ => null
        };
    }
}
