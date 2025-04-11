using Microsoft.VisualStudio.TestTools.UnitTesting;
using A2Adotnet.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using A2Adotnet.Common.Models; // Add specific models used in tests
using A2Adotnet.Common.Protocol.Messages;

namespace A2Adotnet.Client.Tests;

// Helper class for mocking HttpMessageHandler responses
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }
}


[TestClass]
public class ClientTests
{
    private JsonSerializerOptions _jsonOptions = null!;

     [TestInitialize]
    public void Setup()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters = { new RequestIdConverter() /* Add Part converter if needed */ }
        };
    }

    private IA2AClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOptions<A2AClientOptions>>(Options.Create(new A2AClientOptions { BaseAddress = new Uri("http://testhost/") }));

        // Configure HttpClient to use the mock handler
        services.AddHttpClient("A2AClient")
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler(handlerFunc));

        services.AddTransient<IA2AClient, A2AClient>(); // Register the concrete type

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IA2AClient>();
    }

    [TestMethod]
    public async Task SendTaskAsync_Success()
    {
        // Arrange
        var expectedTaskId = "task-1";
        var expectedResultTask = new Common.Models.Task { Id = expectedTaskId, Status = new TaskStatus(TaskState.Completed) };
        var handlerFunc = (HttpRequestMessage req, CancellationToken ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            // Match request ID later if needed, for now assume it's correct
            var successResponse = new A2AResponse<Common.Models.Task> { Id = 1, Result = expectedResultTask }; // Assume ID 1 for simplicity
            response.Content = new StringContent(JsonSerializer.Serialize(successResponse, _jsonOptions));
            return Task.FromResult(response);
        };
        var client = CreateClient(handlerFunc);
        var message = new Message("user", new List<Part> { new TextPart("test") });

        // Act
        var result = await client.SendTaskAsync(expectedTaskId, message);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedTaskId, result.Id);
        Assert.AreEqual(TaskState.Completed, result.Status.State);
    }

    [TestMethod]
    public async Task SendTaskAsync_RpcError()
    {
        // Arrange
         var expectedTaskId = "task-err";
         var expectedErrorCode = A2AErrorCodes.TaskNotFound;
         var expectedErrorMessage = "Task not found";
         var handlerFunc = (HttpRequestMessage req, CancellationToken ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK); // JSON-RPC errors often use 200 OK
            var errorResponse = new A2AErrorResponse { Id = 1, Error = new JsonRpcErrorDetail { Code = expectedErrorCode, Message = expectedErrorMessage } };
            response.Content = new StringContent(JsonSerializer.Serialize(errorResponse, _jsonOptions));
            return Task.FromResult(response);
        };
        var client = CreateClient(handlerFunc);
        var message = new Message("user", new List<Part> { new TextPart("test") });

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<A2AClientException>(() => client.SendTaskAsync(expectedTaskId, message));
        Assert.AreEqual(expectedErrorCode, ex.ErrorCode);
        Assert.IsTrue(ex.Message.Contains(expectedErrorMessage));
    }

     [TestMethod]
    public async Task SendTaskAsync_HttpError()
    {
        // Arrange
         var expectedTaskId = "task-http-err";
         var handlerFunc = (HttpRequestMessage req, CancellationToken ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            response.Content = new StringContent("Server Error"); // Non-JSON response
            return Task.FromResult(response);
        };
        var client = CreateClient(handlerFunc);
        var message = new Message("user", new List<Part> { new TextPart("test") });

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<A2AClientException>(() => client.SendTaskAsync(expectedTaskId, message));
        Assert.IsTrue(ex.Message.Contains("500")); // Check for status code in message
        Assert.IsNull(ex.ErrorCode); // No JSON-RPC error code
    }


    // TODO: Add tests for GetTaskAsync
    // TODO: Add tests for CancelTaskAsync
    // TODO: Add tests for Set/Get PushNotificationAsync
    // TODO: Add tests for SendTaskAndSubscribeAsync (more complex, might need dedicated setup)
    // TODO: Add tests for ResubscribeAsync
    // TODO: Add tests for GetAgentCardAsync
    // TODO: Add tests for authentication header handling
}