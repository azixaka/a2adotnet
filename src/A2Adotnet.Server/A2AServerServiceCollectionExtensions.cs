using A2Adotnet.Common.Models;
using A2Adotnet.Server.Abstractions;
using A2Adotnet.Server.Builder;
using A2Adotnet.Server.Implementations; // For default InMemoryTaskManager
using A2Adotnet.Server.Push; // Added for Push
using A2Adotnet.Server.Sse; // Added for SSE
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Json;

namespace A2Adotnet.Server;

/// <summary>
/// Extension methods for setting up A2A server services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class A2AServerServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core A2A server services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureAgentCard">Action to configure the <see cref="AgentCard"/>.</param>
    /// <returns>An <see cref="IA2AServerBuilder"/> that can be used to further configure the A2A server.</returns>
    public static IA2AServerBuilder AddA2AServer(
        this IServiceCollection services,
        Action<AgentCard> configureAgentCard)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureAgentCard);

        // Configure AgentCard options
        services.Configure(configureAgentCard);

        // Add default JSON options if not already configured by user
        services.TryAddSingleton(sp => sp.GetService<IOptions<JsonSerializerOptions>>()?.Value ?? new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Converters = { new Common.Protocol.Messages.RequestIdConverter() /* Add Part converter if needed */ }
            });

        // Add core services
        services.TryAddScoped<IA2ARequestDispatcher, A2ARequestDispatcher>();

        // Add default Task Manager (in-memory) if none is registered
        services.TryAddSingleton<ITaskManager, InMemoryTaskManager>();

        // Add default SSE Connection Manager (in-memory) if none is registered
        services.TryAddSingleton<ISseConnectionManager, InMemorySseConnectionManager>();

        // Add default Push Notification Sender
        services.TryAddScoped<IPushNotificationSender, HttpPushNotificationSender>();

        // Add IHttpClientFactory for push notifications
        services.AddHttpClient();

        // Automatically discover and register IA2ARequestHandler implementations
        // Scan the entry assembly and potentially referenced assemblies
        // Consider making assembly scanning configurable
        var entryAssembly = Assembly.GetEntryAssembly(); // Or specify assemblies
        if (entryAssembly != null)
        {
            RegisterHandlersFromAssembly(services, entryAssembly);
            foreach (var referencedAssembly in entryAssembly.GetReferencedAssemblies())
            {
                 try
                 {
                    RegisterHandlersFromAssembly(services, Assembly.Load(referencedAssembly));
                 }
                 catch (Exception ex) // Catch potential load errors
                 {
                     System.Diagnostics.Debug.WriteLine($"Could not load assembly {referencedAssembly.FullName} for handler scanning: {ex.Message}");
                 }
            }
        }


        return new A2AServerBuilder(services);
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var handlerInterfaceType = typeof(IA2ARequestHandler<,>);
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            // Register the handler implementation itself (scoped)
            services.AddScoped(handlerType);

            // Also register it as object for discovery by the dispatcher
            services.AddScoped(typeof(object), sp => sp.GetRequiredService(handlerType));
        }
    }

    // --- Builder Extension Methods ---

    /// <summary>
    /// Adds a custom implementation for the <see cref="ITaskManager"/>.
    /// </summary>
    /// <typeparam name="TTaskManager">The type of the task manager implementation.</typeparam>
    /// <param name="builder">The <see cref="IA2AServerBuilder"/>.</param>
    /// <param name="lifetime">The service lifetime (defaults to Singleton).</param>
    /// <returns>The <see cref="IA2AServerBuilder"/>.</returns>
    public static IA2AServerBuilder AddTaskManager<TTaskManager>(
        this IA2AServerBuilder builder,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TTaskManager : class, ITaskManager
    {
        // Remove default registration if it exists
        var defaultTaskManager = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(ITaskManager) && d.ImplementationType == typeof(InMemoryTaskManager));
        if (defaultTaskManager != null)
        {
            builder.Services.Remove(defaultTaskManager);
        }

        // Add the custom one
        builder.Services.Add(new ServiceDescriptor(typeof(ITaskManager), typeof(TTaskManager), lifetime));
        return builder;
    }

     /// <summary>
    /// Adds a specific request handler implementation.
    /// Note: Handlers are typically discovered automatically, but this allows explicit registration.
    /// </summary>
    /// <typeparam name="THandler">The type of the handler implementation.</typeparam>
    /// <param name="builder">The <see cref="IA2AServerBuilder"/>.</param>
    /// <param name="lifetime">The service lifetime (defaults to Scoped).</param>
    /// <returns>The <see cref="IA2AServerBuilder"/>.</returns>
    public static IA2AServerBuilder AddRequestHandler<THandler>(
        this IA2AServerBuilder builder,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class // Assuming handlers implement the generic interface
    {
        builder.Services.Add(new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));
        // Also register as object for discovery
        builder.Services.Add(new ServiceDescriptor(typeof(object), sp => sp.GetRequiredService(typeof(THandler)), lifetime));
        return builder;
    }

    // Add more builder extensions as needed (e.g., AddAgentLogic, ConfigureJsonOptions)
}