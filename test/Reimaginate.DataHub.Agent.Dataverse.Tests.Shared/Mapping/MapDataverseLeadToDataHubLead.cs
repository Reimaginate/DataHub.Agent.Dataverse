using Reimaginate.Mapper;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using DataverseLead = DataverseModel.Lead;
using DataHubLead = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Lead;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseLeadToDataHubLead : ITypeMapper<DataverseLead, DataHubLead>
{
    public Task<DataHubLead> MapAsync(
        DataverseLead from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubLead
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseLead.EntityLogicalName, from.Id),
            Subject = from.Subject,
            Salutation = from.Salutation,
            FirstName = from.FirstName,
            MiddleName = from.MiddleName,
            LastName = from.LastName,
            CompanyName = from.CompanyName,
            JobTitle = from.JobTitle,
            Website = from.WebsiteUrl,
            Email = from.EmailAddress1,
            SecondaryEmail = from.EmailAddress2,
            TertiaryEmail = from.EmailAddress3,
            BusinessPhone = from.Telephone1,
            HomePhone = from.Telephone2,
            OtherPhone = from.Telephone3,
            MobilePhone = from.MobilePhone,
            Fax = from.Fax,
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
            BudgetAmount = MappingHelpers.ToDecimal(from.BudgetAmount),
            BudgetStatus = MapBudgetStatus(from.BudgetStatus),
            EstimatedAmount = MappingHelpers.ToDecimal(from.EstimatedAmount),
            EstimatedCloseDate = from.EstimatedClosedAte,
            LeadQuality = MapLeadQuality(from.LeadQualityCode),
            Need = MapNeed(from.Need),
            PreferredContactMethod = MapPreferredContactMethod(from.PreferredContactMethodCode),
            PurchaseProcess = MapPurchaseProcess(from.PurchaseProcess),
            PurchaseTimeFrame = MapPurchaseTimeFrame(from.PurchaseTimeFrame),
            DoNotEmail = from.DoNotEmail,
            DoNotBulkEmail = from.DoNotBulkEmail,
            DoNotPhone = from.DoNotPhone,
            DoNotFax = from.DoNotFax,
            DoNotPostalMail = from.DoNotPostalMail,
            DoNotSendMarketingMaterial = from.DoNotSendMm,
            Description = from.Description,
            Owner = MappingHelpers.ToOwnerReference(from.OwnerId),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseLead.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseLead.Fields.ModifiedOn)
        });
    }

    private static LeadBudgetStatus? MapBudgetStatus(DataverseModel.BudgetStatus? value)
    {
        return value switch
        {
            DataverseModel.BudgetStatus.NoCommittedBudget => LeadBudgetStatus.NoCommittedBudget,
            DataverseModel.BudgetStatus.MayBuy => LeadBudgetStatus.MayBuy,
            DataverseModel.BudgetStatus.CanBuy => LeadBudgetStatus.CanBuy,
            DataverseModel.BudgetStatus.WillBuy => LeadBudgetStatus.WillBuy,
            _ => null
        };
    }

    private static LeadQuality? MapLeadQuality(DataverseModel.Lead_LeadQualityCode? value)
    {
        return value switch
        {
            DataverseModel.Lead_LeadQualityCode.Hot => LeadQuality.Hot,
            DataverseModel.Lead_LeadQualityCode.Warm => LeadQuality.Warm,
            DataverseModel.Lead_LeadQualityCode.Cold => LeadQuality.Cold,
            _ => null
        };
    }

    private static LeadNeed? MapNeed(DataverseModel.Need? value)
    {
        return value switch
        {
            DataverseModel.Need.MustHave => LeadNeed.MustHave,
            DataverseModel.Need.ShouldHave => LeadNeed.ShouldHave,
            DataverseModel.Need.GoodToHave => LeadNeed.GoodToHave,
            DataverseModel.Need.NoNeed => LeadNeed.NoNeed,
            _ => null
        };
    }

    private static LeadPreferredContactMethod? MapPreferredContactMethod(DataverseModel.Lead_PreferredContactMethodCode? value)
    {
        return value switch
        {
            DataverseModel.Lead_PreferredContactMethodCode.Any => LeadPreferredContactMethod.Any,
            DataverseModel.Lead_PreferredContactMethodCode.Email => LeadPreferredContactMethod.Email,
            DataverseModel.Lead_PreferredContactMethodCode.Phone => LeadPreferredContactMethod.Phone,
            DataverseModel.Lead_PreferredContactMethodCode.Fax => LeadPreferredContactMethod.Fax,
            DataverseModel.Lead_PreferredContactMethodCode.Mail => LeadPreferredContactMethod.Mail,
            _ => null
        };
    }

    private static LeadPurchaseProcess? MapPurchaseProcess(DataverseModel.PurchaseProcess? value)
    {
        return value switch
        {
            DataverseModel.PurchaseProcess.Individual => LeadPurchaseProcess.Individual,
            DataverseModel.PurchaseProcess.Committee => LeadPurchaseProcess.Committee,
            DataverseModel.PurchaseProcess.Unknown => LeadPurchaseProcess.Unknown,
            _ => null
        };
    }

    private static LeadPurchaseTimeFrame? MapPurchaseTimeFrame(DataverseModel.PurchaseTimeFrame? value)
    {
        return value switch
        {
            DataverseModel.PurchaseTimeFrame.Immediate => LeadPurchaseTimeFrame.Immediate,
            DataverseModel.PurchaseTimeFrame.ThisQuarter => LeadPurchaseTimeFrame.ThisQuarter,
            DataverseModel.PurchaseTimeFrame.NextQuarter => LeadPurchaseTimeFrame.NextQuarter,
            DataverseModel.PurchaseTimeFrame.ThisYear => LeadPurchaseTimeFrame.ThisYear,
            DataverseModel.PurchaseTimeFrame.Unknown => LeadPurchaseTimeFrame.Unknown,
            _ => null
        };
    }
}
