using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.CreateDataverseRecord;

public class CreateDataverseRecordCommandHandler<TDataverseEntity>(IDataverseDataService idataverseDataService) : IHandler<CreateDataverseRecordCommand<TDataverseEntity>, Guid>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public async Task<Guid> HandleAsync(CreateDataverseRecordCommand<TDataverseEntity> command, CancellationToken cancellationToken)
    {
        var response = await idataverseDataService.CreateAsync(command.DataverseEntity, cancellationToken);
        return response;
    }
}