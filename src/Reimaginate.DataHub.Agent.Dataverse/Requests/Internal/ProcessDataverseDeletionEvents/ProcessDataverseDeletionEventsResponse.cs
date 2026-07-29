namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseDeletionEvents;

public class ProcessDataverseDeletionEventsResponse
{
    public bool Success { get; set; }
    public string FailureReason { get; set; }
    public List<string> FailedMessages { get; set; }
}