namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetSpecificDataverseEntities;

public class GetSpecificDataverseEntitiesResponse<TDataverseEntity> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public bool Success { get; set; }
    public string FailureReason { get; set; }
    public List<TDataverseEntity> Results { get; set; }
    public List<Guid> NotFound { get; set; }
}