using Microsoft.Xrm.Sdk.Messages;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.Associate;

public class AssociateCommand : IRequest<ExecuteMultipleResponse>
{
    public List<AssociateRequest> Requests { get; set; }
}