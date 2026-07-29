using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.Disassociate;

public class DisassociateCommandHandler(ServiceClient serviceClient) : IHandler<DisassociateCommand, ExecuteMultipleResponse>
{
    public async Task<ExecuteMultipleResponse> HandleAsync(DisassociateCommand request, CancellationToken cancellationToken)
    {
        var requestCollection = new OrganizationRequestCollection();
        requestCollection.AddRange(request.Requests);

        var executeMultipleRequest = new ExecuteMultipleRequest()
        {
            Requests = requestCollection,
            Settings = new ExecuteMultipleSettings()
            {
                ContinueOnError = true,
                ReturnResponses = true
            }
        };

        var response = (ExecuteMultipleResponse)await serviceClient.ExecuteAsync(executeMultipleRequest, cancellationToken);
        return response;
    }
}