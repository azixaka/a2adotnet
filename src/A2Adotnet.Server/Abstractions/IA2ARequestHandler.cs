using A2Adotnet.Common.Protocol.Messages;
using Microsoft.AspNetCore.Http; // Added for HttpContext access if needed by handlers

namespace A2Adotnet.Server.Abstractions;

/// <summary>
/// Interface for handling a specific A2A JSON-RPC method.
/// </summary>
/// <typeparam name="TParams">The type of the parameters for the method.</typeparam>
/// <typeparam name="TResult">The type of the result for the method.</typeparam>
public interface IA2ARequestHandler<TParams, TResult>
    where TParams : class
    // TResult can be class or struct, allow object? for methods returning null/empty
    // where TResult : class
{
    /// <summary>
    /// Gets the JSON-RPC method name this handler is responsible for (e.g., "tasks/send").
    /// </summary>
    string MethodName { get; }

    /// <summary>
    /// Handles the incoming request.
    /// </summary>
    /// <param name="parameters">The deserialized request parameters.</param>
    /// <param name="context">The HttpContext for the current request (provides access to user, services, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the method invocation.</returns>
    /// <exception cref="A2AServerException">Throw specific A2A server exceptions for defined JSON-RPC errors.</exception>
    /// <exception cref="Exception">Other exceptions will be treated as Internal Errors.</exception>
    Task<TResult?> HandleAsync(TParams parameters, HttpContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Base exception for A2A server-side errors that map to specific JSON-RPC error codes.
/// </summary>
public class A2AServerException : Exception
{
    public int ErrorCode { get; }
    public object? ErrorData { get; }

    public A2AServerException(int errorCode, string message, object? data = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ErrorData = data;
    }
}

/// <summary>
/// Standard JSON-RPC error codes for server-side use.
/// </summary>
public static class A2AErrorCodes
{
    // Standard JSON-RPC
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
    public const int InternalError = -32603;

    // A2A Specific (-32000 to -32099)
    public const int TaskNotFound = -32001;
    public const int TaskNotCancelable = -32002;
    public const int PushNotificationNotSupported = -32003;
    public const int UnsupportedOperation = -32004; // e.g., streaming not supported
    public const int IncompatibleContentTypes = -32005;
    // Add others as needed
}