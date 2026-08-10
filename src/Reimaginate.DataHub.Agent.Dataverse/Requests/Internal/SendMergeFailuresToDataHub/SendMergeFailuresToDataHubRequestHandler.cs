using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Services.TimeService;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Core.Models.Events;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendMergeFailuresToDataHub;

public class SendMergeFailuresToDataHubRequestHandler(IDataHubClient dataHubClient, IOptions<DataverseAgentOptions> dataverseAgentConfig, ITimeService timeService)
    : IHandler<SendMergeFailuresToDataHubRequest, NullResponse>
{
    public async Task<NullResponse> HandleAsync(SendMergeFailuresToDataHubRequest request, CancellationToken cancellationToken)
    {
        var itemsToProcess = new List<MergeEntityResult>(request.Failures);

        while (itemsToProcess.Any())
        {
            var batch = itemsToProcess.Take(5000).ToList();

            await dataHubClient.PostRequestAsync<RegisterMergeFailuresRequest, NullResponse>(new RegisterMergeFailuresRequest()
            {
                MergeFailures = batch.Select(s =>
                {
                    var failureType = string.IsNullOrWhiteSpace(s.MergeOutcome)
                        ? MergeOutcomes.MergeFailed
                        : s.MergeOutcome;
                    var failureReason = string.IsNullOrWhiteSpace(s.FailureReason)
                        ? "The merge operation did not provide a failure reason."
                        : s.FailureReason;

                    return new MergeFailure()
                    {
                        DataSource = dataverseAgentConfig.Value.DataSource,
                        DataHubEntityType = s.DataHubEntityType,
                        DataHubEntityId = s.DataHubEntityId,
                        SourceEntityType = s.SourceEntityType,
                        SourceEntityId = s.SourceEntityId,
                        AgentId = dataverseAgentConfig.Value.AgentId,
                        FailureType = failureType,
                        FailureReason = failureReason,
                        Description = failureReason,
                        Timestamp = timeService.Now()
                    };
                }).ToList()
            }, cancellationToken);


            itemsToProcess.RemoveRange(0, batch.Count);
        }
        
        return new NullResponse();
    }
}
