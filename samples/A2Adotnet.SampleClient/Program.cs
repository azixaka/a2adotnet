using A2Adotnet.Client;
using A2Adotnet.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers; // For AuthenticationHeaderValue

// Use HostBuilder for configuration and DI
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Configure A2AClient options from appsettings.json
        services.AddOptions<A2AClientOptions>()
            .Bind(context.Configuration.GetSection("A2AClient"));

        // Add A2AClient with configuration
        services.AddA2AClient(options =>
        {
            // Example: Configure dynamic authentication header (e.g., read API key from config)
            // Replace this with your actual authentication mechanism
            /*
            options.GetAuthenticationHeaderAsync = async () => {
                await Task.CompletedTask; // Simulate async work if needed
                var apiKey = context.Configuration["A2AClient:ApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    // Assuming ApiKey scheme - adjust scheme as needed (e.g., "Bearer")
                    return new AuthenticationHeaderValue("ApiKey", apiKey);
                }
                return null;
            };
            */
        });

        // Add a hosted service to run the client logic
        services.AddHostedService<ClientRunner>();
    })
    .Build();

await host.RunAsync();


// Hosted service to run the client examples
public class ClientRunner : BackgroundService
{
    private readonly ILogger<ClientRunner> _logger;
    private readonly IA2AClient _a2aClient;

    public ClientRunner(ILogger<ClientRunner> logger, IA2AClient a2aClient)
    {
        _logger = logger;
        _a2aClient = a2aClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("A2A Sample Client starting.");

        try
        {
            // --- Example 0: Get Agent Card ---
            _logger.LogInformation("--- Getting Agent Card ---");
            try
            {
                var agentCard = await _a2aClient.GetAgentCardAsync(stoppingToken);
                _logger.LogInformation("Agent Name: {Name}, Version: {Version}", agentCard.Name, agentCard.Version);
                _logger.LogInformation("Agent Supports Streaming: {Streaming}", agentCard.Capabilities.Streaming);
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Failed to get Agent Card.");
            }


            // --- Example 1: Simple Send/Receive ---
            _logger.LogInformation("\n--- Example 1: Simple Send/Receive ---");
            var taskId1 = Guid.NewGuid().ToString();
            var message1 = new Message("user", new List<Part> { new TextPart("Tell me a simple joke") });

            try
            {
                var finalTask1 = await _a2aClient.SendTaskAsync(taskId1, message1, cancellationToken: stoppingToken);
                LogTaskResult(finalTask1);
            }
            catch (Exception ex)
            {
                LogClientError(ex, "Example 1");
            }

            // --- Example 2: Streaming ---
             _logger.LogInformation("\n--- Example 2: Streaming ---");
            var taskId2 = Guid.NewGuid().ToString();
            var message2 = new Message("user", new List<Part> { new TextPart("Write a very short story about a cat.") });

            try
            {
                _logger.LogInformation("Subscribing to updates for Task ID: {TaskId}", taskId2);
                await foreach (var updateEvent in _a2aClient.SendTaskAndSubscribeAsync(taskId2, message2, cancellationToken: stoppingToken))
                {
                    if (updateEvent is TaskStatusUpdateEvent statusUpdate)
                    {
                        _logger.LogInformation("SSE Status Update: {State} - {Message}",
                            statusUpdate.Status.State,
                            statusUpdate.Status.Message?.Parts?.OfType<TextPart>().FirstOrDefault()?.Text ?? "(no message)");
                        if (statusUpdate.Final) _logger.LogInformation("-- SSE Stream Ended --");
                    }
                    else if (updateEvent is TaskArtifactUpdateEvent artifactUpdate)
                    {
                        var textPart = artifactUpdate.Artifact.Parts.OfType<TextPart>().FirstOrDefault();
                        _logger.LogInformation("SSE Artifact Update (Index {Index}, Append: {Append}): {Text}",
                            artifactUpdate.Artifact.Index, artifactUpdate.Artifact.Append, textPart?.Text ?? "(non-text part)");
                    }
                }
                 _logger.LogInformation("Finished consuming SSE stream for Task ID: {TaskId}", taskId2);
            }
            catch (Exception ex)
            {
                LogClientError(ex, "Example 2");
            }

            // --- Example 3: Multi-Turn (Input Required) ---
            // This requires a running agent that actually enters the input-required state.
            // Skipping interactive console input for this basic example.
            _logger.LogInformation("\n--- Example 3: Multi-Turn (Conceptual) ---");
            _logger.LogInformation("Skipping interactive multi-turn example.");

            // --- Example 4: Get Task ---
            _logger.LogInformation("\n--- Example 4: Get Task ---");
             try
            {
                // Try to get the status of the first task again
                var fetchedTask1 = await _a2aClient.GetTaskAsync(taskId1, historyLength: 5, cancellationToken: stoppingToken);
                 _logger.LogInformation("Fetched Task ID {TaskId} again.", taskId1);
                LogTaskResult(fetchedTask1);
            }
            catch (Exception ex)
            {
                LogClientError(ex, "Example 4");
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred in the client runner.");
        }

        _logger.LogInformation("A2A Sample Client finished.");
    }

    private void LogTaskResult(Common.Models.Task task)
    {
         _logger.LogInformation("Task {TaskId} finished with state: {TaskState}", task.Id, task.Status.State);
         if (task.Status.State == TaskState.Completed && task.Artifacts?.Any() == true)
         {
             foreach(var artifact in task.Artifacts)
             {
                 var textPart = artifact.Parts.OfType<TextPart>().FirstOrDefault();
                 _logger.LogInformation("  Artifact (Name: {ArtifactName}): {Text}", artifact.Name ?? "(unnamed)", textPart?.Text ?? "(non-text part)");
             }
         }
         else if (task.Status.Message != null)
         {
              var textPart = task.Status.Message.Parts.OfType<TextPart>().FirstOrDefault();
              _logger.LogInformation("  Status Message: {Text}", textPart?.Text ?? "(non-text part)");
         }
    }

     private void LogClientError(Exception ex, string exampleName)
     {
          if (ex is A2AClientException a2aEx)
          {
              _logger.LogError(ex, "{ExampleName} failed with A2A Error Code {ErrorCode}: {ErrorMessage}", exampleName, a2aEx.ErrorCode, a2aEx.Message);
          }
          else
          {
               _logger.LogError(ex, "{ExampleName} failed with unexpected error.", exampleName);
          }
     }
}