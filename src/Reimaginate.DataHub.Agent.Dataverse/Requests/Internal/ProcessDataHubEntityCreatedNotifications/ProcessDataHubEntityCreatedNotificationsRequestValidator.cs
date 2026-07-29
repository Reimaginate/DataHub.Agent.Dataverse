using FluentValidation;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataHubEntityCreatedNotifications;

public class ProcessDataHubEntityCreatedNotificationsRequestValidator : AbstractValidator<ProcessDataHubEntityCreatedNotificationsRequest>
{
    public ProcessDataHubEntityCreatedNotificationsRequestValidator()
    {
        RuleFor(r => r.Notifications).NotEmpty();
        RuleFor(r => r.DataHubAssemblyMarker).NotNull();
    }
}