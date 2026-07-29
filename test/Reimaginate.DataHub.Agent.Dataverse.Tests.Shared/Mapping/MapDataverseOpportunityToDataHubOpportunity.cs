using Reimaginate.Mapper;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using DataverseAccount = DataverseModel.Account;
using DataverseContact = DataverseModel.Contact;
using DataverseOpportunity = DataverseModel.Opportunity;
using DataHubAccount = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account;
using DataHubContact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;
using DataHubOpportunity = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Opportunity;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseOpportunityToDataHubOpportunity : ITypeMapper<DataverseOpportunity, DataHubOpportunity>
{
    public Task<DataHubOpportunity> MapAsync(
        DataverseOpportunity from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var customerAccount = from.CustomerId?.LogicalName == DataverseAccount.EntityLogicalName
            ? MappingHelpers.ToExternalReference<DataHubAccount>(from.CustomerId, DataverseAccount.EntityLogicalName)
            : null;

        var customerContact = from.CustomerId?.LogicalName == DataverseContact.EntityLogicalName
            ? MappingHelpers.ToExternalReference<DataHubContact>(from.CustomerId, DataverseContact.EntityLogicalName)
            : null;

        var parentContact = MappingHelpers.ToExternalReference<DataHubContact>(from.ParentContactId, DataverseContact.EntityLogicalName);

        return Task.FromResult(new DataHubOpportunity
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseOpportunity.EntityLogicalName, from.Id),
            Name = from.Name,
            Description = from.Description,
            CurrentSituation = from.CurrentSituation,
            CustomerNeed = from.CustomerNeed,
            ProposedSolution = from.ProposedSolution,
            StepName = from.StepName,
            BudgetAmount = MappingHelpers.ToDecimal(from.BudgetAmount),
            BudgetStatus = MapBudgetStatus(from.BudgetStatus),
            CloseProbability = from.CloseProbability,
            EstimatedCloseDate = from.EstimatedClosedAte,
            EstimatedValue = MappingHelpers.ToDecimal(from.EstimatedValue),
            Rating = MapRating(from.OpportunityRatingCode),
            Priority = MapPriority(from.PriorityCode),
            PurchaseProcess = MapPurchaseProcess(from.PurchaseProcess),
            PurchaseTimeFrame = MapPurchaseTimeFrame(from.PurchaseTimeFrame),
            Account = customerAccount,
            Contact = customerContact ?? parentContact,
            ParentAccount = MappingHelpers.ToExternalReference<DataHubAccount>(from.ParentAccountId, DataverseAccount.EntityLogicalName),
            ParentContact = parentContact,
            Owner = MappingHelpers.ToOwnerReference(from.OwnerId),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseOpportunity.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseOpportunity.Fields.ModifiedOn)
        });
    }

    private static OpportunityBudgetStatus? MapBudgetStatus(DataverseModel.BudgetStatus? value)
    {
        return value switch
        {
            DataverseModel.BudgetStatus.NoCommittedBudget => OpportunityBudgetStatus.NoCommittedBudget,
            DataverseModel.BudgetStatus.MayBuy => OpportunityBudgetStatus.MayBuy,
            DataverseModel.BudgetStatus.CanBuy => OpportunityBudgetStatus.CanBuy,
            DataverseModel.BudgetStatus.WillBuy => OpportunityBudgetStatus.WillBuy,
            _ => null
        };
    }

    private static OpportunityRating? MapRating(DataverseModel.Opportunity_OpportunityRatingCode? value)
    {
        return value switch
        {
            DataverseModel.Opportunity_OpportunityRatingCode.Hot => OpportunityRating.Hot,
            DataverseModel.Opportunity_OpportunityRatingCode.Warm => OpportunityRating.Warm,
            DataverseModel.Opportunity_OpportunityRatingCode.Cold => OpportunityRating.Cold,
            _ => null
        };
    }

    private static OpportunityPriority? MapPriority(DataverseModel.Opportunity_PriorityCode? value)
    {
        return value switch
        {
            DataverseModel.Opportunity_PriorityCode.DefaultValue => OpportunityPriority.DefaultValue,
            _ => null
        };
    }

    private static OpportunityPurchaseProcess? MapPurchaseProcess(DataverseModel.PurchaseProcess? value)
    {
        return value switch
        {
            DataverseModel.PurchaseProcess.Individual => OpportunityPurchaseProcess.Individual,
            DataverseModel.PurchaseProcess.Committee => OpportunityPurchaseProcess.Committee,
            DataverseModel.PurchaseProcess.Unknown => OpportunityPurchaseProcess.Unknown,
            _ => null
        };
    }

    private static OpportunityPurchaseTimeFrame? MapPurchaseTimeFrame(DataverseModel.PurchaseTimeFrame? value)
    {
        return value switch
        {
            DataverseModel.PurchaseTimeFrame.Immediate => OpportunityPurchaseTimeFrame.Immediate,
            DataverseModel.PurchaseTimeFrame.ThisQuarter => OpportunityPurchaseTimeFrame.ThisQuarter,
            DataverseModel.PurchaseTimeFrame.NextQuarter => OpportunityPurchaseTimeFrame.NextQuarter,
            DataverseModel.PurchaseTimeFrame.ThisYear => OpportunityPurchaseTimeFrame.ThisYear,
            DataverseModel.PurchaseTimeFrame.Unknown => OpportunityPurchaseTimeFrame.Unknown,
            _ => null
        };
    }
}
