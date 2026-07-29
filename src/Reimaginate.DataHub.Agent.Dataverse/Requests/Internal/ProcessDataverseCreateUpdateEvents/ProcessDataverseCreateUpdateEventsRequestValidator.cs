using FluentValidation;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseCreateUpdateEvents;

public class ProcessDataverseCreateUpdateEventsRequestValidator : AbstractValidator<ProcessDataverseCreateUpdateEventsRequest>
{
    public ProcessDataverseCreateUpdateEventsRequestValidator()
    {
        RuleFor(r => r.Events).NotEmpty();
        RuleFor(r => r.DataHubAssemblyMarker).NotNull();
    }
}