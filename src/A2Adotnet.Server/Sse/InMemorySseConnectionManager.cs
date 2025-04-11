using A2Adotnet.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace A2Adotnet.Server.Sse;

/// <summary>
/// In-memory implementation of ISseConnectionManager using ConcurrentDictionary.
/// Suitable for single-instance deployments.
/// </summary>
public class InMemorySseConnectionManager : ISseConnectionManager
{
    // taskId -> List of active HttpContexts for that task
    private readonly ConcurrentDictionary<string, List<HttpContext>> _connections = new();
    private readonly ILogger<InMemorySseConnectionManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public InMemorySseConnectionManager(ILogger<InMemorySseConnectionManager> logger, IOptions<JsonSerializerOptions>? jsonOptions = null)
    {
        _logger = logger;
        _jsonOptions = jsonOptions?.Value ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters = { new Common.Protocol.Messages.RequestIdConverter() /* Add Part converter if needed */ }
        };
    }

    public async Task AddConnectionAsync(string taskId, HttpContext context, CancellationToken cancellationToken)
    {
        SseHelper.PrepareSseStream(context.Response);

        var connectionList = _connections.AddOrUpdate(taskId,
            addValueFactory: _ => new List<HttpContext> { context },
            updateValueFactory: (_, existingList) =>
            {
                lock (existingList) // Lock for modification
                {
                    existingList.Add(context);
                }
                return existingList;
            });

        _logger.LogInformation("SSE connection added for Task ID: {TaskId}. Total connections for task: {Count}", taskId, connectionList.Count);

        // Keep the connection alive until cancelled
        try
        {
            // Send initial comment/ping? Optional.
            // await SseHelper.WriteSseCommentAsync(context.Response, "connected", cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                // Keep-alive mechanism (e.g., send a comment periodically)
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken); // Adjust interval as needed
                await SseHelper.WriteSseCommentAsync(context.Response, "ping", cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE connection cancelled for Task ID: {TaskId}", taskId);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error in SSE keep-alive loop for Task ID: {TaskId}", taskId);
        }
        finally
        {
            RemoveConnection(taskId, context);
        }
    }

    public void RemoveConnection(string taskId, HttpContext context)
    {
        if (_connections.TryGetValue(taskId, out var connectionList))
        {
            bool removed = false;
            lock (connectionList)
            {
               removed = connectionList.Remove(context);
            }

            if (removed)
            {
                 _logger.LogInformation("SSE connection removed for Task ID: {TaskId}. Remaining connections: {Count}", taskId, connectionList.Count);
                 // Optional: Clean up dictionary entry if list becomes empty
                 if (connectionList.Count == 0)
                 {
                     // Attempt to remove the key if the list is empty (handle potential race condition)
                     _connections.TryRemove(taskId, out _);
                 }
            }
        }
    }

    public async Task SendUpdateAsync(string taskId, TaskUpdateEventBase updateEvent, CancellationToken cancellationToken = default)
    {
        if (_connections.TryGetValue(taskId, out var connectionList))
        {
            List<HttpContext> connectionsToSend;
            lock (connectionList) // Lock to safely copy the list
            {
                connectionsToSend = new List<HttpContext>(connectionList); // Copy to avoid holding lock during I/O
            }

            if (connectionsToSend.Count > 0)
            {
                 _logger.LogDebug("Sending SSE event '{EventType}' to {Count} connections for Task ID: {TaskId}",
                    updateEvent.GetType().Name, connectionsToSend.Count, taskId);

                // Send to all connections concurrently
                var sendTasks = connectionsToSend.Select(async context =>
                {
                    try
                    {
                        // Check if connection is still valid before writing
                        if (!context.RequestAborted.IsCancellationRequested)
                        {
                            await SseHelper.WriteSseEventAsync(context.Response, updateEvent, _jsonOptions, context.RequestAborted);
                        }
                        else
                        {
                            // Connection closed, remove it
                            RemoveConnection(taskId, context);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send SSE update to a connection for Task ID: {TaskId}. Removing connection.", taskId);
                        RemoveConnection(taskId, context); // Remove potentially broken connection
                    }
                });
                await Task.WhenAll(sendTasks);
            }
        }
        else
        {
             _logger.LogDebug("No active SSE connections found for Task ID: {TaskId} when trying to send update.", taskId);
        }
    }
}