using Microsoft.Xrm.Sdk;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.DeleteDataverseRecord;

public class DeleteDataverseRecordCommandHandler(IDataverseDataService idataverseDataService) : IHandler<DeleteDataverseRecordCommand, NullResponse>
{
    public async Task<NullResponse> HandleAsync(DeleteDataverseRecordCommand command, CancellationToken cancellationToken)
    {
        var entityRef = new EntityReference(command.DataverseRecordType, command.DataverseRecordId);
        await idataverseDataService.DeleteAsync(entityRef, cancellationToken);
        return new NullResponse();
    }
}