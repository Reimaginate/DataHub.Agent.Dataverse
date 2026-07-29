using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class Case : DataHubEntity
{
    public Case()
    {
        entityType = nameof(Case);
    }

    public string? Title { get; set; }

    public string? TicketNumber { get; set; }

    public string? Description { get; set; }

    public CasePriority? Priority { get; set; }

    public CaseType? CaseType { get; set; }

    public CaseOrigin? CaseOrigin { get; set; }

    public EntityReference? CustomerAccount { get; set; }

    public EntityReference? CustomerContact { get; set; }

    public EntityReference? PrimaryContact { get; set; }

    public EntityReference? ResponsibleContact { get; set; }

    public EntityReference? ParentCase { get; set; }

    public EntityReference? Subject { get; set; }

    public EntityReference? Entitlement { get; set; }

    public EntityReference? Sla { get; set; }

    public EntityReference? Product { get; set; }

    public EntityReference? Owner { get; set; }
}

public enum CasePriority
{
    High,
    Normal,
    Low
}

public enum CaseType
{
    Question,
    Problem,
    Request
}

public enum CaseOrigin
{
    Phone,
    Email,
    Web
}
