using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.UpdateDataverseAgentMergeMarker;

public class UpdateDataverseAgentMergeMarkerRequestHandler(IDataHubClient dataHubClient) : IHandler<UpdateDataverseAgentMergeMarkerRequest, UpdateDataverseAgentMergeMarkerResponse>
{
    public async Task<UpdateDataverseAgentMergeMarkerResponse> HandleAsync(UpdateDataverseAgentMergeMarkerRequest request, CancellationToken cancellationToken)
    {
        var updateMergeMarkerRequest = new UpdateMergeMarkerRequest()
        {
            MergeMarker = request.Marker,
            NewValue = request.NewValue,
            RunTime = request.RunTime
        };

        var response = await dataHubClient.PostRequestAsync<UpdateMergeMarkerRequest, UpdateMergeMarkerResponse>(updateMergeMarkerRequest, cancellationToken);

        return new UpdateDataverseAgentMergeMarkerResponse()
        {
            ResultingMergeMarker = response.ResultingMergeMarker
        };
    }
}