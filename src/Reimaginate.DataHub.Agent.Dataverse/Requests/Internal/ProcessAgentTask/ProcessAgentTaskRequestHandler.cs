using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncSpecificDataHubEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Agent;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessAgentTask;

public class ProcessAgentTaskRequestHandler<TDataHubAssemblyMarker>(IMediator mediator, IOptions<DataverseAgentOptions> agentConfig) : IHandler<ProcessAgentTaskRequest<TDataHubAssemblyMarker>, JObject>
    where TDataHubAssemblyMarker : DataHubEntity
{
    public async Task<JObject> HandleAsync(ProcessAgentTaskRequest<TDataHubAssemblyMarker> request, CancellationToken cancellationToken) 
    {
        switch (request.AgentRequest.Value<string>(nameof(AgentRequest.RequestType)))
        {
            case nameof(MergeEntitiesRequest):
                var mergeEntitiesResponse = await ProcessMergeEntitiesResponse(request, cancellationToken);
                return JObject.FromObject(mergeEntitiesResponse);

            case nameof(SyncEntitiesRequest):
                var syncEntitiesResponse = await ProcessSyncEntitiesResponse(request, cancellationToken);
                return JObject.FromObject(syncEntitiesResponse);


            default:
                throw new NotImplementedException();

        }
    }

    private async Task<MergeEntitiesResponse> ProcessMergeEntitiesResponse(ProcessAgentTaskRequest<TDataHubAssemblyMarker> request, CancellationToken cancellationToken)
    {
        var mergeEntitiesRequest = request.AgentRequest.ToObject<MergeEntitiesRequest>();

        var dataTypeEntityType = typeof(TDataHubAssemblyMarker).Assembly.GetExportedTypes().FirstOrDefault(w => string.Equals(w.Name, mergeEntitiesRequest.DataHubEntityType, StringComparison.CurrentCultureIgnoreCase) && typeof(DataHubEntity).IsAssignableFrom(w));

        var dataverseTypeName = dataTypeEntityType!
            .GetCustomAttributes(typeof(RelatedEntityTypeAttribute), true)
            .Select(s => (RelatedEntityTypeAttribute)s)
            .First(f => f.DataSource == agentConfig.Value.DataSource)?.TypeName;

        var dataverseType = Type.GetType(dataverseTypeName);

        var reqType = typeof(MergeSpecificDataverseEntitiesRequest<,>);
        var genericReq = reqType.MakeGenericType(dataverseType, dataTypeEntityType);

        var req = (IRequest)Activator.CreateInstance(genericReq, mergeEntitiesRequest.EntityIds.Where(w=>w != null).Select(s => new Guid(s)).ToList(), request.CorrelationId);

        var response = (ProcessMergeResponse)((await mediator.SendAsync(req, cancellationToken)) switch { { IsT1: true } result => throw result.AsT1, { AsT0: var mediatorResultValue } => mediatorResultValue });
        var ret = new MergeEntitiesResponse()
        {
            Success = true,
            //Results = response.Results
        };
        return ret;
    }

    private async Task<SyncEntitiesResponse> ProcessSyncEntitiesResponse(ProcessAgentTaskRequest<TDataHubAssemblyMarker> request, CancellationToken cancellationToken)
    {
        var syncEntitiesRequest = request.AgentRequest.ToObject<SyncEntitiesRequest>();

        var dataTypeEntityType = typeof(TDataHubAssemblyMarker).Assembly.GetExportedTypes().FirstOrDefault(w => string.Equals(w.Name, syncEntitiesRequest.DataHubEntityType, StringComparison.CurrentCultureIgnoreCase) && typeof(DataHubEntity).IsAssignableFrom(w));

        var dataverseTypeName = dataTypeEntityType!
            .GetCustomAttributes(typeof(RelatedEntityTypeAttribute), true)
            .Select(s => (RelatedEntityTypeAttribute)s)
            .First(f => f.DataSource == agentConfig.Value.DataSource)?.TypeName;

        var dataverseType = Type.GetType(dataverseTypeName);

        var reqType = typeof(SyncSpecificDataHubEntitiesRequest<,>);
        var genericReq = reqType.MakeGenericType(dataTypeEntityType, dataverseType);

        var req = (IRequest)Activator.CreateInstance(genericReq, syncEntitiesRequest.DataHubEntityIds, request.CorrelationId);

        var response = (ProcessSyncResponse)((await mediator.SendAsync(req, cancellationToken)) switch { { IsT1: true } result => throw result.AsT1, { AsT0: var mediatorResultValue } => mediatorResultValue });
        var ret = new SyncEntitiesResponse()
        {
            Success = true,
            //Results = response.Results
        };
        return ret;
    }
}
