using FluentValidation;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessDataHubEntityUpdatedNotifications;

public class ProcessDataHubEntityUpdatedNotificationsRequestValidator : AbstractValidator<ProcessDataHubEntityUpdatedNotificationsRequest>
{
    public ProcessDataHubEntityUpdatedNotificationsRequestValidator()
    {
        RuleFor(r => r.Notifications).NotEmpty();
        RuleFor(r => r.DataHubAssemblyMarker).NotNull();
    }
}