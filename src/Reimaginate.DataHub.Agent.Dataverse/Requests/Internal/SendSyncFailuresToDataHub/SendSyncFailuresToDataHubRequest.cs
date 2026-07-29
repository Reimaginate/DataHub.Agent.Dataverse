using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendSyncFailuresToDataHub;

public class SendSyncFailuresToDataHubRequest : IRequest<NullResponse>
{
    public SendSyncFailuresToDataHubRequest()
    {

    }

    public SendSyncFailuresToDataHubRequest(List<SyncEntityResult> failures)
    {
        Failures = failures;
    }

    public List<SyncEntityResult> Failures { get; set; }
}