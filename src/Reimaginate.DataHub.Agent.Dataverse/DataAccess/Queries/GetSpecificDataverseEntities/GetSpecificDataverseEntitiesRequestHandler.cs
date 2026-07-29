using Microsoft.Xrm.Sdk.Query;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetSpecificDataverseEntities;

public class GetSpecificDataverseEntitiesRequestHandler<TDataverseEntity>(IDataverseDataService idataverseDataService) : IHandler<GetSpecificDataverseEntitiesRequest<TDataverseEntity>, GetSpecificDataverseEntitiesResponse<TDataverseEntity>>
    where TDataverseEntity : Microsoft.Xrm.Sdk.Entity
{
    public async Task<GetSpecificDataverseEntitiesResponse<TDataverseEntity>> HandleAsync(GetSpecificDataverseEntitiesRequest<TDataverseEntity> request, CancellationToken cancellationToken)
    {
        try
        {
            var columnSet = request.ColumnSet != null ? new ColumnSet(request.ColumnSet.Except(["id"]).ToArray()) : new ColumnSet(true);

            var ret = await idataverseDataService.GetAsync<TDataverseEntity>(request.EntityIds, columnSet, request.ThrowOnNotFound, cancellationToken);
            return new GetSpecificDataverseEntitiesResponse<TDataverseEntity>()
            {
                Success = ret.NotFound == null || !ret.NotFound.Any(),
                Results = ret.Results,
                NotFound = ret.NotFound
            };
        }
        catch (Exception ex)
        {
            return new GetSpecificDataverseEntitiesResponse<TDataverseEntity>()
            {
                Success = false,
                FailureReason = ex.Message
            };
        }
    }
}