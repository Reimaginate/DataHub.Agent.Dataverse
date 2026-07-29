using FluentValidation;

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.GetDataverseAgentSyncMarker;

public class GetDataverseAgentSyncMarkerRequestValidator : AbstractValidator<GetDataverseAgentSyncMarkerRequest>
{
    public GetDataverseAgentSyncMarkerRequestValidator()
    {
        RuleFor(r => r.AgentId).NotEmpty();
        RuleFor(r => r.DataSourceId).NotEmpty();
        RuleFor(r => r.EntityType).NotEmpty();
    }
}