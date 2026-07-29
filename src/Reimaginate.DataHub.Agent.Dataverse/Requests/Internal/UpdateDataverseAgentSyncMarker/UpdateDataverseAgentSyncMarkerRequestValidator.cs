using FluentValidation;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.UpdateDataverseAgentSyncMarker;

public class UpdateDataverseAgentSyncMarkerRequestValidator : AbstractValidator<UpdateDataverseAgentSyncMarkerRequest>
{
    public UpdateDataverseAgentSyncMarkerRequestValidator()
    {
        RuleFor(r => r.Marker).NotNull();
        RuleFor(r => r.Marker.DataSource).NotEmpty();
        RuleFor(r => r.Marker.AgentId).NotEmpty();
        RuleFor(r => r.Marker.EntityType).NotEmpty();
        RuleFor(r => r.NewValue).NotEmpty();
    }
}