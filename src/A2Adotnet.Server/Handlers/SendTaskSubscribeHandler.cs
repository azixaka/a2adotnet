using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;
using A2Adotnet.Server.Sse; // Added for SSE
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // For AgentCard/Capabilities

namespace A2Adotnet.Server.Handlers;

/// <summary>
/// Handles the "tasks/sendSubscribe" JSON-RPC method.
/// Initiates a task and establishes an SSE connection for updates.
/// Note: This handler does not return a standard JSON-RPC response body.
/// </summary>
internal class SendTaskSubscribeHandler : IA2ARequestHandler<TaskSendParams, object> // Result type is irrelevant
{
    private readonly ITaskManager _taskManager;
    private readonly IAgentLogicInvoker _agentLogic;
    private readonly ISseConnectionManager _sseManager;
    private readonly AgentCapabilities _agentCapabilities;
    private readonly ILogger<SendTaskSubscribeHandler> _logger;

    public string MethodName => "tasks/sendSubscribe";

    public SendTaskSubscribeHandler(
        ITaskManager taskManager,
        IAgentLogicInvoker agentLogic,
        ISseConnectionManager sseManager,
        IOptions<AgentCard> agentCardOptions,
        ILogger<SendTaskSubscribeHandler> logger)
    {
        _taskManager = taskManager;
        _agentLogic = agentLogic;
        _sseManager = sseManager;
        _agentCapabilities = agentCardOptions?.Value?.Capabilities ?? throw new InvalidOperationException("AgentCard capabilities not configured.");
        _logger = logger;
    }

    public async Task<object?> HandleAsync(TaskSendParams parameters, HttpContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling tasks/sendSubscribe request for Task ID: {TaskId}", parameters.Id);

        // Check if agent supports streaming
        if (!_agentCapabilities.Streaming)
        {
            _logger.LogWarning("Attempted to subscribe to Task ID {TaskId}, but agent does not support streaming.", parameters.Id);
            throw new A2AServerException(A2AErrorCodes.UnsupportedOperation, "Agent does not support streaming (tasks/sendSubscribe).");
        }

        // 1. Create or Get Task
        var task = await _taskManager.CreateOrGetTaskAsync(
            parameters.Id,
            parameters.Message,
            parameters.SessionId,
            cancellationToken);

        // 2. Set Push Config if provided
        if (parameters.PushNotification != null)
        {
            // Check if push is supported before trying to set
             if (!_agentCapabilities.PushNotifications)
             {
                 _logger.LogWarning("Push notification config provided for Task ID {TaskId} via subscribe, but agent does not support push notifications.", parameters.Id);
                 // Don't throw, just log? Or throw InvalidParams? Let's throw InvalidParams.
                 throw new A2AServerException(A2AErrorCodes.InvalidParams, "Push notification configuration provided, but agent does not support push notifications.");
             }
            await _taskManager.SetPushNotificationConfigAsync(task.Id, parameters.PushNotification, cancellationToken);
        }

        // 3. Add connection to SSE Manager - This will take over the response stream
        await _sseManager.AddConnectionAsync(task.Id, context, cancellationToken);

        // 4. Trigger agent logic processing (potentially in background)
        // Use Task.Run or similar to avoid blocking the SSE connection setup if logic is long-running.
        // Ensure agent logic updates TaskManager, which should trigger SSE sends via the manager.
        _ = System.Threading.Tasks.Task.Run(async () => { // Qualified Task
             try
             {
                 await _agentLogic.ProcessTaskAsync(task, parameters.Message, cancellationToken);
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Background agent logic failed for Task ID: {TaskId}", task.Id);
                 // Attempt to update task status to failed and notify via SSE/Push
                 try
                 {
                    await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Failed, new Message { Role = "agent", Parts = new List<Part> { new TextPart($"Agent processing failed: {ex.Message}") } }, CancellationToken.None); // Use initializer for Message, constructor for TextPart
                    // TODO: Ensure TaskManager update triggers SSE send via ISseConnectionManager.SendUpdateAsync
                 } catch (Exception updateEx) {
                     _logger.LogError(updateEx, "Failed to update task status to Failed after background error for Task ID: {TaskId}", task.Id);
                 }
             }
        }, cancellationToken); // Pass cancellation token to allow stopping background work


        // 5. Return null - the response is handled by the SSE connection manager keeping the connection open.
        // The dispatcher needs to know not to write a standard JSON-RPC response for this method.
        return null;
    }
}