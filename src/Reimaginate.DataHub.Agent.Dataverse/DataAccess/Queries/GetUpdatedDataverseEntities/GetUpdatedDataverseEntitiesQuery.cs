using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetUpdatedDataverseEntities;

public class GetUpdatedDataverseEntitiesQuery<TDataverseEntity> : IRequest<List<TDataverseEntity>> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public DateTimeOffset FromDateTime { get; set; }
}