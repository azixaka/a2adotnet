using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // For AgentCard/Capabilities

namespace A2Adotnet.Server.Handlers;

/// <summary>
/// Handles the "tasks/pushNotification/get" JSON-RPC method.
/// </summary>
internal class GetPushNotificationHandler : IA2ARequestHandler<TaskIdParams, TaskPushNotificationConfig>
{
    private readonly ITaskManager _taskManager;
    private readonly AgentCapabilities _agentCapabilities; // Inject configured capabilities
    private readonly ILogger<GetPushNotificationHandler> _logger;

    public string MethodName => "tasks/pushNotification/get";

    public GetPushNotificationHandler(
        ITaskManager taskManager,
        IOptions<AgentCard> agentCardOptions, // Get capabilities from AgentCard config
        ILogger<GetPushNotificationHandler> logger)
    {
        _taskManager = taskManager;
        _agentCapabilities = agentCardOptions?.Value?.Capabilities ?? throw new InvalidOperationException("AgentCard capabilities not configured.");
        _logger = logger;
    }

    public async Task<TaskPushNotificationConfig?> HandleAsync(TaskIdParams parameters, HttpContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling tasks/pushNotification/get request for Task ID: {TaskId}", parameters.Id);

        // Check if agent supports push notifications
        if (!_agentCapabilities.PushNotifications)
        {
            _logger.LogWarning("Attempted to get push notification config for Task ID {TaskId}, but agent does not support push notifications.", parameters.Id);
            throw new A2AServerException(A2AErrorCodes.PushNotificationNotSupported, "Agent does not support push notifications.");
        }

        var config = await _taskManager.GetPushNotificationConfigAsync(parameters.Id, cancellationToken);

        if (config == null)
        {
            // Spec isn't explicit if this should error or return null result.
            // Let's throw TaskNotFound, assuming config is tied to an existing task.
            // If config could exist without a task, TaskManager would need adjustment.
            _logger.LogWarning("Push notification config or task not found for Task ID: {TaskId}", parameters.Id);
            throw new A2AServerException(A2AErrorCodes.TaskNotFound, $"Push notification config not found for Task ID '{parameters.Id}'.");
            // Alternatively, could return null: return null;
        }

        _logger.LogInformation("Returning push notification config for Task ID: {TaskId}", parameters.Id);
        // Wrap the retrieved config in the TaskPushNotificationConfig response object
        return new TaskPushNotificationConfig { Id = parameters.Id, PushNotificationConfig = config };
    }
}