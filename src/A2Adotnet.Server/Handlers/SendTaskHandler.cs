using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace A2Adotnet.Server.Handlers;

/// <summary>
/// Handles the "tasks/send" JSON-RPC method.
/// </summary>
internal class SendTaskHandler : IA2ARequestHandler<TaskSendParams, Common.Models.Task>
{
    private readonly ITaskManager _taskManager;
    private readonly IAgentLogicInvoker _agentLogic;
    private readonly ILogger<SendTaskHandler> _logger;

    public string MethodName => "tasks/send";

    public SendTaskHandler(ITaskManager taskManager, IAgentLogicInvoker agentLogic, ILogger<SendTaskHandler> logger)
    {
        _taskManager = taskManager;
        _agentLogic = agentLogic;
        _logger = logger;
    }

    public async Task<Common.Models.Task?> HandleAsync(TaskSendParams parameters, HttpContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling tasks/send request for Task ID: {TaskId}", parameters.Id);

        // 1. Create or Get Task using TaskManager
        var task = await _taskManager.CreateOrGetTaskAsync(
            parameters.Id,
            parameters.Message,
            parameters.SessionId,
            cancellationToken);

        // 2. Set Push Notification Config if provided
        if (parameters.PushNotification != null)
        {
            await _taskManager.SetPushNotificationConfigAsync(task.Id, parameters.PushNotification, cancellationToken);
            // Log or handle potential failure? For now, assume success or handled by TaskManager impl.
        }

        // 3. Check current task state - only process if not already completed/failed/canceled?
        //    Or allow re-sending to completed tasks? A2A spec implies client can reopen.
        //    Let's assume we process regardless, agent logic should handle state.

        // 4. Invoke Agent Logic (This should run synchronously for tasks/send)
        try
        {
            // The agent logic is responsible for updating the task status via ITaskManager
            // to working, completed, failed, or input-required.
            await _agentLogic.ProcessTaskAsync(task, parameters.Message, cancellationToken);

            // 5. Retrieve the final state of the task after processing
            var finalTask = await _taskManager.GetTaskAsync(task.Id, parameters.HistoryLength, cancellationToken);

            if (finalTask == null)
            {
                 // Should not happen if CreateOrGetTaskAsync succeeded
                 _logger.LogError("Task {TaskId} disappeared after processing.", task.Id);
                 throw new A2AServerException(A2AErrorCodes.InternalError, "Task state lost after processing.");
            }

             _logger.LogInformation("Completed tasks/send request for Task ID: {TaskId} with final state: {TaskState}", finalTask.Id, finalTask.Status.State);
            return finalTask;

        }
        catch (Exception ex) when (ex is not A2AServerException) // Catch unexpected agent logic errors
        {
            _logger.LogError(ex, "Agent logic failed for Task ID: {TaskId}", task.Id);
            // Attempt to update task status to failed
            await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Failed, new Message("agent", new List<Part> { new TextPart($"Agent processing failed: {ex.Message}") }), CancellationToken.None); // Use CancellationToken.None for cleanup

            // Re-throw as internal server error for JSON-RPC response
            throw new A2AServerException(A2AErrorCodes.InternalError, $"Agent processing failed: {ex.Message}", innerException: ex);
        }
        // A2AServerExceptions thrown by agent logic will be caught by the dispatcher
    }
}