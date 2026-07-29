namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ResolveResolutionPromises;

public class ResolvedResolutionPromise
{
    public Type DataHubType { get; set; }
    public Type DataverseType { get; set; }
    public Guid DataverseEntityId { get; set; }
    public string DataHubEntityId { get; set; }
}