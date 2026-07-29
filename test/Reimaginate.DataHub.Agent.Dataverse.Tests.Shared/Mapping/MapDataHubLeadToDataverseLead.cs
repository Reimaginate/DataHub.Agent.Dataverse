using Reimaginate.Mapper;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using DataverseLead = DataverseModel.Lead;
using DataHubLead = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Lead;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataHubLeadToDataverseLead : ITypeMapper<DataHubLead, DataverseLead>
{
    public Task<DataverseLead> MapAsync(
        DataHubLead from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var mapped = new DataverseLead
        {
            Subject = from.Subject,
            Salutation = from.Salutation,
            FirstName = from.FirstName,
            MiddleName = from.MiddleName,
            LastName = from.LastName,
            CompanyName = from.CompanyName,
            JobTitle = from.JobTitle,
            WebsiteUrl = from.Website,
            EmailAddress1 = from.Email,
            EmailAddress2 = from.SecondaryEmail,
            EmailAddress3 = from.TertiaryEmail,
            Telephone1 = from.BusinessPhone,
            Telephone2 = from.HomePhone,
            Telephone3 = from.OtherPhone,
            MobilePhone = from.MobilePhone,
            Fax = from.Fax,
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
            BudgetAmount = MappingHelpers.ToMoney(from.BudgetAmount),
            BudgetStatus = MapBudgetStatus(from.BudgetStatus),
            EstimatedAmount = MappingHelpers.ToMoney(from.EstimatedAmount),
            EstimatedClosedAte = from.EstimatedCloseDate,
            LeadQualityCode = MapLeadQuality(from.LeadQuality),
            Need = MapNeed(from.Need),
            PreferredContactMethodCode = MapPreferredContactMethod(from.PreferredContactMethod),
            PurchaseProcess = MapPurchaseProcess(from.PurchaseProcess),
            PurchaseTimeFrame = MapPurchaseTimeFrame(from.PurchaseTimeFrame),
            DoNotEmail = from.DoNotEmail,
            DoNotBulkEmail = from.DoNotBulkEmail,
            DoNotPhone = from.DoNotPhone,
            DoNotFax = from.DoNotFax,
            DoNotPostalMail = from.DoNotPostalMail,
            DoNotSendMm = from.DoNotSendMarketingMaterial,
            Description = from.Description,
            OwnerId = MappingHelpers.ResolveOwner(from.Owner, cache)
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseLead.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.LeadId = dataverseId;
        }

        return Task.FromResult(mapped);
    }

    private static DataverseModel.BudgetStatus? MapBudgetStatus(LeadBudgetStatus? value)
    {
        return value switch
        {
            LeadBudgetStatus.NoCommittedBudget => DataverseModel.BudgetStatus.NoCommittedBudget,
            LeadBudgetStatus.MayBuy => DataverseModel.BudgetStatus.MayBuy,
            LeadBudgetStatus.CanBuy => DataverseModel.BudgetStatus.CanBuy,
            LeadBudgetStatus.WillBuy => DataverseModel.BudgetStatus.WillBuy,
            _ => null
        };
    }

    private static DataverseModel.Lead_LeadQualityCode? MapLeadQuality(LeadQuality? value)
    {
        return value switch
        {
            LeadQuality.Hot => DataverseModel.Lead_LeadQualityCode.Hot,
            LeadQuality.Warm => DataverseModel.Lead_LeadQualityCode.Warm,
            LeadQuality.Cold => DataverseModel.Lead_LeadQualityCode.Cold,
            _ => null
        };
    }

    private static DataverseModel.Need? MapNeed(LeadNeed? value)
    {
        return value switch
        {
            LeadNeed.MustHave => DataverseModel.Need.MustHave,
            LeadNeed.ShouldHave => DataverseModel.Need.ShouldHave,
            LeadNeed.GoodToHave => DataverseModel.Need.GoodToHave,
            LeadNeed.NoNeed => DataverseModel.Need.NoNeed,
            _ => null
        };
    }

    private static DataverseModel.Lead_PreferredContactMethodCode? MapPreferredContactMethod(LeadPreferredContactMethod? value)
    {
        return value switch
        {
            LeadPreferredContactMethod.Any => DataverseModel.Lead_PreferredContactMethodCode.Any,
            LeadPreferredContactMethod.Email => DataverseModel.Lead_PreferredContactMethodCode.Email,
            LeadPreferredContactMethod.Phone => DataverseModel.Lead_PreferredContactMethodCode.Phone,
            LeadPreferredContactMethod.Fax => DataverseModel.Lead_PreferredContactMethodCode.Fax,
            LeadPreferredContactMethod.Mail => DataverseModel.Lead_PreferredContactMethodCode.Mail,
            _ => null
        };
    }

    private static DataverseModel.PurchaseProcess? MapPurchaseProcess(LeadPurchaseProcess? value)
    {
        return value switch
        {
            LeadPurchaseProcess.Individual => DataverseModel.PurchaseProcess.Individual,
            LeadPurchaseProcess.Committee => DataverseModel.PurchaseProcess.Committee,
            LeadPurchaseProcess.Unknown => DataverseModel.PurchaseProcess.Unknown,
            _ => null
        };
    }

    private static DataverseModel.PurchaseTimeFrame? MapPurchaseTimeFrame(LeadPurchaseTimeFrame? value)
    {
        return value switch
        {
            LeadPurchaseTimeFrame.Immediate => DataverseModel.PurchaseTimeFrame.Immediate,
            LeadPurchaseTimeFrame.ThisQuarter => DataverseModel.PurchaseTimeFrame.ThisQuarter,
            LeadPurchaseTimeFrame.NextQuarter => DataverseModel.PurchaseTimeFrame.NextQuarter,
            LeadPurchaseTimeFrame.ThisYear => DataverseModel.PurchaseTimeFrame.ThisYear,
            LeadPurchaseTimeFrame.Unknown => DataverseModel.PurchaseTimeFrame.Unknown,
            _ => null
        };
    }
}
