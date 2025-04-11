using A2Adotnet.Common.Models;
using A2Adotnet.Server;
using A2Adotnet.Server.Abstractions;
using A2Adotnet.Server.Sse; // For ISseConnectionManager

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLogging();

// Add A2A Server services and configure AgentCard from appsettings
builder.Services.AddA2AServer(options =>
{
    builder.Configuration.GetSection("AgentCard").Bind(options);
});
// AddA2AServer registers ITaskManager (InMemory default), IA2ARequestDispatcher, ISseConnectionManager (InMemory default), IPushNotificationSender (Http default)
// It also scans for and registers IA2ARequestHandler implementations (like SendTaskHandler etc. in the A2Adotnet.Server library)

// Register our simple agent logic implementation
builder.Services.AddScoped<IAgentLogicInvoker, SampleAgentLogic>();


var app = builder.Build();

// Configure the HTTP request pipeline.
// No HTTPS redirection for simple local sample
// app.UseHttpsRedirection();

// Add standard ASP.NET Core auth/authz middleware here if needed

app.UseRouting();

// Map A2A endpoints using the extension methods
app.MapA2AWellKnown();
app.MapA2AEndpoint();
// Note: SSE endpoint is handled implicitly by the SendTaskSubscribe/Resubscribe handlers

app.MapGet("/", () => "A2A Sample Agent Server is running."); // Simple root endpoint

app.Run();


// --- Sample Agent Logic Implementation ---

public class SampleAgentLogic : IAgentLogicInvoker
{
    private readonly ITaskManager _taskManager;
    private readonly ISseConnectionManager _sseManager; // Needed for streaming example
    private readonly ILogger<SampleAgentLogic> _logger;

    public SampleAgentLogic(ITaskManager taskManager, ISseConnectionManager sseManager, ILogger<SampleAgentLogic> logger)
    {
        _taskManager = taskManager;
        _sseManager = sseManager;
        _logger = logger;
    }

    public async Task ProcessTaskAsync(Common.Models.Task task, Message triggeringMessage, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing Task ID: {TaskId}", task.Id);

        // Update status to working
        await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Working, new Message("agent", new List<Part> { new TextPart("Processing your request...") }), cancellationToken);

        var inputText = triggeringMessage.Parts.OfType<TextPart>().FirstOrDefault()?.Text?.ToLowerInvariant() ?? "";
        string? skillId = null; // Determine skill based on input or task metadata if available

        // Simple skill routing based on text content
        if (inputText.Contains("echo")) skillId = "echo";
        else if (inputText.Contains("joke")) skillId = "joke";
        else if (inputText.Contains("story") || inputText.Contains("stream")) skillId = "long-story";

        try
        {
            switch (skillId)
            {
                case "echo":
                    await HandleEchoSkill(task, inputText, cancellationToken);
                    break;
                case "joke":
                    await HandleJokeSkill(task, cancellationToken);
                    break;
                case "long-story":
                    // This runs "synchronously" within the handler context for SendTaskSubscribe
                    // but the handler itself doesn't block the response stream.
                    await HandleStorySkillStreaming(task, cancellationToken);
                    break;
                default:
                    await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Failed, new Message("agent", new List<Part> { new TextPart("Sorry, I didn't understand which skill to use.") }), cancellationToken);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
             _logger.LogInformation("Processing cancelled for Task ID: {TaskId}", task.Id);
             await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Canceled, new Message("agent", new List<Part> { new TextPart("Processing was cancelled.") }), CancellationToken.None);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error processing Task ID: {TaskId}", task.Id);
             await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Failed, new Message("agent", new List<Part> { new TextPart($"An error occurred: {ex.Message}") }), CancellationToken.None);
        }
    }

    private async Task HandleEchoSkill(Common.Models.Task task, string inputText, CancellationToken cancellationToken)
    {
        var echoArtifact = new Artifact(
            Parts: new List<Part> { new TextPart($"Echo: {inputText}") }
        );
        await _taskManager.AddArtifactAsync(task.Id, echoArtifact, cancellationToken);
        await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Completed, null, cancellationToken); // Complete the task
    }

     private async Task HandleJokeSkill(Common.Models.Task task, CancellationToken cancellationToken)
    {
        // Simulate some work
        await Task.Delay(500, cancellationToken);

        var jokeArtifact = new Artifact(
            Name: "joke",
            Parts: new List<Part> { new TextPart("Why don't scientists trust atoms? Because they make up everything!") }
        );
        await _taskManager.AddArtifactAsync(task.Id, jokeArtifact, cancellationToken);
        await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Completed, null, cancellationToken);
    }

     private async Task HandleStorySkillStreaming(Common.Models.Task task, CancellationToken cancellationToken)
    {
        // Simulate streaming parts of a story
        string[] storyParts = {
            "Once upon a time, in a land of circuits and code,",
            " there lived a small robot named Bolt.",
            " Bolt dreamed of seeing the world beyond the server room.",
            " One day, a network glitch opened a path...",
            " Bolt ventured out, discovering the wonders of the internet!"
        };

        // Send initial working status via SSE (triggered by TaskManager update)
        await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Working, new Message("agent", new List<Part> { new TextPart("Starting story stream...") }), cancellationToken);

        for (int i = 0; i < storyParts.Length; i++)
        {
            await Task.Delay(750, cancellationToken); // Simulate time between parts

            var storyArtifact = new Artifact(
                Name: "story",
                Parts: new List<Part> { new TextPart(storyParts[i]) },
                Index: 0, // Send all parts for the same artifact index
                Append: i > 0, // Append after the first part
                LastChunk: i == storyParts.Length - 1 // Mark last chunk
            );
            // This triggers the SSE send via the TaskManager -> SseManager hook
            await _taskManager.AddArtifactAsync(task.Id, storyArtifact, cancellationToken);
        }

        // Send final completed status via SSE
        await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Completed, new Message("agent", new List<Part> { new TextPart("Story finished.") }), cancellationToken);
    }
}