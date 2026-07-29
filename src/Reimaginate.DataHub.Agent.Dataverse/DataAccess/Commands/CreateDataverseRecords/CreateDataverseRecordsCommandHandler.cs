using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.CreateDataverseRecords;

public class CreateDataverseRecordsCommandHandler<TDataverseEntity>(IDataverseDataService idataverseDataService) : IHandler<CreateDataverseRecordsCommand<TDataverseEntity>, CreateDataverseRecordsResponse<TDataverseEntity>>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public async Task<CreateDataverseRecordsResponse<TDataverseEntity>> HandleAsync(CreateDataverseRecordsCommand<TDataverseEntity> command, CancellationToken cancellationToken)
    {
        var createResponses = await idataverseDataService.CreateAsync(command.Records, cancellationToken: cancellationToken);

        return new CreateDataverseRecordsResponse<TDataverseEntity>()
        {
            HasErrors = createResponses.Any(a => !a.Value.Success),
            Results = createResponses.ToDictionary(k=>k.Key, v =>
            {
                TDataverseEntity resultingEntity = null;

                if (v.Value.Success)
                {
                    var request = command.Records[v.Key];
                    request.Id = v.Value.EntityId!.Value;
                    resultingEntity = request;
                }

                return new CreateResult<TDataverseEntity>()
                {
                    Success = v.Value.Success,
                    EntityId = v.Value.EntityId,
                    FailureReason = v.Value.Error,
                    ResultingEntity = resultingEntity
                };
            })
        };
    }
}