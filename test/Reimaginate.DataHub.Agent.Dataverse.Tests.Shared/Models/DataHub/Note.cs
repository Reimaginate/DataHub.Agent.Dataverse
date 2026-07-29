using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;

public sealed class Note : DataHubEntity
{
    public Note()
    {
        entityType = nameof(Note);
    }

    public string? Subject { get; set; }

    public string? Text { get; set; }

    public string? FileName { get; set; }

    public string? MimeType { get; set; }

    public string? DocumentBody { get; set; }

    public bool? IsDocument { get; set; }

    public EntityReference? Owner { get; set; }

    public EntityReference? Regarding { get; set; }
}

public enum ActivityDirection
{
    Incoming,
    Outgoing
}
