using A2Adotnet.Common.Models;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace A2Adotnet.Server.Sse;

/// <summary>
/// Helper methods for writing Server-Sent Events (SSE).
/// </summary>
internal static class SseHelper
{
    private static readonly byte[] NewlineBytes = Encoding.UTF8.GetBytes("\n");

    /// <summary>
    /// Writes a task update event to the HTTP response stream in SSE format.
    /// </summary>
    public static async Task WriteSseEventAsync(
        HttpResponse response,
        TaskUpdateEventBase updateEvent,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
    {
        var eventName = updateEvent switch
        {
            TaskStatusUpdateEvent => "TaskStatusUpdateEvent",
            TaskArtifactUpdateEvent => "TaskArtifactUpdateEvent",
            _ => null // Unknown event type
        };

        if (eventName == null) return; // Don't write unknown events

        try
        {
            // Serialize the event data to JSON
            string jsonData = JsonSerializer.Serialize(updateEvent, updateEvent.GetType(), jsonOptions);

            // Write the event name
            await response.WriteAsync($"event: {eventName}\n", cancellationToken);

            // Write the data field(s) - handle multi-line data
            using (var reader = new StringReader(jsonData))
            {
                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                {
                    await response.WriteAsync($"data: {line}\n", cancellationToken);
                }
            }

            // Write the final blank line to signal the end of the event
            await response.WriteAsync("\n", cancellationToken);

            // Flush the response stream to ensure the client receives the event promptly
            await response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected if the client disconnects or cancellation is requested
        }
        catch (Exception ex)
        {
            // Log error? Should be careful about writing to a potentially broken stream.
            System.Diagnostics.Debug.WriteLine($"Error writing SSE event: {ex.Message}");
            // Consider how to handle stream writing errors robustly.
        }
    }

    /// <summary>
    /// Writes a simple SSE comment (keep-alive ping).
    /// </summary>
    public static async Task WriteSseCommentAsync(HttpResponse response, string comment = "", CancellationToken cancellationToken = default)
    {
         try
        {
            await response.WriteAsync($": {comment}\n\n", cancellationToken); // Comment line + blank line
            await response.Body.FlushAsync(cancellationToken);
        }
        catch { /* Ignore errors writing keep-alive */ }
    }

     /// <summary>
    /// Prepares the HttpResponse for an SSE stream.
    /// </summary>
    public static void PrepareSseStream(HttpResponse response)
    {
        response.ContentType = "text/event-stream";
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("Connection", "keep-alive");
        // Optionally set X-Accel-Buffering: no for Nginx environments
    }
}