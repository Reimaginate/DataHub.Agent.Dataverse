using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.CreateDataverseRecords;

public class CreateDataverseRecordsCommand<TDataverseEntity> : IRequest<CreateDataverseRecordsResponse<TDataverseEntity>> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public Dictionary<string, TDataverseEntity> Records { get; set; }
    public bool RetrieveResultingRecords { get; set; }
}