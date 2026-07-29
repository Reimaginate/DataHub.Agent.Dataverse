namespace Reimaginate.DataHub.Agent.Dataverse.Models;

public class DeletionEvent
{
    public string MessageId { get; set; }
    public string EntityType { get; set; }
    public string EntityId { get; set; }
}