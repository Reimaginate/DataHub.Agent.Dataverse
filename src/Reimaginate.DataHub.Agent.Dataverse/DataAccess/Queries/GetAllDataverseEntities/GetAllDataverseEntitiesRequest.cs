using Microsoft.Xrm.Sdk.Query;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetAllDataverseEntities;

public class GetAllDataverseEntitiesRequest<TDataverseEntity> : IRequest<List<TDataverseEntity>> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public FilterExpression FilterExpression { get; set; }
}