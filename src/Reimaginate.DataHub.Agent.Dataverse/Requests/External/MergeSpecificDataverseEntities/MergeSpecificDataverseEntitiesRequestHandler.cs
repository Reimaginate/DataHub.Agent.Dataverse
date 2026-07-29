using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendMergeFailuresToDataHub;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendMergeSuccessesToDataHub;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mapper;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeSpecificDataverseEntities;

public class MergeSpecificDataverseEntitiesRequestHandler<TDataverseEntity, TDataHubEntity>(IOptions<DataverseAgentOptions> config, IDataHubClient dataHubClient, IMediator mediator, IMapper mapper)
    : IHandler<MergeSpecificDataverseEntitiesRequest<TDataverseEntity, TDataHubEntity>, ProcessMergeResponse>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
    where TDataHubEntity : DataHubEntity
{
    public async Task<ProcessMergeResponse> HandleAsync(MergeSpecificDataverseEntitiesRequest<TDataverseEntity, TDataHubEntity> request, CancellationToken cancellationToken)
    {
        request.CorrelationId ??= Guid.NewGuid().ToString();

        var idsToProcess = new List<Guid>(request.EntityIds);
        var results = new List<MergeEntityResult>();


        while (idsToProcess.Any())
        {
            var batch = idsToProcess.Take(5000).ToList();

            var response = (await mediator.TrySend<ProcessMergeResponse>(new MergeEntitiesRequest<TDataverseEntity, TDataHubEntity>()
            {
                CorrelationId = request.CorrelationId,
                DataverseEntityIds = batch,
                ForceUpdate = request.ForceUpdate
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            #region Register sync successes and failures with the Data Hub

            var failures = response.Results.Where(w => MergeOutcomes.IsFailure(w.MergeOutcome)).ToList();
            if (failures.Any()) _ = (await mediator.TrySend<NullResponse>(new SendMergeFailuresToDataHubRequest(failures), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            var successes = response.Results.Where(w => MergeOutcomes.IsSuccess(w.MergeOutcome)).ToList();
            if (successes.Any()) _ = (await mediator.TrySend<NullResponse>(new SendMergeSuccessesToDataHubRequest(successes), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

            #endregion

            results.AddRange(response.Results);

            idsToProcess.RemoveRange(0, batch.Count);
        }

        return new ProcessMergeResponse()
        {
            Results = results
        };
    }
}