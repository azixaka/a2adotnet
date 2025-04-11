using A2Adotnet.Common.Models;
using A2Adotnet.Common.Protocol.Messages; // Added
using A2Adotnet.Server.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options; // Added for IOptionsMonitor
using System.Text.Json;

namespace A2Adotnet.Server;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to map A2A endpoints.
/// </summary>
public static class A2AEndpointRouteBuilderExtensions
{
    // Constants for default paths
    private const string DefaultA2AEndpointPath = "/a2a";
    private const string DefaultWellKnownAgentCardPath = "/.well-known/agent.json";

    /// <summary>
    /// Maps the standard A2A JSON-RPC endpoint (typically POST /a2a).
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="path">The path to map the endpoint to. Defaults to "/a2a".</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder MapA2AEndpoint( // Reverted return type
        this IEndpointRouteBuilder endpoints,
        string path = DefaultA2AEndpointPath)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(path);

        // Map the endpoint. Metadata can be added later if OpenAPI support is integrated.
        return endpoints.MapPost(path, HandleA2ARequestAsync);
    }

    /// <summary>
    /// Maps the standard A2A Agent Card endpoint (typically GET /.well-known/agent.json).
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="path">The path to map the endpoint to. Defaults to "/.well-known/agent.json".</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder MapA2AWellKnown( // Reverted return type
        this IEndpointRouteBuilder endpoints,
        string path = DefaultWellKnownAgentCardPath)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(path);

        var routeBuilder = endpoints.MapGet(path, async (HttpContext context) =>
        {
            var agentCardOptions = context.RequestServices.GetService<IOptionsMonitor<AgentCard>>(); // Use IOptionsMonitor
            var jsonOptions = context.RequestServices.GetService<IOptions<JsonSerializerOptions>>()?.Value // Get configured options
                              ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };



            if (agentCardOptions?.CurrentValue == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("AgentCard configuration not found.");
                return;
            }

            // Ensure URL in card matches request context if needed? Or assume configured correctly.
            // For now, just return the configured card.

            context.Response.ContentType = "application/json; charset=utf-8";
            await JsonSerializer.SerializeAsync(context.Response.Body, agentCardOptions.CurrentValue, jsonOptions, context.RequestAborted);
        });
        // Metadata can be added later if OpenAPI support is integrated.
        // routeBuilder.Produces<AgentCard>(StatusCodes.Status200OK, "application/json");
        // routeBuilder.Produces(StatusCodes.Status404NotFound);

        return routeBuilder; // routeBuilder here is IEndpointConventionBuilder from MapGet
    }

    // Placeholder for the actual request handler logic (will be implemented in the dispatcher task)
    private static async System.Threading.Tasks.Task HandleA2ARequestAsync(HttpContext context)
    {
        // 1. Get the IA2ARequestDispatcher service
        // 2. Call dispatcher.DispatchRequestAsync(context)
        // This keeps the mapping clean and delegates logic.

        var dispatcher = context.RequestServices.GetService<IA2ARequestDispatcher>(); // Define this interface later
        if (dispatcher == null)
        {
             context.Response.StatusCode = StatusCodes.Status500InternalServerError;
             await context.Response.WriteAsync("A2A Request Dispatcher not configured.");
             return;
        }

        await dispatcher.DispatchRequestAsync(context);
    }

    // TODO: Add MapA2ASseEndpoint if implementing SSE support directly here
    // Or handle SSE connection upgrade within the main HandleA2ARequestAsync dispatcher logic.
}

// Define placeholder interface for the dispatcher (to be implemented later)
public interface IA2ARequestDispatcher
{
    System.Threading.Tasks.Task DispatchRequestAsync(HttpContext context); // Must return Task
}