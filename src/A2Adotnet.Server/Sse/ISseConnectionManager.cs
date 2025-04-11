using A2Adotnet.Common.Models;
using Microsoft.AspNetCore.Http; // For HttpContext

namespace A2Adotnet.Server.Sse;

/// <summary>
/// Manages active Server-Sent Event (SSE) connections for A2A tasks.
/// </summary>
public interface ISseConnectionManager
{
    /// <summary>
    /// Adds a new SSE connection for a specific task ID.
    /// The implementation should handle keeping the connection alive and writing events.
    /// </summary>
    /// <param name="taskId">The ID of the task to associate the connection with.</param>
    /// <param name="context">The HttpContext representing the client connection.</param>
    /// <param name="cancellationToken">Token to signal connection closure.</param>
    /// <returns>A System.Threading.Tasks.Task representing the asynchronous operation of managing the connection.</returns>
    System.Threading.Tasks.Task AddConnectionAsync(string taskId, HttpContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a connection associated with a task ID (e.g., on disconnect).
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="context">The HttpContext of the connection to remove.</param>
    void RemoveConnection(string taskId, HttpContext context);

    /// <summary>
    /// Sends a task update event to all active connections for a specific task ID.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="updateEvent">The update event to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A System.Threading.Tasks.Task representing the asynchronous operation.</returns>
    System.Threading.Tasks.Task SendUpdateAsync(string taskId, TaskUpdateEventBase updateEvent, CancellationToken cancellationToken = default);
}