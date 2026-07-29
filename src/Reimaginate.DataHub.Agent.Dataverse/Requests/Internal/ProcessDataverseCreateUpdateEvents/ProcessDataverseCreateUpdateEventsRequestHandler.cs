using System.Diagnostics;
using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeSpecificDataverseEntities;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseCreateUpdateEvents;

public class ProcessDataverseCreateUpdateEventsRequestHandler(IMediator mediator, IOptions<DataverseAgentOptions> dataverseAgentOptions)
    : IHandler<ProcessDataverseCreateUpdateEventsRequest, ProcessDataverseCreateUpdateEventsResponse>
{
    public async Task<ProcessDataverseCreateUpdateEventsResponse> HandleAsync(ProcessDataverseCreateUpdateEventsRequest request, CancellationToken cancellationToken)
    {
        var results = new List<ProcessDataverseCreateUpdateEventResult>();
        
        var entityTypeGroups = request.Events.GroupBy(g => g.EntityType);
        foreach (var entityTypeGroup in entityTypeGroups)
        {
            try
            {
                var dataTypeEntityType = request.DataHubAssemblyMarker.Assembly.GetExportedTypes().FirstOrDefault(dhType =>
                {
                    var atts = dhType.GetCustomAttributes(typeof(RelatedEntityTypeAttribute), true);
                    return atts.Any(att =>
                    {
                        try
                        {
                            var ret = (RelatedEntityTypeAttribute)att;
                            var dataverseType = Type.GetType(ret.TypeName)!;
                            if (dataverseType == null)
                            {
                                //throw new Exception($"Dataverse type not found for {ret.TypeName}. This may be due to a misconfiguration of the RelatedEntityType attribute or that the Dataverse entity model does not exist.");
                                return false;
                            }

                            var entityLogicalName = dataverseType.GetField("EntityLogicalName")?.GetValue(dataverseType)?.ToString();
                            var dataSource = dataverseAgentOptions.Value.DataSource;
                            return ret.DataSource == dataSource && entityLogicalName == entityTypeGroup.Key;
                        }
                        catch (Exception ex)
                        {
                            //TODO: Log
                            return false;
                        }
                    });
                });

                if (dataTypeEntityType == null)
                {
                    results.AddRange(entityTypeGroup.Select(s => new ProcessDataverseCreateUpdateEventResult()
                    {
                        DataverseEventInfo = s.DataverseEventInfo,
                        Success = false,
                        FailureReason = $"DataHub entity type not found for {entityTypeGroup.Key}. This may be due to a misconfiguration of the RelatedEntityType attribute."
                    }));

                    continue;
                }

                var att = (RelatedEntityTypeAttribute)dataTypeEntityType.GetCustomAttributes(typeof(RelatedEntityTypeAttribute), true).FirstOrDefault(w => ((RelatedEntityTypeAttribute)w).DataSource == dataverseAgentOptions.Value.DataSource);

                var reqType = typeof(MergeSpecificDataverseEntitiesRequest<,>);
                var entityType = Type.GetType(att!.TypeName)!;
                var genericReq = reqType.MakeGenericType(entityType, dataTypeEntityType);

                var entityIds = entityTypeGroup.Select(s => new Guid(s.EntityId)).ToList();

                var req = (IRequest)Activator.CreateInstance(genericReq, entityIds, Guid.NewGuid().ToString());
                _ = (await mediator.SendAsync((IRequest)req, cancellationToken)) switch { { IsT1: true } result => throw result.AsT1, { AsT0: var mediatorResultValue } => mediatorResultValue };

                results.AddRange(entityTypeGroup.Select(s=> new ProcessDataverseCreateUpdateEventResult()
                {
                    DataverseEventInfo = s.DataverseEventInfo,
                    Success = true
                }));
            }
            catch (Exception ex)
            {
                results.AddRange(entityTypeGroup.Select(s => new ProcessDataverseCreateUpdateEventResult()
                {
                    DataverseEventInfo = s.DataverseEventInfo,
                    Success = false,
                    FailureReason = ex.Message
                }));
            }
        }
        return new ProcessDataverseCreateUpdateEventsResponse()
        {
            Results = results
        };
    }
}