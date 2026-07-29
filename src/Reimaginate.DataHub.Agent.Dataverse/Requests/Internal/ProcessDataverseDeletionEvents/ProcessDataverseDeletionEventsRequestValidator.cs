using FluentValidation;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataverseDeletionEvents;

public class ProcessDataverseDeletionEventsRequestValidator : AbstractValidator<ProcessDataverseDeletionEventsRequest>
{
    public ProcessDataverseDeletionEventsRequestValidator()
    {
        RuleFor(r => r.Events).NotEmpty();
        RuleFor(r => r.DataHubAssemblyMarker).NotNull();
    }
}