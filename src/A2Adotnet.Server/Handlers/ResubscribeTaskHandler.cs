using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;
using A2Adotnet.Server.Sse; // Added for SSE
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // For AgentCard/Capabilities

namespace A2Adotnet.Server.Handlers;

/// <summary>
/// Handles the "tasks/resubscribe" JSON-RPC method.
/// Re-establishes an SSE connection for an existing task.
/// Note: This handler does not return a standard JSON-RPC response body.
/// </summary>
internal class ResubscribeTaskHandler : IA2ARequestHandler<TaskQueryParams, object> // Result type is irrelevant
{
    private readonly ITaskManager _taskManager;
    private readonly ISseConnectionManager _sseManager;
    private readonly AgentCapabilities _agentCapabilities;
    private readonly ILogger<ResubscribeTaskHandler> _logger;

    public string MethodName => "tasks/resubscribe";

    public ResubscribeTaskHandler(
        ITaskManager taskManager,
        ISseConnectionManager sseManager,
        IOptions<AgentCard> agentCardOptions,
        ILogger<ResubscribeTaskHandler> logger)
    {
        _taskManager = taskManager;
        _sseManager = sseManager;
        _agentCapabilities = agentCardOptions?.Value?.Capabilities ?? throw new InvalidOperationException("AgentCard capabilities not configured.");
        _logger = logger;
    }

    public async Task<object?> HandleAsync(TaskQueryParams parameters, HttpContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling tasks/resubscribe request for Task ID: {TaskId}", parameters.Id);

        // Check if agent supports streaming
        if (!_agentCapabilities.Streaming)
        {
            _logger.LogWarning("Attempted to resubscribe to Task ID {TaskId}, but agent does not support streaming.", parameters.Id);
            throw new A2AServerException(A2AErrorCodes.UnsupportedOperation, "Agent does not support streaming (tasks/resubscribe).");
        }

        // 1. Verify task exists
        var task = await _taskManager.GetTaskAsync(parameters.Id, 0, cancellationToken); // History length 0 just to check existence
        if (task == null)
        {
             _logger.LogWarning("Attempted to resubscribe to non-existent Task ID: {TaskId}", parameters.Id);
             throw new A2AServerException(A2AErrorCodes.TaskNotFound, $"Task with ID '{parameters.Id}' not found for resubscription.");
        }

        // Optional: Check if task is already in a terminal state?
        // The spec doesn't explicitly forbid resubscribing to completed tasks,
        // but the client might not receive any further updates.

        // 2. Add connection to SSE Manager
        await _sseManager.AddConnectionAsync(parameters.Id, context, cancellationToken);

        // 3. Optional: Re-send current status or recent events upon resubscription?
        //    The spec doesn't mandate this. The client might miss events between disconnect and resubscribe.
        //    Could potentially send current status:
        //    await _sseManager.SendUpdateAsync(task.Id, new TaskStatusUpdateEvent { Id = task.Id, Status = task.Status }, cancellationToken);

        // 4. Return null - the response is handled by the SSE connection manager.
        return null;
    }
}