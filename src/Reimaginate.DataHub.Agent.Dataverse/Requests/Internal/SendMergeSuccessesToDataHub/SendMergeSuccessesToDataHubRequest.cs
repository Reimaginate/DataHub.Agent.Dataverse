using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendMergeSuccessesToDataHub;

public class SendMergeSuccessesToDataHubRequest : IRequest<NullResponse>
{
    public SendMergeSuccessesToDataHubRequest()
    {

    }

    public SendMergeSuccessesToDataHubRequest(List<MergeEntityResult> successes)
    {
        Successes = successes;
    }

    public List<MergeEntityResult> Successes { get; set; }
}