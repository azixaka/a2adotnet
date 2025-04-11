using A2Adotnet.Common.Models;

namespace A2Adotnet.Server.Abstractions;

/// <summary>
/// Interface for managing the state and lifecycle of A2A Tasks.
/// Implementations will handle persistence (e.g., in-memory, database).
/// </summary>
public interface ITaskManager
{
    /// <summary>
    /// Creates or retrieves an existing task based on ID and Session ID.
    /// If the task exists, it may update it with the new message.
    /// If it doesn't exist, it creates a new one.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="initialMessage">The initial message from the client.</param>
    /// <param name="sessionId">Optional session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or retrieved Task, potentially in 'submitted' state.</returns>
    Task<Common.Models.Task> CreateOrGetTaskAsync(
        string taskId,
        Message initialMessage,
        string? sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current state of a task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="historyLength">Optional number of history messages to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Task object, or null if not found.</returns>
    Task<Common.Models.Task?> GetTaskAsync(
        string taskId,
        int? historyLength,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of an existing task. Optionally adds the status message to history.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="newState">The new task state.</param>
    /// <param name="statusMessage">Optional message associated with the status update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false if task not found.</returns>
    Task<bool> UpdateTaskStatusAsync(
        string taskId,
        TaskState newState,
        Message? statusMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an artifact to an existing task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="artifact">The artifact to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false if task not found.</returns>
    Task<bool> AddArtifactAsync(
        string taskId,
        Artifact artifact,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a message to the task's history (if history is being stored).
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="message">The message to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false if task not found.</returns>
    Task<bool> AddHistoryMessageAsync(
        string taskId,
        Message message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to cancel a task, setting its state to 'canceled'.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated Task object if cancellation was possible, null otherwise (e.g., task not found or already in terminal state).</returns>
    Task<Common.Models.Task?> CancelTaskAsync(
        string taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or updates the push notification configuration for a task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="config">The push notification configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false if task not found.</returns>
    Task<bool> SetPushNotificationConfigAsync(
        string taskId,
        PushNotificationConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the push notification configuration for a task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The configuration, or null if not found or not set.</returns>
    Task<PushNotificationConfig?> GetPushNotificationConfigAsync(
        string taskId,
        CancellationToken cancellationToken = default);
}