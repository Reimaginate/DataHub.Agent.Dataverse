using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.Mediator;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecord;

public class UpdateDataverseRecordCommandHandler<TDataverseEntity>(IDataverseDataService idataverseDataService) : IHandler<UpdateDataverseRecordCommand<TDataverseEntity>, Guid>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public async Task<Guid> HandleAsync(UpdateDataverseRecordCommand<TDataverseEntity> command, CancellationToken cancellationToken)
    {
        await idataverseDataService.UpdateAsync(command.DataverseEntity, cancellationToken);
        return command.DataverseEntity.Id;
    }
}