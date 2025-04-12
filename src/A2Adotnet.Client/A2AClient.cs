using A2Adotnet.Common.Models;
using A2Adotnet.Common.Protocol.Messages;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Adotnet.Client;

/// <summary>
/// Options for configuring the A2AClient.
/// </summary>
public class A2AClientOptions
{
    /// <summary>
    /// The base URL of the A2A agent server (e.g., "https://my-agent.example.com/").
    /// The A2A endpoint path (typically "/a2a") will be appended.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Optional delegate to provide authentication headers dynamically for each request.
    /// </summary>
    public Func<Task<AuthenticationHeaderValue?>>? GetAuthenticationHeaderAsync { get; set; }

    /// <summary>
    /// Optional custom JsonSerializerOptions. If null, default options are used.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }
}

/// <summary>
/// Default implementation of the IA2AClient interface.
/// </summary>
public class A2AClient : IA2AClient
{
    private readonly HttpClient _httpClient;
    private readonly A2AClientOptions _options;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private long _requestIdCounter = 0; // Simple counter for request IDs

    // Constants
    private const string DefaultA2AEndpointPath = "/a2a"; // Or make configurable?
    private const string WellKnownAgentCardPath = "/.well-known/agent.json";

    public A2AClient(HttpClient httpClient, IOptions<A2AClientOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (_options.BaseAddress != null)
        {
            _httpClient.BaseAddress = _options.BaseAddress;
        }
        else if (_httpClient.BaseAddress == null)
        {
            // BaseAddress is crucial, either set it on HttpClient registration or via options.
            throw new ArgumentException("HttpClient must have a BaseAddress set, or BaseAddress must be provided in A2AClientOptions.", nameof(options));
        }

        // Configure JsonSerializerOptions
        _jsonSerializerOptions = _options.JsonSerializerOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Match JSON-RPC style
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Optimize payload size
            // Add any necessary converters (like for RequestId, Part polymorphism)
            Converters = { new RequestIdConverter() /* Add Part converter if needed */ }
        };
        // Ensure the Part converter is added if using custom converter factory
        // Or rely on [JsonDerivedType] attributes if using .NET 7+
    }

    // --- Method Implementations (To be added in subsequent tasks) ---

    public Task<AgentCard> GetAgentCardAsync(CancellationToken cancellationToken = default)
    {
        // Implementation for fetching /.well-known/agent.json
        throw new NotImplementedException();
    }

    public async Task<Common.Models.Task> SendTaskAsync(
        string taskId,
        Message message,
        string? sessionId = null,
        PushNotificationConfig? pushNotificationConfig = null,
        int? historyLength = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        ArgumentNullException.ThrowIfNull(message);

        var parameters = new TaskSendParams
        {
            Id = taskId,
            Message = message,
            SessionId = sessionId,
            PushNotification = pushNotificationConfig,
            HistoryLength = historyLength,
            Metadata = metadata
        };

        // tasks/send returns the final Task object
        var result = await SendRpcRequestAsync<TaskSendParams, Common.Models.Task>(
            "tasks/send",
            parameters,
            cancellationToken);

        // The RPC helper throws on error, so if we get here, result should not be null
        // according to the spec (tasks/send returns Task).
        // Add null check just for safety, though it indicates a spec violation by the server if null.
        return result ?? throw new A2AClientException($"Server returned null result for tasks/send (Task ID: {taskId}).");
    }

    public async Task<Common.Models.Task> GetTaskAsync(
        string taskId,
        int? historyLength = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);

        var parameters = new TaskQueryParams
        {
            Id = taskId,
            HistoryLength = historyLength,
            Metadata = metadata
        };

        // tasks/get returns the current Task object
        var result = await SendRpcRequestAsync<TaskQueryParams, Common.Models.Task>(
            "tasks/get",
            parameters,
            cancellationToken);

        // The RPC helper throws on error, so if we get here, result should not be null
        // according to the spec (tasks/get returns Task).
        return result ?? throw new A2AClientException($"Server returned null result for tasks/get (Task ID: {taskId}).");
    }

    public async Task<Common.Models.Task> CancelTaskAsync(
        string taskId,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);

        var parameters = new TaskIdParams
        {
            Id = taskId,
            Metadata = metadata
        };

        // tasks/cancel returns the updated Task object (potentially with 'canceled' state)
        var result = await SendRpcRequestAsync<TaskIdParams, Common.Models.Task>(
            "tasks/cancel",
            parameters,
            cancellationToken);

        // The RPC helper throws on error (e.g., TaskNotCancelableError), so if we get here, result should not be null.
        return result ?? throw new A2AClientException($"Server returned null result for tasks/cancel (Task ID: {taskId}).");
    }

     public async Task<TaskPushNotificationConfig> SetPushNotificationAsync(
        string taskId,
        PushNotificationConfig config,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        ArgumentNullException.ThrowIfNull(config);

        var parameters = new TaskPushNotificationConfig // Re-use the record type for params
        {
            Id = taskId,
            PushNotificationConfig = config
        };

        // tasks/pushNotification/set returns the confirmed TaskPushNotificationConfig object
        var result = await SendRpcRequestAsync<TaskPushNotificationConfig, TaskPushNotificationConfig>(
            "tasks/pushNotification/set",
            parameters,
            cancellationToken);

        // The RPC helper throws on error, so if we get here, result should not be null.
        return result ?? throw new A2AClientException($"Server returned null result for tasks/pushNotification/set (Task ID: {taskId}).");
    }

    public async Task<TaskPushNotificationConfig> GetPushNotificationAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);

        var parameters = new TaskIdParams // Re-use TaskIdParams
        {
            Id = taskId
        };

        // tasks/pushNotification/get returns the current TaskPushNotificationConfig object
        var result = await SendRpcRequestAsync<TaskIdParams, TaskPushNotificationConfig>(
            "tasks/pushNotification/get",
            parameters,
            cancellationToken);

        // The RPC helper throws on error. Result could potentially be null if no config is set,
        // but the spec implies it returns the object or an error. Let's handle potential null.
        // If the server follows spec strictly and errors when no config exists, this null check might be redundant.
        return result ?? throw new A2AClientException($"Server returned null result for tasks/pushNotification/get, implying no configuration exists or an error occurred (Task ID: {taskId}).");
    }

    public async IAsyncEnumerable<TaskUpdateEventBase> SendTaskAndSubscribeAsync(
        string taskId,
        Message message,
        string? sessionId = null,
        PushNotificationConfig? pushNotificationConfig = null,
        int? historyLength = null,
        Dictionary<string, object>? metadata = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        ArgumentNullException.ThrowIfNull(message);

        var parameters = new TaskSendParams
        {
            Id = taskId,
            Message = message,
            SessionId = sessionId,
            PushNotification = pushNotificationConfig,
            HistoryLength = historyLength,
            Metadata = metadata
        };

        var requestId = GenerateRequestId();
        var rpcRequest = new JsonRpcRequest<TaskSendParams>
        {
            Id = requestId, // Although SSE is notification-like, the initial request might need an ID for errors
            Method = "tasks/sendSubscribe",
            Params = parameters
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, DefaultA2AEndpointPath);
        await AddAuthenticationHeaderAsync(requestMessage).ConfigureAwait(false);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        requestMessage.Content = JsonContent.Create(rpcRequest, typeof(JsonRpcRequest<TaskSendParams>), options: _jsonSerializerOptions);

        HttpResponseMessage response;
        try
        {
            // Send the request and ensure we get headers first
            response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new A2AClientException($"A2A network request failed for method 'tasks/sendSubscribe'.", ex);
        }
        catch (OperationCanceledException)
        {
            // If cancellation happens before getting response, just let it propagate
            throw;
        }
        catch (Exception ex)
        {
             throw new A2AClientException($"An unexpected error occurred during A2A request setup for method 'tasks/sendSubscribe'.", ex);
        }

        // Check for non-success status code *before* trying to read the stream
        if (!response.IsSuccessStatusCode)
        {
            // Attempt to read error details from body if possible
            string errorBody = string.Empty;
            try { errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false); } catch { /* Ignore read error */ }
            try
            {
                 if (!string.IsNullOrWhiteSpace(errorBody))
                 {
                    var errorResponse = JsonSerializer.Deserialize<A2AErrorResponse>(errorBody, _jsonSerializerOptions);
                    if (errorResponse?.Error != null)
                    {
                        throw new A2AClientException(errorResponse.Error, errorResponse.Id);
                    }
                 }
            }
            catch (JsonException ex)
            {
                 throw new A2AClientException($"A2A request 'tasks/sendSubscribe' failed with status code {response.StatusCode}. Failed to parse error response: {errorBody}", ex);
            }
            // If no JSON error, throw general exception
            response.EnsureSuccessStatusCode(); // This will throw HttpRequestException
        }

        // Process the SSE stream
        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        string? eventName = null;
        var dataBuilder = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

            if (line == null) // End of stream
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line)) // Blank line indicates end of an event
            {
                if (eventName != null && dataBuilder.Length > 0)
                {
                    var data = dataBuilder.ToString();
                    TaskUpdateEventBase? updateEvent = null;
                    try
                    {
                        updateEvent = eventName switch
                        {
                            "TaskStatusUpdateEvent" => JsonSerializer.Deserialize<TaskStatusUpdateEvent>(data, _jsonSerializerOptions),
                            "TaskArtifactUpdateEvent" => JsonSerializer.Deserialize<TaskArtifactUpdateEvent>(data, _jsonSerializerOptions),
                            // Add other event types if defined by spec later
                            _ => null // Unknown event type
                        };
                    }
                    catch (JsonException ex)
                    {
                         // Log warning about invalid data? Or throw? For now, yield null or skip.
                         // Consider adding logging infrastructure later.
                         System.Diagnostics.Debug.WriteLine($"Failed to deserialize SSE data for event '{eventName}': {ex.Message}");
                    }

                    if (updateEvent != null)
                    {
                        yield return updateEvent;

                        // Check if the status update event signals the end
                        if (updateEvent is TaskStatusUpdateEvent statusEvent && statusEvent.Final)
                        {
                            yield break; // Stop iteration
                        }
                    }
                }
                // Reset for next event
                eventName = null;
                dataBuilder.Clear();
            }
            else if (line.StartsWith("event:"))
            {
                eventName = line.Substring("event:".Length).Trim();
            }
            else if (line.StartsWith("data:"))
            {
                dataBuilder.AppendLine(line.Substring("data:".Length).Trim());
            }
            // Ignore other lines like 'id:', 'retry:', comments (':')
        }
    }

    public async IAsyncEnumerable<TaskUpdateEventBase> ResubscribeAsync(
        string taskId,
        int? historyLength = null,
        Dictionary<string, object>? metadata = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);

        var parameters = new TaskQueryParams
        {
            Id = taskId,
            HistoryLength = historyLength,
            Metadata = metadata
        };

        var requestId = GenerateRequestId(); // Generate ID for potential error correlation
        var rpcRequest = new JsonRpcRequest<TaskQueryParams>
        {
            Id = requestId,
            Method = "tasks/resubscribe",
            Params = parameters
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, DefaultA2AEndpointPath);
        await AddAuthenticationHeaderAsync(requestMessage).ConfigureAwait(false);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        requestMessage.Content = JsonContent.Create(rpcRequest, typeof(JsonRpcRequest<TaskQueryParams>), options: _jsonSerializerOptions);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new A2AClientException($"A2A network request failed for method 'tasks/resubscribe'.", ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
             throw new A2AClientException($"An unexpected error occurred during A2A request setup for method 'tasks/resubscribe'.", ex);
        }

        // Check for non-success status code before reading stream
        if (!response.IsSuccessStatusCode)
        {
            string errorBody = string.Empty;
            try { errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false); } catch { /* Ignore */ }
            try
            {
                 if (!string.IsNullOrWhiteSpace(errorBody))
                 {
                    var errorResponse = JsonSerializer.Deserialize<A2AErrorResponse>(errorBody, _jsonSerializerOptions);
                    if (errorResponse?.Error != null)
                    {
                        throw new A2AClientException(errorResponse.Error, errorResponse.Id);
                    }
                 }
            }
            catch (JsonException ex)
            {
                 throw new A2AClientException($"A2A request 'tasks/resubscribe' failed with status code {response.StatusCode}. Failed to parse error response: {errorBody}", ex);
            }
            response.EnsureSuccessStatusCode();
        }

        // Process the SSE stream (Identical logic to SendTaskAndSubscribeAsync)
        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        string? eventName = null;
        var dataBuilder = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

            if (line == null) break;

            if (string.IsNullOrWhiteSpace(line))
            {
                if (eventName != null && dataBuilder.Length > 0)
                {
                    var data = dataBuilder.ToString();
                    TaskUpdateEventBase? updateEvent = null;
                    try
                    {
                        updateEvent = eventName switch
                        {
                            "TaskStatusUpdateEvent" => JsonSerializer.Deserialize<TaskStatusUpdateEvent>(data, _jsonSerializerOptions),
                            "TaskArtifactUpdateEvent" => JsonSerializer.Deserialize<TaskArtifactUpdateEvent>(data, _jsonSerializerOptions),
                            _ => null
                        };
                    }
                    catch (JsonException ex)
                    {
                         System.Diagnostics.Debug.WriteLine($"Failed to deserialize SSE data for event '{eventName}': {ex.Message}");
                    }

                    if (updateEvent != null)
                    {
                        yield return updateEvent;
                        if (updateEvent is TaskStatusUpdateEvent statusEvent && statusEvent.Final)
                        {
                            yield break;
                        }
                    }
                }
                eventName = null;
                dataBuilder.Clear();
            }
            else if (line.StartsWith("event:"))
            {
                eventName = line.Substring("event:".Length).Trim();
            }
            else if (line.StartsWith("data:"))
            {
                // Append line data, removing potential leading space and adding newline for multi-line data
                dataBuilder.AppendLine(line.Substring("data:".Length).TrimStart());
            }
        }
    }


    // --- Helper Methods ---

    private RequestId GenerateRequestId()
    {
        // Simple incrementing ID for this client instance. Consider alternatives for distributed scenarios.
        return new RequestId(Interlocked.Increment(ref _requestIdCounter));
    }

    private async System.Threading.Tasks.Task AddAuthenticationHeaderAsync(HttpRequestMessage request)
    {
        if (_options.GetAuthenticationHeaderAsync != null)
        {
            request.Headers.Authorization = await _options.GetAuthenticationHeaderAsync();
        }
    }

    /// <summary>
    /// Sends a JSON-RPC request and processes the response, handling success and error cases.
    /// </summary>
    private async Task<TResult?> SendRpcRequestAsync<TParams, TResult>(
        string method,
        TParams parameters,
        CancellationToken cancellationToken)
        where TParams : class
        where TResult : class // Allow null result for methods that might return null
    {
        var requestId = GenerateRequestId();
        var rpcRequest = new JsonRpcRequest<TParams>
        {
            Id = requestId,
            Method = method,
            Params = parameters
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, DefaultA2AEndpointPath);
        await AddAuthenticationHeaderAsync(requestMessage).ConfigureAwait(false);

        try
        {
            // Using System.Net.Http.Json for convenience
            requestMessage.Content = JsonContent.Create(rpcRequest, typeof(JsonRpcRequest<TParams>), options: _jsonSerializerOptions);

            using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            // Read content stream for potential deserialization
            await using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                // Attempt to deserialize as success first
                try
                {
                    var successResponse = await JsonSerializer.DeserializeAsync<A2AResponse<TResult>>(contentStream, _jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
                    if (successResponse != null && successResponse.Id == requestId)
                    {
                        return successResponse.Result;
                    }
                    // Fall through to error handling if ID doesn't match or structure is wrong
                }
                catch (JsonException) { /* Ignore if it doesn't fit success schema */ }

                // Reset stream position and try deserializing as error (unexpected for success status code, but possible)
                contentStream.Position = 0;
                try
                {
                    var errorResponse = await JsonSerializer.DeserializeAsync<A2AErrorResponse>(contentStream, _jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
                    if (errorResponse?.Error != null)
                    {
                        throw new A2AClientException(errorResponse.Error, errorResponse.Id);
                    }
                }
                catch (JsonException) { /* Ignore if it doesn't fit error schema */ }

                // If we reach here, the response was successful but couldn't be parsed correctly
                throw new A2AClientException($"Received successful status code ({response.StatusCode}) but failed to deserialize A2A response for request ID {requestId}.");
            }
            else // Non-success status code
            {
                // Attempt to deserialize as JSON-RPC error
                try
                {
                    var errorResponse = await JsonSerializer.DeserializeAsync<A2AErrorResponse>(contentStream, _jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
                    if (errorResponse?.Error != null)
                    {
                        // Throw specific exception based on JSON-RPC error
                        throw new A2AClientException(errorResponse.Error, errorResponse.Id);
                    }
                }
                catch (JsonException ex)
                {
                    // If error deserialization fails, throw a general exception with HTTP details
                    throw new A2AClientException($"A2A request failed with status code {response.StatusCode}. Failed to parse error response.", ex);
                }
                // If error deserialization succeeded but Error object was null (invalid JSON-RPC)
                throw new A2AClientException($"A2A request failed with status code {response.StatusCode}. Received invalid error response structure.");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new A2AClientException($"A2A network request failed for method '{method}'.", ex);
        }
        catch (JsonException ex)
        {
            // Catch serialization errors specifically
             throw new A2AClientException($"Failed to serialize request or deserialize response for method '{method}'.", ex);
        }
        // Catch A2AClientException specifically to avoid re-wrapping
        catch (A2AClientException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) // Don't wrap cancellation
        {
            // Catch any other unexpected exceptions
            throw new A2AClientException($"An unexpected error occurred during A2A request for method '{method}'.", ex);
        }
    }
}