using FluentValidation;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.GetDataverseAgentMergeMarker;

public class GetDataverseAgentMergeMarkerRequestValidator : AbstractValidator<GetDataverseAgentMergeMarkerRequest>
{
    public GetDataverseAgentMergeMarkerRequestValidator()
    {
        RuleFor(r => r.AgentId).NotEmpty();
        RuleFor(r => r.DataSourceId).NotEmpty();
        RuleFor(r => r.EntityType).NotEmpty();
    }
}