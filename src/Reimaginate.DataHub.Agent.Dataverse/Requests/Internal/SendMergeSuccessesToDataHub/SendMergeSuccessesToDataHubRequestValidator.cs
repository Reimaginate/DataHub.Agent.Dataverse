using FluentValidation;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendMergeSuccessesToDataHub;

public class SendMergeSuccessesToDataHubRequestValidator : AbstractValidator<SendMergeSuccessesToDataHubRequest>
{
    public SendMergeSuccessesToDataHubRequestValidator()
    {
        RuleFor(r => r.Successes).NotEmpty();
    }

}