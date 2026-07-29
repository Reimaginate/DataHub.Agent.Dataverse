using Azure.Messaging.ServiceBus;
using Newtonsoft.Json.Linq;

namespace Reimaginate.DataHub.Agent.Dataverse.Models;

public class DataverseEventInfo
{
    public string EventType { get; set; }
    public JObject ExecutionContext { get; set; }
    public ServiceBusReceivedMessage QueueMessage { get; set; }
}