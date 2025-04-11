using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc.Testing; // For WebApplicationFactory
using A2Adotnet.SampleServer; // Reference the server Program/Startup
using A2Adotnet.Client;
using Microsoft.Extensions.DependencyInjection;
using A2Adotnet.Common.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // For List

namespace A2Adotnet.Integration.Tests;

[TestClass]
public class BasicIntegrationTests
{
    private static WebApplicationFactory<Program> _factory = null!; // Use Program from SampleServer
    private IA2AClient _client = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _factory = new WebApplicationFactory<Program>();
    }

    [TestInitialize]
    public void TestInit()
    {
        // Create a client that talks to the in-memory server
        var httpClient = _factory.CreateClient(); // Gets HttpClient pointing to the test server
        var services = new ServiceCollection();
        services.AddSingleton(httpClient); // Provide the HttpClient
        services.AddA2AClient(options =>
        {
            // BaseAddress is handled by WebApplicationFactory's client
        });
        var serviceProvider = services.BuildServiceProvider();
        _client = serviceProvider.GetRequiredService<IA2AClient>();
    }

    [TestMethod]
    public async Task SendTask_Echo_Success()
    {
        // Arrange
        var taskId = "int-test-echo-1";
        var message = new Message("user", new List<Part> { new TextPart("echo test message") });

        // Act
        var resultTask = await _client.SendTaskAsync(taskId, message);

        // Assert
        Assert.IsNotNull(resultTask);
        Assert.AreEqual(taskId, resultTask.Id);
        Assert.AreEqual(TaskState.Completed, resultTask.Status.State);
        Assert.IsNotNull(resultTask.Artifacts);
        Assert.AreEqual(1, resultTask.Artifacts.Count);
        var textPart = resultTask.Artifacts[0].Parts.OfType<TextPart>().FirstOrDefault();
        Assert.IsNotNull(textPart);
        Assert.AreEqual("Echo: echo test message", textPart.Text);
    }

     [TestMethod]
    public async Task SendTask_Joke_Success()
    {
        // Arrange
        var taskId = "int-test-joke-1";
        var message = new Message("user", new List<Part> { new TextPart("tell me a joke") });

        // Act
        var resultTask = await _client.SendTaskAsync(taskId, message);

        // Assert
        Assert.IsNotNull(resultTask);
        Assert.AreEqual(taskId, resultTask.Id);
        Assert.AreEqual(TaskState.Completed, resultTask.Status.State);
        Assert.IsNotNull(resultTask.Artifacts);
        Assert.AreEqual(1, resultTask.Artifacts.Count);
        var textPart = resultTask.Artifacts[0].Parts.OfType<TextPart>().FirstOrDefault();
        Assert.IsNotNull(textPart);
        Assert.IsTrue(textPart.Text.Length > 10); // Basic check for joke content
    }

    [TestMethod]
    public async Task GetTask_AfterSend_Success()
    {
        // Arrange
        var taskId = "int-test-get-1";
        var message = new Message("user", new List<Part> { new TextPart("echo for get test") });
        await _client.SendTaskAsync(taskId, message); // Send first

        // Act
        var resultTask = await _client.GetTaskAsync(taskId);

        // Assert
        Assert.IsNotNull(resultTask);
        Assert.AreEqual(taskId, resultTask.Id);
        Assert.AreEqual(TaskState.Completed, resultTask.Status.State); // Should be completed by now
        Assert.IsNotNull(resultTask.Artifacts);
        Assert.AreEqual(1, resultTask.Artifacts.Count);
    }

    // TODO: Add integration test for SendTaskAndSubscribeAsync (streaming)
    // TODO: Add integration test for CancelTaskAsync
    // TODO: Add integration test for GetAgentCardAsync

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory?.Dispose();
    }
}