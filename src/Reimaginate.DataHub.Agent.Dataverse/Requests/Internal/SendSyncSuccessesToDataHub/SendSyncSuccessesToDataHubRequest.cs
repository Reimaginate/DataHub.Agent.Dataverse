using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendSyncSuccessesToDataHub;

public class SendSyncSuccessesToDataHubRequest : IRequest<NullResponse>
{
    public SendSyncSuccessesToDataHubRequest()
    {

    }

    public SendSyncSuccessesToDataHubRequest(List<SyncEntityResult> successes)
    {
        Successes = successes;
    }

    public List<SyncEntityResult> Successes { get; set; }
}