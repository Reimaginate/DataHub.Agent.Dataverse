namespace Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;

public class CreateRecordResponse
{
    public Guid? EntityId { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; }
    
}