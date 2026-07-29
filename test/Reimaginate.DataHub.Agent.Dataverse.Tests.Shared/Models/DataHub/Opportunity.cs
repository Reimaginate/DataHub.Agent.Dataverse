using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class Opportunity : DataHubEntity
{
    public Opportunity()
    {
        entityType = nameof(Opportunity);
    }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? CurrentSituation { get; set; }

    public string? CustomerNeed { get; set; }

    public string? ProposedSolution { get; set; }

    public string? StepName { get; set; }

    public decimal? BudgetAmount { get; set; }

    public OpportunityBudgetStatus? BudgetStatus { get; set; }

    public int? CloseProbability { get; set; }

    public DateTime? EstimatedCloseDate { get; set; }

    public decimal? EstimatedValue { get; set; }

    public OpportunityRating? Rating { get; set; }

    public OpportunityPriority? Priority { get; set; }

    public OpportunityPurchaseProcess? PurchaseProcess { get; set; }

    public OpportunityPurchaseTimeFrame? PurchaseTimeFrame { get; set; }

    public EntityReference? Account { get; set; }

    public EntityReference? Contact { get; set; }

    public EntityReference? ParentAccount { get; set; }

    public EntityReference? ParentContact { get; set; }

    public EntityReference? Owner { get; set; }
}

public enum OpportunityBudgetStatus
{
    NoCommittedBudget,
    MayBuy,
    CanBuy,
    WillBuy
}

public enum OpportunityRating
{
    Hot,
    Warm,
    Cold
}

public enum OpportunityPriority
{
    DefaultValue
}

public enum OpportunityPurchaseProcess
{
    Individual,
    Committee,
    Unknown
}

public enum OpportunityPurchaseTimeFrame
{
    Immediate,
    ThisQuarter,
    NextQuarter,
    ThisYear,
    Unknown
}
