using Reimaginate.Mapper;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using DataverseAccount = DataverseModel.Account;
using DataverseContact = DataverseModel.Contact;
using DataverseCustomerAddress = DataverseModel.CustomerAddress;
using DataHubAccount = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account;
using DataHubContact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;
using DataHubCustomerAddress = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.CustomerAddress;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseCustomerAddressToDataHubCustomerAddress : ITypeMapper<DataverseCustomerAddress, DataHubCustomerAddress>
{
    public Task<DataHubCustomerAddress> MapAsync(
        DataverseCustomerAddress from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var parentAccount = from.ParentId?.LogicalName == DataverseAccount.EntityLogicalName
            ? MappingHelpers.ToExternalReference<DataHubAccount>(from.ParentId, DataverseAccount.EntityLogicalName)
            : null;

        var parentContact = from.ParentId?.LogicalName == DataverseContact.EntityLogicalName
            ? MappingHelpers.ToExternalReference<DataHubContact>(from.ParentId, DataverseContact.EntityLogicalName)
            : null;

        return Task.FromResult(new DataHubCustomerAddress
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseCustomerAddress.EntityLogicalName, from.Id),
            AddressNumber = from.AddressNumber,
            AddressType = MapAddressType(from.AddressTypeCode),
            Name = from.Name,
            AddressLine1 = from.Line1,
            AddressLine2 = from.Line2,
            AddressLine3 = from.Line3,
            City = from.City,
            County = from.County,
            StateOrProvince = from.StateOrProvince,
            PostalCode = from.PostalCode,
            Country = from.Country,
            PostOfficeBox = from.PostofficeBox,
            PrimaryContactName = from.PrimaryContactName,
            Telephone1 = from.Telephone1,
            Telephone2 = from.Telephone2,
            Telephone3 = from.Telephone3,
            Fax = from.Fax,
            FreightTerms = MapFreightTerms(from.FreightTermsCode),
            ShippingMethod = MapShippingMethod(from.ShippingMethodCode),
            Latitude = from.Latitude,
            Longitude = from.Longitude,
            UpsZone = from.UpsZone,
            UtcOffset = from.UtcOffset,
            ParentAccount = parentAccount,
            ParentContact = parentContact,
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseCustomerAddress.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseCustomerAddress.Fields.ModifiedOn)
        });
    }

    private static CustomerAddressAddressType? MapAddressType(DataverseModel.CustomerAddress_AddressTypeCode? value)
    {
        return value switch
        {
            DataverseModel.CustomerAddress_AddressTypeCode.BillTo => CustomerAddressAddressType.BillTo,
            DataverseModel.CustomerAddress_AddressTypeCode.ShipTo => CustomerAddressAddressType.ShipTo,
            DataverseModel.CustomerAddress_AddressTypeCode.Primary => CustomerAddressAddressType.Primary,
            DataverseModel.CustomerAddress_AddressTypeCode.Other => CustomerAddressAddressType.Other,
            _ => null
        };
    }

    private static CustomerAddressFreightTerms? MapFreightTerms(DataverseModel.CustomerAddress_FreightTermsCode? value)
    {
        return value switch
        {
            DataverseModel.CustomerAddress_FreightTermsCode.Fob => CustomerAddressFreightTerms.Fob,
            DataverseModel.CustomerAddress_FreightTermsCode.NoCharge => CustomerAddressFreightTerms.NoCharge,
            _ => null
        };
    }

    private static CustomerAddressShippingMethod? MapShippingMethod(DataverseModel.CustomerAddress_ShippingMethodCode? value)
    {
        return value switch
        {
            DataverseModel.CustomerAddress_ShippingMethodCode.Airborne => CustomerAddressShippingMethod.Airborne,
            DataverseModel.CustomerAddress_ShippingMethodCode.Dhl => CustomerAddressShippingMethod.Dhl,
            DataverseModel.CustomerAddress_ShippingMethodCode.FedEx => CustomerAddressShippingMethod.FedEx,
            DataverseModel.CustomerAddress_ShippingMethodCode.Ups => CustomerAddressShippingMethod.Ups,
            DataverseModel.CustomerAddress_ShippingMethodCode.PostalMail => CustomerAddressShippingMethod.PostalMail,
            DataverseModel.CustomerAddress_ShippingMethodCode.FullLoad => CustomerAddressShippingMethod.FullLoad,
            DataverseModel.CustomerAddress_ShippingMethodCode.WillCall => CustomerAddressShippingMethod.WillCall,
            _ => null
        };
    }
}
