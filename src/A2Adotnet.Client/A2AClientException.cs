using A2Adotnet.Common.Protocol.Messages;

namespace A2Adotnet.Client;

/// <summary>
/// Represents errors that occur during A2A client operations, potentially including JSON-RPC error details.
/// </summary>
public class A2AClientException : Exception
{
    /// <summary>
    /// Gets the JSON-RPC error code if the exception resulted from a JSON-RPC error response.
    /// </summary>
    public int? ErrorCode { get; }

    /// <summary>
    /// Gets the JSON-RPC error data if the exception resulted from a JSON-RPC error response.
    /// </summary>
    public object? ErrorData { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="A2AClientException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public A2AClientException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="A2AClientException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public A2AClientException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="A2AClientException"/> class based on a JSON-RPC error response.
    /// </summary>
    /// <param name="errorDetail">The JSON-RPC error detail.</param>
    /// <param name="requestId">The optional request ID associated with the error.</param>
    public A2AClientException(JsonRpcErrorDetail errorDetail, RequestId? requestId = null)
        : base($"A2A request failed{(requestId != null ? $" (ID: {requestId})" : "")}: {errorDetail.Message} (Code: {errorDetail.Code})")
    {
        ArgumentNullException.ThrowIfNull(errorDetail);
        ErrorCode = errorDetail.Code;
        ErrorData = errorDetail.Data;
    }
}