using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncSpecificDataHubEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;
using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Constants;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Requests.Agent;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.Mediator;
using MergeEntitiesRequest = Reimaginate.DataHub.SharedModels.Requests.Agent.MergeEntitiesRequest;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseAgentJob
{
    public class ProcessDataverseAgentJobRequestHandler(IOptions<DataverseAgentOptions> agentConfig, IDataHubClient dataHubClient, IMediator mediator)
        : IHandler<ProcessDataverseAgentJobRequest, ProcessDataverseAgentJobResponse>
    {
        public async Task<ProcessDataverseAgentJobResponse> HandleAsync(ProcessDataverseAgentJobRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var job = request.Job;
                var requestType = job.Request.Value<string>(nameof(AgentRequest.RequestType));

                switch (requestType)
                {
                    case nameof(MergeEntitiesRequest):

                        var mergeEntitiesRequest = job.Request.ToObject<MergeEntitiesRequest>();

                        try
                        {
                            var response = await ProcessMergeEntitiesResponse(mergeEntitiesRequest, request.DataHubAssemblyMarker, cancellationToken);
                            foreach (var mergeResponse in response.Results)
                            {
                                mergeResponse.ResultingDataHubEntity = null;
                                mergeResponse.ResultingDataHubEntityUpdates = null;
                                mergeResponse.ResultingSourceEntity = null;
                                mergeResponse.ResultingSourceEntityUpdates = null;
                            }

                            job.Response = JToken.FromObject(response);

                            var failures = response.Results.Where(w => MergeOutcomes.IsFailure(w.MergeOutcome)).ToList();
                            if (failures.Any())
                            {
                                job.Status = "Failed";

                                await dataHubClient.PostRequestAsync<UpdateJobRequest, UpdateJobResponse>(new UpdateJobRequest()
                                {
                                    Job = job
                                }, cancellationToken);

                                return new ProcessDataverseAgentJobResponse()
                                {
                                    Success = false,
                                    FailureReason = string.Join(" | ", failures.Select(s => $"{s.DataHubEntityId}: {s.FailureReason}"))
                                };
                            }

                            job.Status = "Complete";

                            var updateJobResponse = await dataHubClient.PostRequestAsync<UpdateJobRequest, UpdateJobResponse>(new UpdateJobRequest()
                            {
                                Job = job
                            }, cancellationToken);

                            if (!updateJobResponse.Success)
                            {
                                return new ProcessDataverseAgentJobResponse()
                                {
                                    Success = false,
                                    FailureReason = "JOB_UPDATE_FAILED: " + updateJobResponse.FailureReason
                                };
                            }

                            return new ProcessDataverseAgentJobResponse()
                            {
                                Success = true
                            };
                        }
                        catch (Exception ex)
                        {
                            return new ProcessDataverseAgentJobResponse()
                            {
                                Success = false,
                                FailureReason = ex.Message
                            };
                        }


                    case nameof(SyncEntitiesRequest):

                        var syncEntitiesRequest = job.Request.ToObject<SyncEntitiesRequest>();

                        try
                        {
                            var response = await ProcessSyncEntitiesResponse(syncEntitiesRequest, request.DataHubAssemblyMarker, cancellationToken);
                            foreach (var mergeResponse in response.Results)
                            {
                                mergeResponse.ResultingDataHubEntity = null;
                                mergeResponse.ResultingDataHubEntityUpdates = null;
                                mergeResponse.ResultingSourceEntity = null;
                                mergeResponse.ResultingSourceEntityUpdates = null;
                            }

                            job.Response = JToken.FromObject(response);

                            var failures = response.Results.Where(w => SyncOutcomes.IsFailure(w.SyncOutcome)).ToList();
                            if (failures.Any())
                            {
                                job.Status = "Failed";

                                await dataHubClient.PostRequestAsync<UpdateJobRequest, UpdateJobResponse>(new UpdateJobRequest()
                                {
                                    Job = job
                                }, cancellationToken);

                                return new ProcessDataverseAgentJobResponse()
                                {
                                    Success = false,
                                    FailureReason = string.Join(" | ", failures.Select(s => $"{s.DataHubEntityId}: {s.FailureReason}"))
                                };
                            }

                            job.Status = "Complete";

                            var updateJobResponse = await dataHubClient.PostRequestAsync<UpdateJobRequest, UpdateJobResponse>(new UpdateJobRequest()
                            {
                                Job = job
                            }, cancellationToken);

                            if (!updateJobResponse.Success)
                            {
                                return new ProcessDataverseAgentJobResponse()
                                {
                                    Success = false,
                                    FailureReason = "JOB_UPDATE_FAILED: " + updateJobResponse.FailureReason
                                };
                            }

                            return new ProcessDataverseAgentJobResponse()
                            {
                                Success = true
                            };
                        }
                        catch (Exception ex)
                        {
                            return new ProcessDataverseAgentJobResponse()
                            {
                                Success = false,
                                FailureReason = ex.Message
                            };
                        }



                    default:
                        return new ProcessDataverseAgentJobResponse()
                        {
                            Success = false,
                            FailureReason = "INVALID_AGENT_REQUEST_TYPE: " + requestType
                        };
                }
            }
            catch (Exception ex)
            {
                return new ProcessDataverseAgentJobResponse()
                {
                    Success = false,
                    FailureReason = ex.Message
                };
            }
        }

        private async Task<ProcessMergeResponse> ProcessMergeEntitiesResponse(MergeEntitiesRequest request, Type dataHubAssemblyMarker, CancellationToken cancellationToken)
        {
            var dataTypeEntityType = dataHubAssemblyMarker.Assembly.GetExportedTypes().FirstOrDefault(w => string.Equals(w.Name, request.DataHubEntityType, StringComparison.CurrentCultureIgnoreCase) && typeof(DataHubEntity).IsAssignableFrom(w));

            var dataverseTypeName = dataTypeEntityType!
                .GetCustomAttributes(typeof(RelatedEntityTypeAttribute), true)
                .Select(s => (RelatedEntityTypeAttribute)s)
                .First(f => f.DataSource == agentConfig.Value.DataSource)?.TypeName;

            var dataverseType = Type.GetType(dataverseTypeName);

            var reqType = typeof(MergeSpecificDataverseEntitiesRequest<,>);
            var genericReq = reqType.MakeGenericType(dataverseType, dataTypeEntityType);

            var req = (IRequest)Activator.CreateInstance(genericReq, request.EntityIds.Where(w => w != null).Select(s => new Guid(s)).ToList(), null);

            var response = (ProcessMergeResponse)((await mediator.SendAsync(req, cancellationToken)) switch { { IsT1: true } result => throw result.AsT1, { AsT0: var mediatorResultValue } => mediatorResultValue });
            return response;
        }

        private async Task<ProcessSyncResponse> ProcessSyncEntitiesResponse(SyncEntitiesRequest request, Type dataHubAssemblyMarker, CancellationToken cancellationToken)
        {
            var dataTypeEntityType = dataHubAssemblyMarker.Assembly.GetExportedTypes().FirstOrDefault(w => string.Equals(w.Name, request.DataHubEntityType, StringComparison.CurrentCultureIgnoreCase) && typeof(DataHubEntity).IsAssignableFrom(w));

            var dataverseTypeName = dataTypeEntityType!
                .GetCustomAttributes(typeof(RelatedEntityTypeAttribute), true)
                .Select(s => (RelatedEntityTypeAttribute)s)
                .First(f => f.DataSource == agentConfig.Value.DataSource)?.TypeName;

            var dataverseType = Type.GetType(dataverseTypeName);

            var reqType = typeof(SyncSpecificDataHubEntitiesRequest<,>);
            var genericReq = reqType.MakeGenericType(dataTypeEntityType, dataverseType);

            var req = (IRequest)Activator.CreateInstance(genericReq, request.DataHubEntityIds, null);

            var response = (ProcessSyncResponse)((await mediator.SendAsync(req, cancellationToken)) switch { { IsT1: true } result => throw result.AsT1, { AsT0: var mediatorResultValue } => mediatorResultValue });
            return response;
        }
    }
}
