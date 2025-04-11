using A2Adotnet.Common.Models;

namespace A2Adotnet.Server.Abstractions;

/// <summary>
/// Interface for invoking the core agent logic associated with a task.
/// Implementations will contain the specific business logic, LLM calls, tool usage, etc.
/// </summary>
public interface IAgentLogicInvoker
{
    /// <summary>
    /// Processes a newly submitted or continuing task.
    /// This method should perform the agent's work and update the task state
    /// via the ITaskManager (e.g., setting status to working, adding artifacts,
    /// setting status to completed/failed/input-required).
    /// </summary>
    /// <param name="task">The task being processed.</param>
    /// <param name="triggeringMessage">The message that triggered this processing (either initial or follow-up).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// IMPORTANT: The implementation MUST update the task status via ITaskManager upon completion or when input is required.
    /// For non-streaming ('tasks/send'), this method should ideally complete the task processing synchronously within the request context.
    /// For streaming ('tasks/sendSubscribe'), this method might run in the background, posting updates via ITaskManager.
    /// </remarks>
    Task ProcessTaskAsync(Common.Models.Task task, Message triggeringMessage, CancellationToken cancellationToken);
}