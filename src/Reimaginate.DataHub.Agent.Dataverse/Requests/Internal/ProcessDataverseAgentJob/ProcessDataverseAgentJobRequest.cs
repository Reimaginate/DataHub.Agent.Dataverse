using Reimaginate.DataHub.SharedModels.Core.Models.DTO;
using Reimaginate.DataHub.SharedModels.Core.Models.Jobs;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseAgentJob
{
    public class ProcessDataverseAgentJobRequest : IRequest<ProcessDataverseAgentJobResponse>
    {
        public JobDTO Job { get; set; }
        public Type DataHubAssemblyMarker { get; set; }
    }
}
