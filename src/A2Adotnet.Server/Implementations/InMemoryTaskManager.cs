using A2Adotnet.Common.Models;
using A2Adotnet.Common.Models; // Re-added
using A2Adotnet.Server.Abstractions;
using A2Adotnet.Server.Push; // Added for IPushNotificationSender
using A2Adotnet.Server.Sse; // Added for ISseConnectionManager
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging; // Added for logging

namespace A2Adotnet.Server.Implementations;

/// <summary>
/// Simple in-memory implementation of ITaskManager using ConcurrentDictionary.
/// Note: History and push configs are stored but history retrieval might be limited/inefficient.
/// This implementation is suitable for single-instance deployments or testing.
/// </summary>
public class InMemoryTaskManager : ITaskManager
{
    // Store the main task data
    private readonly ConcurrentDictionary<string, Common.Models.Task> _tasks = new();
    private readonly ConcurrentDictionary<string, List<Message>> _history = new();
    private readonly ConcurrentDictionary<string, PushNotificationConfig> _pushConfigs = new();
    private readonly ISseConnectionManager _sseManager;
    private readonly IPushNotificationSender _pushSender; // Added
    private readonly ILogger<InMemoryTaskManager> _logger;

    // Simple locking object for operations that need atomicity across dictionaries if needed,
    // though ConcurrentDictionary handles basic thread safety for single operations.
    // For simplicity here, we assume operations on _tasks are primary and others follow.

    public InMemoryTaskManager(
        ISseConnectionManager sseManager,
        IPushNotificationSender pushSender, // Added
        ILogger<InMemoryTaskManager> logger)
    {
        _sseManager = sseManager;
        _pushSender = pushSender; // Added
        _logger = logger;
    }
    // A more robust implementation might use more granular locking or different data structures.

    public Task<Common.Models.Task> CreateOrGetTaskAsync(
        string taskId,
        Message initialMessage,
        string? sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        ArgumentNullException.ThrowIfNull(initialMessage);

        Common.Models.Task? task = null;
        // bool created = false; // Removed unused variable

        task = _tasks.AddOrUpdate(taskId,
            // Add function (if task doesn't exist)
            addValueFactory: (id) =>
            {
                // created = true; // Removed unused variable assignment
                var newTask = new Common.Models.Task
                {
                    Id = id,
                    SessionId = sessionId ?? Guid.NewGuid().ToString(), // Generate session if needed
                    Status = new Common.Models.TaskStatus { State = TaskState.Submitted, Timestamp = DateTimeOffset.UtcNow }, // Use initializer
                    Artifacts = new List<Artifact>(),
                    History = null, // History handled separately
                    Metadata = null
                };
                // Initialize history list
                _history.TryAdd(id, new List<Message> { initialMessage });
                return newTask;
            },
            // Update function (if task exists - potentially update session or add message?)
            // For now, just return existing and add message to history
            updateValueFactory: (id, existingTask) =>
            {
                // Add message to history if task already exists
                 if (_history.TryGetValue(id, out var historyList))
                 {
                    lock(historyList) // Lock the specific list for modification
                    {
                       historyList.Add(initialMessage);
                    }
                 }
                 else
                 {
                    // Should not happen if AddOrUpdate logic is correct, but handle defensively
                     _history.TryAdd(id, new List<Message> { initialMessage });
                 }
                // Optionally update session ID if provided and different? For now, keep original.
                return existingTask;
            });

        return System.Threading.Tasks.Task.FromResult(task);
    }


    public Task<Common.Models.Task?> GetTaskAsync(
        string taskId,
        int? historyLength,
        CancellationToken cancellationToken = default)
    {
        if (_tasks.TryGetValue(taskId, out var task))
        {
            // Clone the task to avoid external modification of the stored object
            var clonedTask = task with { }; // Shallow clone using record 'with' expression

            // Populate history if requested
            if (historyLength.HasValue && historyLength > 0 && _history.TryGetValue(taskId, out var historyList))
            {
                 lock(historyList) // Lock for safe reading/copying
                 {
                    // Get the last 'historyLength' items
                    clonedTask = clonedTask with { History = historyList.TakeLast(historyLength.Value).ToList() };
                 }
            }
            else
            {
                clonedTask = clonedTask with { History = null }; // Ensure history is null if not requested/found
            }
            return System.Threading.Tasks.Task.FromResult<Common.Models.Task?>(clonedTask);
        }
        return System.Threading.Tasks.Task.FromResult<Common.Models.Task?>(null);
    }

     public Task<bool> UpdateTaskStatusAsync(
        string taskId,
        TaskState newState,
        Message? statusMessage,
        CancellationToken cancellationToken = default)
     {
        if (_tasks.TryGetValue(taskId, out var existingTask))
        {
            // Create new status
            var newStatus = new Common.Models.TaskStatus { State = newState, Message = statusMessage, Timestamp = DateTimeOffset.UtcNow }; // Use initializer

            // Update task using 'with' expression for immutability
            var updatedTask = existingTask with { Status = newStatus };

            // Try to update the dictionary value
            if (_tasks.TryUpdate(taskId, updatedTask, existingTask))
            {
                // Add status message to history if provided
                if (statusMessage != null)
                {
                    AddHistoryMessageAsync(taskId, statusMessage, cancellationToken); // Fire-and-forget history add
                }

                // Notify SSE listeners about the status update
                var statusEvent = new TaskStatusUpdateEvent { Id = taskId, Status = newStatus };
                // Use Task.Run or similar fire-and-forget pattern if SendUpdateAsync could block significantly
                _ = _sseManager.SendUpdateAsync(taskId, statusEvent, CancellationToken.None);

                // Also trigger push notification if applicable
                TriggerPushNotificationIfNeeded(taskId, updatedTask, newState);

                return System.Threading.Tasks.Task.FromResult(true);
            }
            // If TryUpdate fails, it means the value was changed concurrently. Retry or fail?
            // For simplicity, we fail here. A more robust implementation might retry.
            return System.Threading.Tasks.Task.FromResult(false);
        }
        return System.Threading.Tasks.Task.FromResult(false); // Task not found
     }

    public Task<bool> AddArtifactAsync(
        string taskId,
        Artifact artifact,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (_tasks.TryGetValue(taskId, out var existingTask))
        {
            // Create a new list of artifacts, adding the new one
            var newArtifacts = existingTask.Artifacts == null
                ? new List<Artifact> { artifact }
                : new List<Artifact>(existingTask.Artifacts) { artifact }; // Clone list

            var updatedTask = existingTask with { Artifacts = newArtifacts };

            if (_tasks.TryUpdate(taskId, updatedTask, existingTask))
            {
                // Notify SSE listeners about the new artifact
                var artifactEvent = new TaskArtifactUpdateEvent { Id = taskId, Artifact = artifact };
                _ = _sseManager.SendUpdateAsync(taskId, artifactEvent, CancellationToken.None); // Fire-and-forget

                return System.Threading.Tasks.Task.FromResult(true);
            }
            return System.Threading.Tasks.Task.FromResult(false); // Concurrent update failed
        }
        return System.Threading.Tasks.Task.FromResult(false); // Task not found
    }

    public Task<bool> AddHistoryMessageAsync(
        string taskId,
        Message message,
        CancellationToken cancellationToken = default)
    {
         ArgumentNullException.ThrowIfNull(message);
         if (_history.TryGetValue(taskId, out var historyList))
         {
            lock(historyList)
            {
                historyList.Add(message);
            }
            return System.Threading.Tasks.Task.FromResult(true);
         }
         // Task might exist in _tasks but not _history if created abnormally, handle defensively
         else if (_tasks.ContainsKey(taskId))
         {
             // Try adding a new history list
             return System.Threading.Tasks.Task.FromResult(_history.TryAdd(taskId, new List<Message> { message }));
         }
         return System.Threading.Tasks.Task.FromResult(false); // Task not found
    }


    public Task<Common.Models.Task?> CancelTaskAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        if (_tasks.TryGetValue(taskId, out var existingTask))
        {
            // Check if task is already in a terminal state
            if (existingTask.Status.State is TaskState.Completed or TaskState.Canceled or TaskState.Failed)
            {
                return System.Threading.Tasks.Task.FromResult<Common.Models.Task?>(null); // Cannot cancel
            }

            var newStatus = new Common.Models.TaskStatus { State = TaskState.Canceled, Timestamp = DateTimeOffset.UtcNow }; // Use initializer
            var updatedTask = existingTask with { Status = newStatus };

            if (_tasks.TryUpdate(taskId, updatedTask, existingTask))
            {
                return System.Threading.Tasks.Task.FromResult<Common.Models.Task?>(updatedTask);
            }
            // Concurrent update failed
            return System.Threading.Tasks.Task.FromResult<Common.Models.Task?>(null);
        }
        return System.Threading.Tasks.Task.FromResult<Common.Models.Task?>(null); // Task not found
    }

    public Task<bool> SetPushNotificationConfigAsync(
        string taskId,
        PushNotificationConfig config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        // Check if task exists before setting config? Optional.
        if (!_tasks.ContainsKey(taskId))
        {
            return System.Threading.Tasks.Task.FromResult(false); // Or throw? For now, fail silently if task doesn't exist.
        }
        _pushConfigs.AddOrUpdate(taskId, config, (id, existing) => config);
        return System.Threading.Tasks.Task.FromResult(true);
    }

    public Task<PushNotificationConfig?> GetPushNotificationConfigAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        _pushConfigs.TryGetValue(taskId, out var config);
        return System.Threading.Tasks.Task.FromResult(config); // Returns null if not found
    }

    // Helper to trigger push notification
    private void TriggerPushNotificationIfNeeded(string taskId, Common.Models.Task updatedTask, TaskState newState)
    {
        // Send push notification for relevant terminal or input states
        if (newState is TaskState.Completed or TaskState.Failed or TaskState.Canceled or TaskState.InputRequired)
        {
             if (_pushConfigs.TryGetValue(taskId, out var pushConfig))
             {
                 _logger.LogInformation("Triggering push notification for Task ID {TaskId} due to state change to {TaskState}", taskId, newState);
                 // Fire-and-forget push notification sending
                 _ = _pushSender.SendNotificationAsync(pushConfig, updatedTask, CancellationToken.None);
             }
        }
    }
}