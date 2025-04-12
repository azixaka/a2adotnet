using A2Adotnet.Common.Protocol.Messages;
using A2Adotnet.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // Added for logging
using Microsoft.Extensions.Options;
using System.Buffers; // Added for PipeReader
using System.IO.Pipelines; // Added for PipeReader
using System.Reflection;
using System.Collections.Concurrent; // Added
using System.Text.Json;
using System.Text.Json.Nodes; // Added for partial deserialization
using System.Text; // Added for Encoding

namespace A2Adotnet.Server;

/// <summary>
/// Default implementation of IA2ARequestDispatcher. Handles deserialization,
/// method dispatching, handler execution, and response serialization.
/// </summary>
internal class A2ARequestDispatcher : IA2ARequestDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<A2ARequestDispatcher> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    // Cache handler information for performance
    private static readonly ConcurrentDictionary<string, HandlerInfo> _handlerCache = new();

    private record HandlerInfo(Type HandlerType, Type ParamsType, Type ResultType, MethodInfo HandleMethod);

    public A2ARequestDispatcher(IServiceProvider serviceProvider, ILogger<A2ARequestDispatcher> logger, IOptions<JsonSerializerOptions>? jsonOptions = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _jsonSerializerOptions = jsonOptions?.Value ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters = { new RequestIdConverter() /* Add Part converter if needed */ }
        };
    }

    public async Task DispatchRequestAsync(HttpContext context)
    {
        RequestId? requestId = null; // Keep track of ID for error responses
        string requestBody;

        try
        {
            // Read request body efficiently using PipeReader
            requestBody = await ReadRequestBodyAsync(context.Request.BodyReader, context.RequestAborted);

            // Attempt partial deserialization to get method and ID
            var requestNode = JsonNode.Parse(requestBody);
            if (requestNode == null)
            {
                await WriteErrorResponseAsync(context, null, A2AErrorCodes.ParseError, "Failed to parse JSON request body.");
                return;
            }

            string? method = requestNode["method"]?.GetValue<string>();
            requestId = TryGetRequestId(requestNode);

            if (string.IsNullOrEmpty(method))
            {
                await WriteErrorResponseAsync(context, requestId, A2AErrorCodes.InvalidRequest, "Request is missing 'method' property.");
                return;
            }

            // Find handler based on method name
            var handlerInfo = GetHandlerInfo(method);
            if (handlerInfo == null)
            {
                await WriteErrorResponseAsync(context, requestId, A2AErrorCodes.MethodNotFound, $"Method '{method}' not found.");
                return;
            }

            // Deserialize parameters
            object? parameters = null;
            var paramsNode = requestNode["params"];
            if (paramsNode != null)
            {
                try
                {
                    parameters = paramsNode.Deserialize(handlerInfo.ParamsType, _jsonSerializerOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize parameters for method '{Method}'.", method);
                    await WriteErrorResponseAsync(context, requestId, A2AErrorCodes.InvalidParams, $"Invalid parameters for method '{method}': {ex.Message}");
                    return;
                }
            }

            // Parameters might be optional for some methods, check if required but null
            if (parameters == null && handlerInfo.ParamsType != typeof(object)) // Assuming object means optional/no params
            {
                 // Check if the handler expects non-null params
                 // This check might need refinement based on how handlers declare optional params
                 // For now, assume null is invalid if ParamsType is specific
                 await WriteErrorResponseAsync(context, requestId, A2AErrorCodes.InvalidParams, $"Missing parameters for method '{method}'.");
                 return;
            }


            // Get handler instance from DI scope
            // Using ActivatorUtilities allows constructor injection for handlers
            var handlerInstance = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, handlerInfo.HandlerType);
            if (handlerInstance == null)
            {
                 _logger.LogError("Could not create handler instance for type {HandlerType}", handlerInfo.HandlerType.FullName);
                 await WriteErrorResponseAsync(context, requestId, A2AErrorCodes.InternalError, $"Could not resolve handler for method '{method}'.");
                 return;
            }

            // Invoke the handler's HandleAsync method
            try
            {
                // Use reflection to call the generic HandleAsync method
                var task = (Task?)handlerInfo.HandleMethod.Invoke(handlerInstance, new object?[] { parameters, context, context.RequestAborted });

                if (task == null)
                {
                     throw new InvalidOperationException($"Handler for method '{method}' returned null Task.");
                }

                await task;

                // Get result from the Task<TResult>
                // Need to handle Task<T> vs Task (if any handlers return void/Task)
                object? result = null;
                if (handlerInfo.ResultType != typeof(void)) // Check if handler has a return type
                {
                    // Access the Result property of the completed Task<TResult>
                    var resultProperty = task.GetType().GetProperty("Result");
                    if (resultProperty != null)
                    {
                        result = resultProperty.GetValue(task);
                    }
                }


                // Write success response ONLY if the handler didn't already handle the response stream (like SSE)
                // We identify SSE handlers by checking if their result type is 'object' (as defined in the handler)
                // and the actual returned result is null. A more robust check might involve specific attributes or interfaces.
                // If we successfully invoked a handler for a request (not notification), ID must be present.
                if (requestId == null)
                {
                    // This indicates a logic error or spec violation if a non-notification request succeeded without an ID.
                    _logger.LogError("Successfully handled method '{Method}' but request ID was unexpectedly null.", method);
                    throw new A2AServerException(A2AErrorCodes.InternalError, "Internal error: Request ID missing after successful handler execution.");
                }

                // Write success response ONLY if the handler didn't already handle the response stream (like SSE)
                // We identify SSE handlers by checking if their result type is 'object' (as defined in the handler)
                // and the actual returned result is null. A more robust check might involve specific attributes or interfaces.
                if (!(handlerInfo.ResultType == typeof(object) && result == null))
                {
                    await WriteSuccessResponseAsync(context, requestId.Value, result);
                }
                else
                {
                    // For SSE handlers that returned null, the response stream is already being managed.
                    _logger.LogDebug("Handler for method '{Method}' completed, SSE stream management assumed.", method);
                }
            }
            catch (TargetInvocationException ex) when (ex.InnerException is A2AServerException a2aEx)
            {
                 // Handle specific A2A errors thrown by the handler
                 await WriteErrorResponseAsync(context, requestId, a2aEx.ErrorCode, a2aEx.Message, a2aEx.ErrorData);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                 // Handle other exceptions thrown by the handler
                 _logger.LogError(ex.InnerException, "Error executing handler for method '{Method}'.", method);
                 await WriteErrorResponseAsync(context, requestId, A2AErrorCodes.InternalError, $"Internal server error executing method '{method}'.", ex.InnerException.Message);
            }
             catch (A2AServerException a2aEx) // Catch exceptions thrown directly before invocation
            {
                 await WriteErrorResponseAsync(context, requestId, a2aEx.ErrorCode, a2aEx.Message, a2aEx.ErrorData);
            }
            catch (Exception ex)
            {
                 // Catch unexpected errors during handler invocation
                 _logger.LogError(ex, "Unexpected error invoking handler for method '{Method}'.", method);
                 await WriteErrorResponseAsync(context, requestId, A2AErrorCodes.InternalError, "Internal server error.", ex.Message);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON parsing error in request body.");
            await WriteErrorResponseAsync(context, requestId, A2AErrorCodes.ParseError, $"JSON parse error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error dispatching A2A request.");
            await WriteErrorResponseAsync(context, requestId, A2AErrorCodes.InternalError, "An unexpected internal server error occurred.");
        }
    }

    private static async Task<string> ReadRequestBodyAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        // Efficiently read the request body using PipeReader
        // Adapted from ASP.NET Core internal examples
        while (true)
        {
            ReadResult result = await reader.ReadAsync(cancellationToken);
            ReadOnlySequence<byte> buffer = result.Buffer;

            // Process the buffer
            if (TryGetJsonString(ref buffer, out var jsonString))
            {
                // Mark the buffer as examined
                reader.AdvanceTo(buffer.Start, buffer.End);
                return jsonString;
            }

            // Mark the buffer as examined
            reader.AdvanceTo(buffer.Start, buffer.End);

            // If the buffer is complete, break
            if (result.IsCompleted)
            {
                // Should have found the JSON string if valid
                break;
            }
        }
        // If loop completes without finding JSON (e.g., empty body)
        return string.Empty; // Or throw? Depends on whether empty body is valid JSON-RPC
    }

     private static bool TryGetJsonString(ref ReadOnlySequence<byte> buffer, out string jsonString)
    {
        // Check if the buffer contains a complete JSON object/array
        // This is a simplified check; robust parsing might need more complex state machine
        var reader = new Utf8JsonReader(buffer, isFinalBlock: false, state: default);
        if (JsonDocument.TryParseValue(ref reader, out _))
        {
            // If TryParseValue succeeds, the buffer up to reader.BytesConsumed contains valid JSON
            var jsonSequence = buffer.Slice(0, reader.BytesConsumed);
            jsonString = Encoding.UTF8.GetString(jsonSequence);
            // Advance the original buffer past the consumed JSON
            buffer = buffer.Slice(reader.BytesConsumed);
            return true;
        }

        jsonString = string.Empty;
        return false;
    }


    private RequestId? TryGetRequestId(JsonNode node)
    {
        var idNode = node["id"];
        if (idNode == null) return null;

        try
        {
            if (idNode is JsonValue idValue)
            {
                 if (idValue.TryGetValue<string>(out var stringId)) return new RequestId(stringId);
                 if (idValue.TryGetValue<long>(out var longId)) return new RequestId(longId);
                 if (idValue.TryGetValue<int>(out var intId)) return new RequestId(intId);
            }
        }
        catch (Exception ex)
        {
             _logger.LogWarning(ex, "Failed to extract RequestId from JSON node.");
        }
        return null; // Return null if ID is present but invalid type or parsing fails
    }


    private HandlerInfo? GetHandlerInfo(string method)
    {
        if (_handlerCache.TryGetValue(method, out var cachedInfo))
        {
            return cachedInfo;
        }

        // Find handler types implementing IA2ARequestHandler<,>
        // This requires scanning assemblies or having handlers registered in DI.
        // Assuming handlers are registered with their specific interface type.
        var handlerGenericType = typeof(IA2ARequestHandler<,>);
        var handlers = _serviceProvider.GetServices<object>() // Get all registered services
            .Where(s => s.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerGenericType))
            .ToList();

        foreach (var handler in handlers)
        {
            var handlerType = handler.GetType();
            var handlerInterface = handlerType.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerGenericType);
            var handlerMethodName = (string?)handlerType.GetProperty(nameof(IA2ARequestHandler<object, object>.MethodName))?.GetValue(handler);

            if (handlerMethodName == method)
            {
                var genericArgs = handlerInterface.GetGenericArguments();
                var paramsType = genericArgs[0];
                var resultType = genericArgs[1];
                var handleMethod = handlerInterface.GetMethod(nameof(IA2ARequestHandler<object, object>.HandleAsync));

                if (handleMethod == null) continue; // Should not happen

                var info = new HandlerInfo(handlerType, paramsType, resultType, handleMethod);
                _handlerCache.TryAdd(method, info);
                return info;
            }
        }

        _logger.LogWarning("No handler found for method '{Method}'.", method);
        return null;
    }

    private async Task WriteSuccessResponseAsync(HttpContext context, RequestId id, object? result)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = StatusCodes.Status200OK;

        // Need to create the generic response type dynamically
        Type resultType = result?.GetType() ?? typeof(object); // Use object if result is null
        Type responseType = typeof(A2AResponse<>).MakeGenericType(resultType);

        // Create an instance of A2AResponse<TResult>
        var responseInstance = Activator.CreateInstance(responseType);
        responseType.GetProperty(nameof(A2AResponse<object>.JsonRpc))?.SetValue(responseInstance, "2.0");
        responseType.GetProperty(nameof(A2AResponse<object>.Id))?.SetValue(responseInstance, id);
        responseType.GetProperty(nameof(A2AResponse<object>.Result))?.SetValue(responseInstance, result);


        await JsonSerializer.SerializeAsync(context.Response.Body, responseInstance, responseType, _jsonSerializerOptions, context.RequestAborted);
    }

    private async Task WriteErrorResponseAsync(HttpContext context, RequestId? id, int code, string message, object? data = null)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        // Determine appropriate status code based on JSON-RPC error code?
        // Simple approach: Use 400 for parse/request errors, 500 for others.
        context.Response.StatusCode = (code == A2AErrorCodes.ParseError || code == A2AErrorCodes.InvalidRequest || code == A2AErrorCodes.MethodNotFound || code == A2AErrorCodes.InvalidParams)
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;


        var errorDetail = new JsonRpcErrorDetail { Code = code, Message = message, Data = data };
        var errorResponse = new A2AErrorResponse { Id = id, Error = errorDetail }; // Id can be null here

        await JsonSerializer.SerializeAsync(context.Response.Body, errorResponse, _jsonSerializerOptions, context.RequestAborted);
    }
}