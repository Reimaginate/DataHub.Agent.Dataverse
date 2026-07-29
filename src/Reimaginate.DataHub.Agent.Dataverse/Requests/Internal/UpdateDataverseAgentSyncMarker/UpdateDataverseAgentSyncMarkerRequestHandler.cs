using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.UpdateDataverseAgentSyncMarker;

public class UpdateDataverseAgentSyncMarkerRequestHandler(IDataHubClient dataHubClient) : IHandler<UpdateDataverseAgentSyncMarkerRequest, UpdateDataverseAgentSyncMarkerResponse>
{
    public async Task<UpdateDataverseAgentSyncMarkerResponse> HandleAsync(UpdateDataverseAgentSyncMarkerRequest request, CancellationToken cancellationToken)
    {
        var dataHubRequest = new UpdateSyncMarkerRequest()
        {
            SyncMarker = request.Marker,
            NewValue = request.NewValue,
            RunTime = request.RunTime
        };

        var orchestratorResponse = await dataHubClient.PostRequestAsync<UpdateSyncMarkerRequest, UpdateSyncMarkerResponse>(dataHubRequest, cancellationToken);

        return new UpdateDataverseAgentSyncMarkerResponse()
        {
            ResultingSyncMarker = orchestratorResponse.ResultingSyncMarker
        };
    }
}