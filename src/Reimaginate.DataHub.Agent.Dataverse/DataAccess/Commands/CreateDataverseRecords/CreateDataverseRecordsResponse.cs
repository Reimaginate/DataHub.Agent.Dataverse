namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.CreateDataverseRecords;

public class CreateDataverseRecordsResponse<TDataverseEntity> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public bool HasErrors { get; set; }
    public Dictionary<string, CreateResult<TDataverseEntity>> Results { get; set; } = new();
}