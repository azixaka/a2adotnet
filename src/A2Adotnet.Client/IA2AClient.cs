using A2Adotnet.Common.Models;
using A2Adotnet.Common.Protocol.Messages;

namespace A2Adotnet.Client;

/// <summary>
/// Interface for an A2A protocol client.
/// </summary>
public interface IA2AClient
{
    /// <summary>
    /// Fetches the AgentCard from the configured agent endpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent's AgentCard.</returns>
    /// <exception cref="A2AClientException">Thrown if the request fails or the response is invalid.</exception>
    Task<AgentCard> GetAgentCardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to initiate or continue a task using the standard request/response pattern.
    /// </summary>
    /// <param name="taskId">The unique ID for the task.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="sessionId">Optional session ID to group related tasks.</param>
    /// <param name="pushNotificationConfig">Optional push notification configuration for this task.</param>
    /// <param name="historyLength">Optional number of recent messages to request in the response history.</param>
    /// <param name="metadata">Optional metadata for the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The final Task object after synchronous processing by the agent.</returns>
    /// <exception cref="A2AClientException">Thrown if the request fails or the response indicates an error.</exception>
    Task<Common.Models.Task> SendTaskAsync(
        string taskId,
        Message message,
        string? sessionId = null,
        PushNotificationConfig? pushNotificationConfig = null,
        int? historyLength = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current state and artifacts of a task.
    /// </summary>
    /// <param name="taskId">The ID of the task to retrieve.</param>
    /// <param name="historyLength">Optional number of recent messages to request in the response history.</param>
    /// <param name="metadata">Optional metadata for the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current Task object.</returns>
    /// <exception cref="A2AClientException">Thrown if the request fails or the response indicates an error.</exception>
    Task<Common.Models.Task> GetTaskAsync(
        string taskId,
        int? historyLength = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests cancellation of a running task.
    /// </summary>
    /// <param name="taskId">The ID of the task to cancel.</param>
    /// <param name="metadata">Optional metadata for the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Task object reflecting the cancellation attempt (likely with status 'canceled' or an error if not cancelable).</returns>
    /// <exception cref="A2AClientException">Thrown if the request fails or the response indicates an error.</exception>
    Task<Common.Models.Task> CancelTaskAsync(
        string taskId,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or updates the push notification configuration for a task.
    /// </summary>
    /// <param name="taskId">The ID of the task.</param>
    /// <param name="config">The push notification configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The confirmed push notification configuration.</returns>
    /// <exception cref="A2AClientException">Thrown if the request fails or the response indicates an error.</exception>
    Task<TaskPushNotificationConfig> SetPushNotificationAsync(
        string taskId,
        PushNotificationConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current push notification configuration for a task.
    /// </summary>
    /// <param name="taskId">The ID of the task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current push notification configuration.</returns>
    /// <exception cref="A2AClientException">Thrown if the request fails or the response indicates an error.</exception>
    Task<TaskPushNotificationConfig> GetPushNotificationAsync(
        string taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to initiate or continue a task and subscribes to real-time updates via Server-Sent Events (SSE).
    /// </summary>
    /// <param name="taskId">The unique ID for the task.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="sessionId">Optional session ID to group related tasks.</param>
    /// <param name="pushNotificationConfig">Optional push notification configuration for this task.</param>
    /// <param name="historyLength">Optional number of recent messages to request in the response history.</param>
    /// <param name="metadata">Optional metadata for the request.</param>
    /// <param name="cancellationToken">Cancellation token for stopping the stream consumption.</param>
    /// <returns>An asynchronous stream of <see cref="TaskUpdateEventBase"/> (either <see cref="TaskStatusUpdateEvent"/> or <see cref="TaskArtifactUpdateEvent"/>).</returns>
    /// <exception cref="A2AClientException">Thrown if the initial request fails or an error occurs during streaming setup.</exception>
    /// <exception cref="HttpRequestException">Thrown for network errors during streaming.</exception>
    /// <exception cref="JsonException">Thrown for JSON parsing errors during streaming.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the <paramref name="cancellationToken"/> is cancelled.</exception>
    IAsyncEnumerable<TaskUpdateEventBase> SendTaskAndSubscribeAsync(
        string taskId,
        Message message,
        string? sessionId = null,
        PushNotificationConfig? pushNotificationConfig = null,
        int? historyLength = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resubscribes to an existing task's update stream after a disconnection.
    /// </summary>
    /// <param name="taskId">The ID of the task to resubscribe to.</param>
    /// <param name="historyLength">Optional hint about how many recent messages to include in the response history (behavior might vary by server).</param>
    /// <param name="metadata">Optional metadata for the request.</param>
    /// <param name="cancellationToken">Cancellation token for stopping the stream consumption.</param>
    /// <returns>An asynchronous stream of <see cref="TaskUpdateEventBase"/> (either <see cref="TaskStatusUpdateEvent"/> or <see cref="TaskArtifactUpdateEvent"/>).</returns>
    /// <exception cref="A2AClientException">Thrown if the initial request fails or an error occurs during streaming setup.</exception>
    /// <exception cref="HttpRequestException">Thrown for network errors during streaming.</exception>
    /// <exception cref="JsonException">Thrown for JSON parsing errors during streaming.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the <paramref name="cancellationToken"/> is cancelled.</exception>
    IAsyncEnumerable<TaskUpdateEventBase> ResubscribeAsync(
        string taskId,
        int? historyLength = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);
}