using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecords;

public class UpdateDataverseRecordsCommand : IRequest<UpdateDataverseRecordsResponse<Microsoft.Xrm.Sdk.Entity>>
{
    public Dictionary<string, Microsoft.Xrm.Sdk.Entity> Records { get; set; }
    public bool? DisableRowVersionCheck { get; set; }
    public bool AutoFetchUpdatedEntities { get; set; } = true;
}

public class UpdateDataverseRecordsCommand<TDataverseEntity> : IRequest<UpdateDataverseRecordsResponse<TDataverseEntity>> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public Dictionary<string,TDataverseEntity> Records { get; set; }
    public bool? DisableRowVersionCheck { get; set; }
    public bool AutoFetchUpdatedEntities { get; set; } = true;
}