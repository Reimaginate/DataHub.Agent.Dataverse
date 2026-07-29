namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.CreateDataverseRecords;

public class CreateResult<TDataverseEntity> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public Guid? EntityId { get; set; }
    public TDataverseEntity ResultingEntity { get; set; }
    public bool Success { get; set; }
    public string FailureReason { get; set; }
}