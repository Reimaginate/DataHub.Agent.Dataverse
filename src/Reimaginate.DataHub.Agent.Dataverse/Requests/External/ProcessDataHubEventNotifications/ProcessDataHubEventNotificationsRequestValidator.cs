using FluentValidation;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.ProcessDataHubEventNotifications;

public class ProcessDataHubEventNotificationsRequestValidator : AbstractValidator<ProcessDataHubEventNotificationsRequest>
{
    public ProcessDataHubEventNotificationsRequestValidator()
    {
        RuleFor(r => r.EventGridEvents).NotEmpty();
        RuleFor(r => r.DataHubAssemblyMarker).NotNull();
    }
}