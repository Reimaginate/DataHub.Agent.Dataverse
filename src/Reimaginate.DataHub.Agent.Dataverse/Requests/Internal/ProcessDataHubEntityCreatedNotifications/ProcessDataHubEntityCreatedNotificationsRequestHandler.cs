using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncSpecificDataHubEntities;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataHubEntityCreatedNotifications;

public class ProcessDataHubEntityCreatedNotificationsRequestHandler(IMediator mediator, IOptions<DataverseAgentOptions> dataverseAgentOptions)
    : IHandler<ProcessDataHubEntityCreatedNotificationsRequest, NullResponse>
{
    public async Task<NullResponse> HandleAsync(ProcessDataHubEntityCreatedNotificationsRequest request, CancellationToken cancellationToken)
    {
        var notificationsToProcess = request.Notifications.Where(w => w.DataSource != dataverseAgentOptions.Value.DataSource).ToList();

        var dataHubEntityTypes = notificationsToProcess.GroupBy(g => g.DataHubEntityType);
        foreach (var dataHubEntityType in dataHubEntityTypes)
        {
            var dataHubEntityIds = dataHubEntityType.Select(s => s.DataHubEntityId).ToList();

            var dataTypeEntityType = request.DataHubAssemblyMarker.Assembly.GetExportedTypes().FirstOrDefault(w => w.Name == dataHubEntityType.Key && typeof(DataHubEntity).IsAssignableFrom(w));
            if (dataTypeEntityType == null) continue;
         
            var dataverseTypeName = dataTypeEntityType
                .GetCustomAttributes(typeof(RelatedEntityTypeAttribute), true)
                .Select(s => (RelatedEntityTypeAttribute)s)
                .FirstOrDefault(f => f.DataSource == dataverseAgentOptions.Value.DataSource)?.TypeName;

            if(string.IsNullOrEmpty(dataverseTypeName)) continue;

            var dataverseType = Type.GetType(dataverseTypeName);

            if (dataverseType == null) continue;

            var reqType = typeof(SyncSpecificDataHubEntitiesRequest<,>);
            var genericReq = reqType.MakeGenericType(dataTypeEntityType, dataverseType);

            var req = (IRequest)Activator.CreateInstance(genericReq, dataHubEntityIds, Guid.NewGuid().ToString());

            _ = (await mediator.SendAsync((IRequest)req, cancellationToken)) switch { { IsT1: true } result => throw result.AsT1, { AsT0: var mediatorResultValue } => mediatorResultValue };
        }

        return new NullResponse();
    }
}