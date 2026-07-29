using Microsoft.Xrm.Sdk.Messages;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.Disassociate;

public class DisassociateCommand : IRequest<ExecuteMultipleResponse>
{
    public List<DisassociateRequest> Requests { get; set; }

}