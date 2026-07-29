using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Core.Models.Events;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendSyncFailuresToDataHub;

public class SendSyncFailuresToDataHubRequestHandler(IDataHubClient dataHubClient, IOptions<DataverseAgentOptions> dataverseAgentConfig, ITimeService timeService)
    : IHandler<SendSyncFailuresToDataHubRequest, NullResponse>
{
    public async Task<NullResponse> HandleAsync(SendSyncFailuresToDataHubRequest request, CancellationToken cancellationToken)
    {
        var itemsToProcess = new List<SyncEntityResult>(request.Failures);

        while (itemsToProcess.Any())
        {
            var batch = itemsToProcess.Take(5000).ToList();

            await dataHubClient.PostRequestAsync<RegisterSyncFailuresRequest, NullResponse>(new RegisterSyncFailuresRequest()
            {
                SyncFailures = batch.Select(s => new SyncFailure()
                {
                    DataSource = dataverseAgentConfig.Value.DataSource,
                    DataHubEntityType = s.DataHubEntityType,
                    DataHubEntityId = s.DataHubEntityId,
                    SourceEntityType = s.SourceEntityType,
                    SourceEntityId = s.SourceEntityId,
                    AgentId = dataverseAgentConfig.Value.AgentId,
                    FailureReason = s.FailureReason,
                    FailureType = s.FailureReason,
                    Description = s.FailureReason,
                    Timestamp = timeService.Now()
                }).ToList()
            }, cancellationToken);
            

            itemsToProcess.RemoveRange(0, batch.Count);
        }
        
        return new NullResponse();
    }
}