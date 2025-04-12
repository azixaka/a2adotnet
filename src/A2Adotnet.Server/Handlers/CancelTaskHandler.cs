using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace A2Adotnet.Server.Handlers;

/// <summary>
/// Handles the "tasks/cancel" JSON-RPC method.
/// </summary>
internal class CancelTaskHandler : IA2ARequestHandler<TaskIdParams, Common.Models.Task>
{
    private readonly ITaskManager _taskManager;
    private readonly ILogger<CancelTaskHandler> _logger;
    // Potentially inject IAgentLogicInvoker if cancellation requires notifying agent logic

    public string MethodName => "tasks/cancel";

    public CancelTaskHandler(ITaskManager taskManager, ILogger<CancelTaskHandler> logger)
    {
        _taskManager = taskManager;
        _logger = logger;
    }

    public async Task<Common.Models.Task?> HandleAsync(TaskIdParams parameters, HttpContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling tasks/cancel request for Task ID: {TaskId}", parameters.Id);

        var updatedTask = await _taskManager.CancelTaskAsync(parameters.Id, cancellationToken);

        if (updatedTask == null)
        {
            // Determine if task wasn't found or wasn't cancelable
            var existingTask = await _taskManager.GetTaskAsync(parameters.Id, 0, cancellationToken);
            if (existingTask == null)
            {
                _logger.LogWarning("Task not found for cancellation: {TaskId}", parameters.Id);
                throw new A2AServerException(A2AErrorCodes.TaskNotFound, $"Task with ID '{parameters.Id}' not found.");
            }
            else
            {
                 _logger.LogWarning("Task {TaskId} is already in a terminal state ({TaskState}) and cannot be canceled.", parameters.Id, existingTask.Status.State);
                 throw new A2AServerException(A2AErrorCodes.TaskNotCancelable, $"Task with ID '{parameters.Id}' is in a terminal state ({existingTask.Status.State}) and cannot be canceled.");
            }
        }

        // TODO: Consider if agent logic needs notification about cancellation.
        // If so, inject IAgentLogicInvoker and call a cancellation method.

        _logger.LogInformation("Successfully processed cancellation request for Task ID: {TaskId}", parameters.Id);
        return updatedTask; // Return the task with 'canceled' status
    }
}