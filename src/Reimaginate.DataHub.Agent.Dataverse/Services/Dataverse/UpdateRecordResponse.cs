namespace Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;

public class UpdateRecordResponse
{
    public Guid EntityId { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; }
}