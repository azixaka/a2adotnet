using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions; // For ITaskManager, IAgentLogicHandler etc. (to be defined)
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
    public static IEndpointConventionBuilder MapA2AEndpoint(
        this IEndpointRouteBuilder endpoints,
        string path = DefaultA2AEndpointPath)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(path);

        // Require authorization by default? Or leave it to the user's app setup?
        // For now, let's map it directly. Users can add .RequireAuthorization() if needed.
        var route = endpoints.MapPost(path, HandleA2ARequestAsync)
            .Accepts<object>("application/json") // Accepts generic JSON
            .Produces(StatusCodes.Status200OK, contentType: "application/json") // Success
            .Produces(StatusCodes.Status400BadRequest, contentType: "application/json") // JSON-RPC parse/request errors
            .Produces(StatusCodes.Status500InternalServerError, contentType: "application/json"); // Internal errors

        // Add specific error codes if needed via ProducesResponseType
        // .Produces<A2AErrorResponse>(StatusCodes.Status400BadRequest, "application/json")

        return route;
    }

    /// <summary>
    /// Maps the standard A2A Agent Card endpoint (typically GET /.well-known/agent.json).
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="path">The path to map the endpoint to. Defaults to "/.well-known/agent.json".</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder MapA2AWellKnown(
        this IEndpointRouteBuilder endpoints,
        string path = DefaultWellKnownAgentCardPath)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(path);

        var route = endpoints.MapGet(path, async (HttpContext context) =>
        {
            var agentCardOptions = context.RequestServices.GetService<IOptions<AgentCard>>();
            var jsonOptions = context.RequestServices.GetService<IOptions<JsonSerializerOptions>>()?.Value
                              ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };


            if (agentCardOptions?.Value == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("AgentCard configuration not found.");
                return;
            }

            // Ensure URL in card matches request context if needed? Or assume configured correctly.
            // For now, just return the configured card.

            context.Response.ContentType = "application/json; charset=utf-8";
            await JsonSerializer.SerializeAsync(context.Response.Body, agentCardOptions.Value, jsonOptions, context.RequestAborted);
        })
        .Produces<AgentCard>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status404NotFound);

        return route;
    }

    // Placeholder for the actual request handler logic (will be implemented in the dispatcher task)
    private static async Task HandleA2ARequestAsync(HttpContext context)
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
    Task DispatchRequestAsync(HttpContext context);
}