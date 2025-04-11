using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace A2Adotnet.Server.Handlers;

/// <summary>
/// Handles the "tasks/get" JSON-RPC method.
/// </summary>
internal class GetTaskHandler : IA2ARequestHandler<TaskQueryParams, Common.Models.Task>
{
    private readonly ITaskManager _taskManager;
    private readonly ILogger<GetTaskHandler> _logger;

    public string MethodName => "tasks/get";

    public GetTaskHandler(ITaskManager taskManager, ILogger<GetTaskHandler> logger)
    {
        _taskManager = taskManager;
        _logger = logger;
    }

    public async Task<Common.Models.Task?> HandleAsync(TaskQueryParams parameters, HttpContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling tasks/get request for Task ID: {TaskId}", parameters.Id);

        var task = await _taskManager.GetTaskAsync(parameters.Id, parameters.HistoryLength, cancellationToken);

        if (task == null)
        {
            _logger.LogWarning("Task not found for ID: {TaskId}", parameters.Id);
            // Throw specific A2A error for Task Not Found
            throw new A2AServerException(A2AErrorCodes.TaskNotFound, $"Task with ID '{parameters.Id}' not found.");
        }

        _logger.LogInformation("Returning task state for Task ID: {TaskId}", parameters.Id);
        return task;
    }
}