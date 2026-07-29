namespace Reimaginate.DataHub.Agent.Dataverse.Models;

public class CreateUpdateEvent
{
    public DataverseEventInfo DataverseEventInfo { get; set; }
    public string EntityType { get; set; }
    public string EntityId { get; set; }
   
}