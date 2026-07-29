using Newtonsoft.Json;
using Reimaginate.DataHub.Requests.External.Client.DeserializeClientRequest;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;

namespace Reimaginate.DataHub.Agent.TestFramework.Dataverse.IntegrationTesting;

public sealed class InProcessDataHubClient(IMediator mediator) : IDataHubClient
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        DateParseHandling = DateParseHandling.DateTimeOffset
    };

    public async Task<TResponse> PostRequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : DataHubClientRequest<TResponse>
        where TResponse : class
    {
        request.CorrelationId ??= Guid.NewGuid().ToString("N");

        var serializedRequest = new SerializedRequest
        {
            RequestType = request.RequestType,
            CorrelationId = request.CorrelationId,
            Data = JsonConvert.SerializeObject(request, SerializerSettings)
        };

        var deserializedRequest = (await mediator.TrySend<IRequest>(
            new DeserializeClientRequestRequest { SerializedRequest = serializedRequest },
            cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };

        return (await mediator.TrySend<TResponse>(
            (IRequest<TResponse>)deserializedRequest,
            cancellationToken)) switch { { Item2: { } exception } => throw exception, { Item1: var mediatorResultValue } => mediatorResultValue };
    }
}
