using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Core.Models.Events;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendMergeSuccessesToDataHub;

public class SendMergeSuccessesToDataHubRequestHandler(IDataHubClient dataHubClient, IOptions<DataverseAgentOptions> dataverseAgentConfig, ITimeService timeService)
    : IHandler<SendMergeSuccessesToDataHubRequest, NullResponse>
{
    public async Task<NullResponse> HandleAsync(SendMergeSuccessesToDataHubRequest request, CancellationToken cancellationToken)
    {
        var itemsToProcess = new List<MergeEntityResult>(request.Successes);

        while (itemsToProcess.Any())
        {
            var batch = itemsToProcess.Take(5000).ToList();

            await dataHubClient.PostRequestAsync<RegisterMergeSuccessesRequest, NullResponse>(new RegisterMergeSuccessesRequest()
            {
                MergeSuccesses = batch.Select(s => new MergeSuccess()
                {
                    AgentId = dataverseAgentConfig.Value.AgentId,
                    DataSource = dataverseAgentConfig.Value.DataSource,
                    DataHubEntityType = s.DataHubEntityType,
                    DataHubEntityId = s.DataHubEntityId,
                    SourceEntityType = s.SourceEntityType,
                    SourceEntityId = s.SourceEntityId,
                    Timestamp = timeService.Now()
                }).ToList()
            }, cancellationToken);


            itemsToProcess.RemoveRange(0, batch.Count);
        }

        return new NullResponse();
    }
}