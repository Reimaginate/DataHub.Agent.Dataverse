using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecord;

public class UpdateDataverseRecordCommand<TDataverseEntity> : IRequest<Guid> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public TDataverseEntity DataverseEntity { get; set; }
}