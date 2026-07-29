using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.GetDataverseAgentSyncMarker;

public class GetDataverseAgentSyncMarkerRequestHandler(IDataHubClient dataHubClient) : IHandler<GetDataverseAgentSyncMarkerRequest, GetDataverseAgentSyncMarkerResponse>
{
    public async Task<GetDataverseAgentSyncMarkerResponse> HandleAsync(GetDataverseAgentSyncMarkerRequest request, CancellationToken cancellationToken)
    {
        var dataHubRequest = new GetSyncMarkerRequest()
        {
            AgentId = request.AgentId,
            DataSource = request.DataSourceId,
            DataHubEntityType = request.EntityType,
            DefaultValue = request.DefaultValue
        };


        var orchestratorResponse = await dataHubClient.PostRequestAsync<GetSyncMarkerRequest, GetSyncMarkerResponse>(dataHubRequest, cancellationToken);

        return new GetDataverseAgentSyncMarkerResponse()
        {
            SyncMarker = orchestratorResponse.SyncMarker
        };
    }
}