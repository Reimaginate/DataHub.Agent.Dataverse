using Microsoft.Xrm.Sdk;

namespace Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;

public class OperationResult
{
    public bool Success { get; set; }
    public Exception Exception { get; set; }
    public Guid? ResultingEntityId { get; set; }
    public Entity SourceRecord { get; set; }
}