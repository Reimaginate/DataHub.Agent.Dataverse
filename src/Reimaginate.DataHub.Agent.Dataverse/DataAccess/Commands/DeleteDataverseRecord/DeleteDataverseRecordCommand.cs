using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.DeleteDataverseRecord;

public class DeleteDataverseRecordCommand : IRequest<NullResponse>
{
    public string DataverseRecordType { get; set; }
    public Guid DataverseRecordId { get; set; }
}