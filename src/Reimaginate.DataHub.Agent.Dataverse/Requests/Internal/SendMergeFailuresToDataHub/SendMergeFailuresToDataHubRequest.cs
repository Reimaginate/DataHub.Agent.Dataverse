using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendMergeFailuresToDataHub;

public class SendMergeFailuresToDataHubRequest : IRequest<NullResponse>
{
    public SendMergeFailuresToDataHubRequest()
    {

    }

    public SendMergeFailuresToDataHubRequest(List<MergeEntityResult> failures)
    {
        Failures = failures;
    }

    public List<MergeEntityResult> Failures { get; set; }
}