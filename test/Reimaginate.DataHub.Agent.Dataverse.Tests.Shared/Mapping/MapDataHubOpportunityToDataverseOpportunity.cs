using Reimaginate.Mapper;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using DataverseAccount = DataverseModel.Account;
using DataverseContact = DataverseModel.Contact;
using DataverseOpportunity = DataverseModel.Opportunity;
using DataHubAccount = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account;
using DataHubContact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;
using DataHubOpportunity = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Opportunity;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataHubOpportunityToDataverseOpportunity : ITypeMapper<DataHubOpportunity, DataverseOpportunity>
{
    public Task<DataverseOpportunity> MapAsync(
        DataHubOpportunity from,
        CancellationToken cancellationToken,
        Dictionary<string, object>? cache = null)
    {
        var account = MappingHelpers.ResolveReference<DataHubAccount>(from.Account, DataverseAccount.EntityLogicalName, cache);
        var contact = MappingHelpers.ResolveReference<DataHubContact>(from.Contact, DataverseContact.EntityLogicalName, cache);

        var mapped = new DataverseOpportunity
        {
            Name = from.Name,
            Description = from.Description,
            CurrentSituation = from.CurrentSituation,
            CustomerNeed = from.CustomerNeed,
            ProposedSolution = from.ProposedSolution,
            StepName = from.StepName,
            BudgetAmount = MappingHelpers.ToMoney(from.BudgetAmount),
            BudgetStatus = MapBudgetStatus(from.BudgetStatus),
            CloseProbability = from.CloseProbability,
            EstimatedClosedAte = from.EstimatedCloseDate,
            EstimatedValue = MappingHelpers.ToMoney(from.EstimatedValue),
            OpportunityRatingCode = MapRating(from.Rating),
            PriorityCode = MapPriority(from.Priority),
            PurchaseProcess = MapPurchaseProcess(from.PurchaseProcess),
            PurchaseTimeFrame = MapPurchaseTimeFrame(from.PurchaseTimeFrame),
            CustomerId = account ?? contact,
            ParentAccountId = MappingHelpers.ResolveReference<DataHubAccount>(from.ParentAccount, DataverseAccount.EntityLogicalName, cache),
            ParentContactId = MappingHelpers.ResolveReference<DataHubContact>(from.ParentContact, DataverseContact.EntityLogicalName, cache),
            OwnerId = MappingHelpers.ResolveOwner(from.Owner, cache)
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseOpportunity.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.OpportunityId = dataverseId;
        }

        return Task.FromResult(mapped);
    }

    private static DataverseModel.BudgetStatus? MapBudgetStatus(OpportunityBudgetStatus? value)
    {
        return value switch
        {
            OpportunityBudgetStatus.NoCommittedBudget => DataverseModel.BudgetStatus.NoCommittedBudget,
            OpportunityBudgetStatus.MayBuy => DataverseModel.BudgetStatus.MayBuy,
            OpportunityBudgetStatus.CanBuy => DataverseModel.BudgetStatus.CanBuy,
            OpportunityBudgetStatus.WillBuy => DataverseModel.BudgetStatus.WillBuy,
            _ => null
        };
    }

    private static DataverseModel.Opportunity_OpportunityRatingCode? MapRating(OpportunityRating? value)
    {
        return value switch
        {
            OpportunityRating.Hot => DataverseModel.Opportunity_OpportunityRatingCode.Hot,
            OpportunityRating.Warm => DataverseModel.Opportunity_OpportunityRatingCode.Warm,
            OpportunityRating.Cold => DataverseModel.Opportunity_OpportunityRatingCode.Cold,
            _ => null
        };
    }

    private static DataverseModel.Opportunity_PriorityCode? MapPriority(OpportunityPriority? value)
    {
        return value switch
        {
            OpportunityPriority.DefaultValue => DataverseModel.Opportunity_PriorityCode.DefaultValue,
            _ => null
        };
    }

    private static DataverseModel.PurchaseProcess? MapPurchaseProcess(OpportunityPurchaseProcess? value)
    {
        return value switch
        {
            OpportunityPurchaseProcess.Individual => DataverseModel.PurchaseProcess.Individual,
            OpportunityPurchaseProcess.Committee => DataverseModel.PurchaseProcess.Committee,
            OpportunityPurchaseProcess.Unknown => DataverseModel.PurchaseProcess.Unknown,
            _ => null
        };
    }

    private static DataverseModel.PurchaseTimeFrame? MapPurchaseTimeFrame(OpportunityPurchaseTimeFrame? value)
    {
        return value switch
        {
            OpportunityPurchaseTimeFrame.Immediate => DataverseModel.PurchaseTimeFrame.Immediate,
            OpportunityPurchaseTimeFrame.ThisQuarter => DataverseModel.PurchaseTimeFrame.ThisQuarter,
            OpportunityPurchaseTimeFrame.NextQuarter => DataverseModel.PurchaseTimeFrame.NextQuarter,
            OpportunityPurchaseTimeFrame.ThisYear => DataverseModel.PurchaseTimeFrame.ThisYear,
            OpportunityPurchaseTimeFrame.Unknown => DataverseModel.PurchaseTimeFrame.Unknown,
            _ => null
        };
    }
}
