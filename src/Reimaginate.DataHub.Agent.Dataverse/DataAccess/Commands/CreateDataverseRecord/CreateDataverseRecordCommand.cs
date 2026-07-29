using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.CreateDataverseRecord;

public class CreateDataverseRecordCommand<TDataverseEntity> : IRequest<Guid> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public TDataverseEntity DataverseEntity { get; set; }
}