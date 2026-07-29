using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendSyncFailuresToDataHub;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendSyncSuccessesToDataHub;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SyncEntities;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using Reimaginate.ProcessingLockService;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncSpecificDataHubEntities;

public class SyncSpecificDataHubEntitiesRequestHandler<TDataHubEntity, TDataverseEntity>(IOptions<DataverseAgentOptions> dataverseAgentConfig, IDataHubClient dataHubClient, IMediator mediator, IMapper mapper, IProcessingLockService processingLockService)
    : IHandler<SyncSpecificDataHubEntitiesRequest<TDataHubEntity, TDataverseEntity>, ProcessSyncResponse>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
    where TDataHubEntity : DataHubEntity
{
    private readonly IOptions<DataverseAgentOptions> _dataverseAgentConfig = dataverseAgentConfig;
    private readonly IDataHubClient _dataHubClient = dataHubClient;
    private readonly IMapper _mapper = mapper;
    private readonly IProcessingLockService _processingLockService = processingLockService;

    public async Task<ProcessSyncResponse> HandleAsync(SyncSpecificDataHubEntitiesRequest<TDataHubEntity, TDataverseEntity> request, CancellationToken cancellationToken)
    {
        if (request.EntityIds.Count > 5000) throw new Exception("SyncSpecificDataHubEntitiesRequest supports a maximum of 5000 records per call");

        request.CorrelationId ??= Guid.NewGuid().ToString();

        var results = new List<SyncEntityResult>();

        ProcessSyncResponse response = null;

        try
        {
            response = (await mediator.TrySend<ProcessSyncResponse>(new SyncEntitiesRequest<TDataHubEntity, TDataverseEntity>()
            {
                CorrelationId = request.CorrelationId,
                EntityIds = request.EntityIds,
            }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
            results.AddRange(response.Results);
        }
        catch (Exception ex)
        {
            response = new ProcessSyncResponse()
            {
                Results = request.EntityIds.Select(entityId => new SyncEntityResult()
                {
                    DataSource = "Dataverse",
                    DataHubEntityType = typeof(TDataHubEntity).Name,
                    DataHubEntityId = entityId,
                    SyncOutcome = SyncOutcomes.SyncFailed,
                    FailureReason = ex.Message

                }).ToList()
            };
        }


        #region Register sync successes and failures with the Data Hub

        var failures = results.Where(w => SyncOutcomes.IsFailure(w.SyncOutcome)).ToList();
        if (failures.Any()) _ = (await mediator.TrySend<NullResponse>(new SendSyncFailuresToDataHubRequest(failures), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        var successes = results.Where(w => w.SyncOutcome != SyncOutcomes.SyncFailed).ToList();
        if (successes.Any()) _ = (await mediator.TrySend<NullResponse>(new SendSyncSuccessesToDataHubRequest(successes), cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        #endregion

        return response;
    }
}