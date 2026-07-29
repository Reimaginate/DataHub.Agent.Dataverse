using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SyncEntities;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mapper;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncDependencyDataHubEntities;

public class SyncDependencyDataHubEntitiesRequestHandler<TDataHubEntity, TDataverseEntity>(IOptions<DataverseAgentOptions> config, IDataHubClient dataHubClient, IMediator mediator, IMapper mapper)
    : IHandler<SyncDependencyDataHubEntitiesRequest<TDataHubEntity, TDataverseEntity>, ProcessSyncResponse>
    where TDataHubEntity : DataHubEntity
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    private readonly IOptions<DataverseAgentOptions> _config = config;
    private readonly IDataHubClient _dataHubClient = dataHubClient;
    private readonly IMapper _mapper = mapper;

    public async Task<ProcessSyncResponse> HandleAsync(SyncDependencyDataHubEntitiesRequest<TDataHubEntity, TDataverseEntity> request, CancellationToken cancellationToken)
    {
        var response = (await mediator.TrySend<ProcessSyncResponse>(new SyncEntitiesRequest<TDataHubEntity, TDataverseEntity>()
        {
            CorrelationId = request.CorrelationId,
            EntityIds = request.EntityIds,
            DependencyTree = request.DependencyTree,
            ResolutionPromises = request.ResolutionPromises
        }, cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        return response;
    }
}