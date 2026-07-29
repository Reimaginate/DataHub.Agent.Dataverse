using FluentValidation;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseAgentJob
{
    public class ProcessDataverseAgentJobRequestValidator : AbstractValidator<ProcessDataverseAgentJobRequest>
    {
        public ProcessDataverseAgentJobRequestValidator()
        {
            RuleFor(e => e.Job).NotNull();
            RuleFor(r => r.DataHubAssemblyMarker).NotNull();
        }
    }
}