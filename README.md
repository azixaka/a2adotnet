![A2A Banner](images/A2A_banner.png)

# A2Adotnet: A C#/.NET Implementation of the A2A Protocol

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/your-repo/A2Adotnet) <!-- Replace with actual build status badge -->
[![License](https://img.shields.io/badge/license-Apache%202.0-blue)](LICENSE) <!-- Assuming Apache 2.0, update if different -->

This repository contains a C#/.NET implementation of the **Agent-to-Agent (A2A) protocol**.

## What is A2A?

The Agent-to-Agent (A2A) protocol is an open standard initiated by Google designed to enable communication and interoperability between disparate AI agent systems. The core goal is to allow agents built on different frameworks or by different vendors to discover each other's capabilities, negotiate interaction modes (text, forms, files, etc.), and collaborate on tasks securely.

This project provides libraries for building both A2A clients and servers in a .NET environment.

**Reference:**
*   **Google's A2A Project (Python/JS):** [https://github.com/google/A2A](https://github.com/google/A2A)
*   **A2A Specification:** (Link to the spec within this repo or the official one if available)

## Features

*   **A2Adotnet.Common:** Contains shared models representing the A2A protocol objects (AgentCard, Task, Message, Part, Artifact, etc.) based on the JSON schema.
*   **A2Adotnet.Client:** A library for building A2A clients capable of interacting with A2A servers. Supports standard request/response, SSE streaming, and push notification configuration.
*   **A2Adotnet.Server:** ASP.NET Core integration components for building A2A servers. Includes request dispatching, handler abstractions, SSE connection management, and push notification sending capabilities.
*   **Samples:** Example client and server applications demonstrating usage.

## Getting Started

### Prerequisites

*   .NET 8 SDK or later

### Building the Solution

1.  Clone the repository:
    ```bash
    git clone https://github.com/your-repo/A2Adotnet.git # Replace with actual repo URL
    cd A2Adotnet
    ```
2.  Build the solution using the .NET CLI:
    ```bash
    dotnet build A2Adotnet.sln
    ```
3.  (Optional) Run tests:
    ```bash
    dotnet test A2Adotnet.sln
    ```

## Usage Samples

### Running the Sample Server

The sample server demonstrates hosting a basic A2A agent with echo, joke, and streaming capabilities.

1.  Navigate to the sample server directory:
    ```bash
    cd samples/A2Adotnet.SampleServer
    ```
2.  Run the server:
    ```bash
    dotnet run
    ```
    The server will typically start listening on `http://localhost:5123` (check console output). The Agent Card should be available at `http://localhost:5123/.well-known/agent.json` and the A2A endpoint at `http://localhost:5123/a2a`.

### Running the Sample Client

The sample client demonstrates interacting with an A2A agent using the client library.

1.  Ensure the Sample Server (or another A2A agent) is running.
2.  Update the `BaseAddress` in `samples/A2Adotnet.SampleClient/appsettings.json` if your server is running on a different URL.
3.  Navigate to the sample client directory:
    ```bash
    cd samples/A2Adotnet.SampleClient
    ```
4.  Run the client:
    ```bash
    dotnet run
    ```
    The client will execute several predefined interactions with the agent (get card, send task, stream task) and log the results to the console.

### Basic Client Code Snippet

```csharp
// --- In your Program.cs or Startup.cs ---
using A2Adotnet.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Configure options (e.g., from appsettings.json)
        services.AddOptions<A2AClientOptions>()
            .Bind(context.Configuration.GetSection("A2AClient"));

        // Add the A2A client
        services.AddA2AClient();

        // Register your service that uses IA2AClient
        services.AddTransient<MyAgentInteractor>();
    })
    .Build();

var interactor = host.Services.GetRequiredService<MyAgentInteractor>();
await interactor.RunAsync();

// --- Your service using the client ---
using A2Adotnet.Client;
using A2Adotnet.Common.Models;

public class MyAgentInteractor
{
    private readonly IA2AClient _a2aClient;

    public MyAgentInteractor(IA2AClient a2aClient)
    {
        _a2aClient = a2aClient;
    }

    public async Task RunAsync()
    {
        try
        {
            var taskId = Guid.NewGuid().ToString();
            var message = new Message("user", new List<Part> { new TextPart("Tell me a joke") });
            var resultTask = await _a2aClient.SendTaskAsync(taskId, message);

            Console.WriteLine($"Task {resultTask.Id} completed with status: {resultTask.Status.State}");
            // Process resultTask.Artifacts...
        }
        catch (A2AClientException ex)
        {
            Console.WriteLine($"A2A Client Error: {ex.Message}");
        }
    }
}
```

### Basic Server Code Snippet

```csharp
// --- In your ASP.NET Core Program.cs ---
using A2Adotnet.Server;
using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions; // For IAgentLogicInvoker

var builder = WebApplication.CreateBuilder(args);

// Add A2A Server services
builder.Services.AddA2AServer(options =>
{
    // Configure AgentCard, typically binding from appsettings.json
    builder.Configuration.GetSection("AgentCard").Bind(options);
    // Example manual configuration:
    // options.Name = "My A2A Agent";
    // options.Url = "https://my-agent.example.com/a2a";
    // options.Version = "1.0";
    // options.Capabilities = new AgentCapabilities { Streaming = true };
    // options.Skills = new List<AgentSkill> { /* ... */ };
});

// Register your agent logic implementation
builder.Services.AddScoped<IAgentLogicInvoker, MyAgentLogic>();
// AddA2AServer registers default ITaskManager, ISseConnectionManager etc.
// Use builder extensions like .AddTaskManager<MyDbTaskManager>() to override defaults.

var app = builder.Build();

app.UseRouting();
// Add Auth middleware if needed: app.UseAuthentication(); app.UseAuthorization();

// Map A2A endpoints
app.MapA2AWellKnown(); // Serves /.well-known/agent.json
app.MapA2AEndpoint();  // Serves /a2a (or configured path)

app.Run();

// --- Your Agent Logic Implementation ---
public class MyAgentLogic : IAgentLogicInvoker
{
    private readonly ITaskManager _taskManager;
    // Inject other services like ISseConnectionManager if needed

    public MyAgentLogic(ITaskManager taskManager)
    {
        _taskManager = taskManager;
    }

    public async Task ProcessTaskAsync(Common.Models.Task task, Message triggeringMessage, CancellationToken cancellationToken)
    {
        // 1. Update status to working
        await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Working, null, cancellationToken);

        // 2. Perform agent work based on triggeringMessage
        var resultText = "Processed: " + triggeringMessage.Parts.OfType<TextPart>().FirstOrDefault()?.Text;
        var resultArtifact = new Artifact(Parts: new List<Part> { new TextPart(resultText) });

        // 3. Add artifacts and set final status
        await _taskManager.AddArtifactAsync(task.Id, resultArtifact, cancellationToken);
        await _taskManager.UpdateTaskStatusAsync(task.Id, TaskState.Completed, null, cancellationToken);
    }
}

```

## Contributing

Contributions are welcome! Please see the [CONTRIBUTING.md](CONTRIBUTING.md) file for guidelines. (You might need to create this file).

## License

This project is licensed under the [Apache 2.0 License](LICENSE).