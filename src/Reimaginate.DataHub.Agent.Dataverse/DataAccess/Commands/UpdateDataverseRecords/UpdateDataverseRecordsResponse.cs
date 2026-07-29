namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecords;

public class UpdateDataverseRecordsResponse<TDataverseEntity> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public bool HasErrors { get; set; }
    public Dictionary<string, UpdateResult<TDataverseEntity>> Results { get; set; } = new();
}