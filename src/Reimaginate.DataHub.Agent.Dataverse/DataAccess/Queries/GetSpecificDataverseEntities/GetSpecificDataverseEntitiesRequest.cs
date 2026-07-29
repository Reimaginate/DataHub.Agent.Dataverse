using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetSpecificDataverseEntities;

public class GetSpecificDataverseEntitiesRequest<TDataverseEntity> : IRequest<GetSpecificDataverseEntitiesResponse<TDataverseEntity>> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public List<string> ColumnSet { get; set; }
    public List<Guid> EntityIds { get; set; }
    public bool ThrowOnNotFound { get; set; } = true;
}