using FluentValidation;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendMergeFailuresToDataHub;

public class SendMergeFailuresToDataHubRequestValidator : AbstractValidator<SendMergeFailuresToDataHubRequest>
{
    public SendMergeFailuresToDataHubRequestValidator()
    {
        RuleFor(r => r.Failures).NotEmpty();
    }
}