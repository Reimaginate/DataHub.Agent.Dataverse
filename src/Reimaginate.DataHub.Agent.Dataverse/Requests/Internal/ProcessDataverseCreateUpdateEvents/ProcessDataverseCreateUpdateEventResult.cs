using Reimaginate.DataHub.Agent.Dataverse.Models;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseCreateUpdateEvents;

public class ProcessDataverseCreateUpdateEventResult
{
    public DataverseEventInfo DataverseEventInfo { get; set; }
    public bool Success { get; set; }
    public string FailureReason { get; set; }

}