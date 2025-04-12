using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // For AgentCard/Capabilities

namespace A2Adotnet.Server.Handlers;

/// <summary>
/// Handles the "tasks/pushNotification/set" JSON-RPC method.
/// </summary>
internal class SetPushNotificationHandler : IA2ARequestHandler<TaskPushNotificationConfig, TaskPushNotificationConfig>
{
    private readonly ITaskManager _taskManager;
    private readonly AgentCapabilities _agentCapabilities; // Inject configured capabilities
    private readonly ILogger<SetPushNotificationHandler> _logger;

    public string MethodName => "tasks/pushNotification/set";

    public SetPushNotificationHandler(
        ITaskManager taskManager,
        IOptions<AgentCard> agentCardOptions, // Get capabilities from AgentCard config
        ILogger<SetPushNotificationHandler> logger)
    {
        _taskManager = taskManager;
        _agentCapabilities = agentCardOptions?.Value?.Capabilities ?? throw new InvalidOperationException("AgentCard capabilities not configured.");
        _logger = logger;
    }

    public async Task<TaskPushNotificationConfig?> HandleAsync(TaskPushNotificationConfig parameters, HttpContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling tasks/pushNotification/set request for Task ID: {TaskId}", parameters.Id);

        // Check if agent supports push notifications
        if (!_agentCapabilities.PushNotifications)
        {
            _logger.LogWarning("Attempted to set push notification for Task ID {TaskId}, but agent does not support push notifications.", parameters.Id);
            throw new A2AServerException(A2AErrorCodes.PushNotificationNotSupported, "Agent does not support push notifications.");
        }

        // TODO: Implement optional URL validation (e.g., GET challenge request) as described in Push Notifications doc.

        var success = await _taskManager.SetPushNotificationConfigAsync(parameters.Id, parameters.PushNotificationConfig, cancellationToken);

        if (!success)
        {
            // Assume task not found if Set failed (TaskManager impl might differ)
            _logger.LogWarning("Task not found when trying to set push notification config: {TaskId}", parameters.Id);
            throw new A2AServerException(A2AErrorCodes.TaskNotFound, $"Task with ID '{parameters.Id}' not found.");
        }

        _logger.LogInformation("Successfully set push notification config for Task ID: {TaskId}", parameters.Id);
        // Return the input parameters as confirmation, as per spec example
        return parameters;
    }
}