using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.GetDataverseAgentMergeMarker;

public class GetDataverseAgentMergeMarkerRequestHandler(IDataHubClient dataHubClient) : IHandler<GetDataverseAgentMergeMarkerRequest, GetDataverseAgentMergeMarkerResponse>
{
    public async Task<GetDataverseAgentMergeMarkerResponse> HandleAsync(GetDataverseAgentMergeMarkerRequest request, CancellationToken cancellationToken)
    {
        var dataHubRequest = new GetMergeMarkerRequest()
        {
            AgentId = request.AgentId,
            DataSource = request.DataSourceId,
            SourceEntityType = request.EntityType,
            DefaultValue = request.DefaultValue
        };


        var response = await dataHubClient.PostRequestAsync<GetMergeMarkerRequest, GetMergeMarkerResponse>(dataHubRequest, cancellationToken);

        return new GetDataverseAgentMergeMarkerResponse()
        {
            MergeMarker = response.MergeMarker
        };
    }
}