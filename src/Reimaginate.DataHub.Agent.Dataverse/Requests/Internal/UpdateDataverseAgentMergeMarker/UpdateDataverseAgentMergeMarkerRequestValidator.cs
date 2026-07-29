using FluentValidation;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.UpdateDataverseAgentMergeMarker;

public class UpdateDataverseAgentMergeMarkerRequestValidator : AbstractValidator<UpdateDataverseAgentMergeMarkerRequest>
{
    public UpdateDataverseAgentMergeMarkerRequestValidator()
    {
        RuleFor(r => r.Marker).NotNull();
        RuleFor(r => r.Marker.DataSource).NotEmpty();
        RuleFor(r => r.Marker.AgentId).NotEmpty();
        RuleFor(r => r.Marker.EntityType).NotEmpty();
        RuleFor(r => r.NewValue).NotEmpty();
    }
}