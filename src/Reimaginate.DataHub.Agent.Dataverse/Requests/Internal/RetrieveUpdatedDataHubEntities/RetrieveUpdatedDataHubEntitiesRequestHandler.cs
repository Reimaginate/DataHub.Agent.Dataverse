using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Helpers;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.RetrieveUpdatedDataHubEntities;

public class RetrieveUpdatedDataHubEntitiesRequestHandler<TDataHubEntity>(IDataHubClient dataHubClient, IOptions<DataverseAgentOptions> dataverseAgentConfig)
    : IHandler<RetrieveUpdatedDataHubEntitiesRequest<TDataHubEntity>, RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity>>
    where TDataHubEntity : DataHubEntity
{
    private readonly IOptions<DataverseAgentOptions> _dataverseAgentConfig = dataverseAgentConfig;

    public async Task<RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity>> HandleAsync(RetrieveUpdatedDataHubEntitiesRequest<TDataHubEntity> request, CancellationToken cancellationToken)
    {
        var req = new GetUpdatedDataHubEntitiesRequest()
        {
            EntityType = typeof(TDataHubEntity).Name,
            FromDateTime = request.FromDateTime,
            ContinuationToken = request.ContinuationToken,
            PageSize = request.BatchSize ?? 500,
            Select = "x.id,x.lastUpdated"
        };

        var ret = await dataHubClient.PostRequestAsync<GetUpdatedDataHubEntitiesRequest, GetDataHubEntitiesResponse>(req, cancellationToken);

        return new RetrieveUpdatedDataHubEntitiesResponse<TDataHubEntity>()
        {
            Results = ret.Results.Select(s => s.ToObjectIgnoreErrors<TDataHubEntity>()).ToList(),
            ResultCount = ret.ResultCount,
            MoreResultsAvailable = ret.MoreResultsAvailable,
            ContinuationToken = ret.ContinuationToken
        };
    }
}