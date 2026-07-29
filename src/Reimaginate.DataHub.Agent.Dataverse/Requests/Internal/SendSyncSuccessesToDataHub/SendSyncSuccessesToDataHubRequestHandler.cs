using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Core.Models.Events;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendSyncSuccessesToDataHub;

public class SendSyncSuccessesToDataHubRequestHandler(IDataHubClient dataHubClient, IOptions<DataverseAgentOptions> dataverseAgentConfig, ITimeService timeService)
    : IHandler<SendSyncSuccessesToDataHubRequest, NullResponse>
{
    public async Task<NullResponse> HandleAsync(SendSyncSuccessesToDataHubRequest request, CancellationToken cancellationToken)
    {
        var itemsToProcess = new List<SyncEntityResult>(request.Successes);

        while (itemsToProcess.Any())
        {
            var batch = itemsToProcess.Take(5000).ToList();

            await dataHubClient.PostRequestAsync<RegisterSyncSuccessesRequest, NullResponse>(new RegisterSyncSuccessesRequest()
            {
                SyncSuccesses = batch.Select(s => new SyncSuccess()
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