namespace Reimaginate.DataHub.Agent.Dataverse.Requests.External.ProcessDataHubEventNotifications;

public class ProcessDataHubEventNotificationsResponse
{
    public ProcessDataHubEventNotificationsResponse()
    { }
    public ProcessDataHubEventNotificationsResponse(bool success)
    {
        Success = success;
    }

    public ProcessDataHubEventNotificationsResponse(bool success, string errorMessage)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
}