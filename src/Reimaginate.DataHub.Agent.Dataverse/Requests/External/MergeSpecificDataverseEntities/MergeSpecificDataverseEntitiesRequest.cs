using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

// ReSharper disable IdentifierTypo

namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeSpecificDataverseEntities;

public class MergeSpecificDataverseEntitiesRequest<TDataverseEntity, TDataHubEntity> : IRequest<ProcessMergeResponse> where TDataverseEntity : Microsoft.Xrm.Sdk.Entity where TDataHubEntity : DataHubEntity
{


    public MergeSpecificDataverseEntitiesRequest()
    { }

    public MergeSpecificDataverseEntitiesRequest(List<Guid> entityIds, string correlationId = null)
    {
        EntityIds = entityIds;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
    }

    public List<Guid> EntityIds { get; set; }

    public string CorrelationId { get; set; }

    public bool ForceUpdate { get; set; }

}