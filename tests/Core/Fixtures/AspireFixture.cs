using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Tests.Core.Fixtures;

/// <summary>
/// Base fixture for testing non-web services using Aspire DistributedApplication.
/// Manages container lifecycle for services, databases, and message queues.
/// 
/// Use this for:
/// - Background service testing
/// - Message queue consumers
/// - Standalone microservices
/// - Container orchestration scenarios
/// 
/// Example:
/// <code>
/// public class OrderProcessorFixture : AspireFixture
/// {
///     public IResourceBuilder<RabbitMQServerResource> RabbitMQ { get; private set; }
///     public IResourceBuilder<PostgresServerResource> Database { get; private set; }
///     
///     protected override void ConfigureDistributedApp(IDistributedApplicationBuilder builder)
///     {
///         RabbitMQ = builder.AddRabbitMQ("rabbitmq");
///         Database = builder.AddPostgres("orderdb");
///     }
/// }
/// </code>
/// </summary>
public abstract class AspireFixture : IAsyncLifetime
{
    private IHost? _app;
    protected ResourceNotificationService? NotificationService { get; private set; }
    protected ResourceService? ResourceService { get; private set; }

    /// <summary>
    /// Override to configure resources (databases, caches, message queues, services)
    /// </summary>
    protected virtual void ConfigureDistributedApp(IDistributedApplicationBuilder builder)
    {
        // Override in derived classes
    }

    /// <summary>
    /// Get endpoint URL for a named resource
    /// </summary>
    protected async Task<Uri> GetResourceEndpointAsync(string resourceName, string endpointName = "http")
    {
        EnsureInitialized();

        var resource = ResourceService!
            .GetResources()
            .FirstOrDefault(r => r.Name == resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found");

        var endpoint = resource.Annotations
            .OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == endpointName)
            ?? throw new InvalidOperationException($"Endpoint '{endpointName}' not found on '{resourceName}'");

        return endpoint.AllocatedEndpoint?.Uri
            ?? throw new InvalidOperationException($"Endpoint not allocated for '{resourceName}'");
    }

    /// <summary>
    /// Get connection string for a resource (PostgreSQL, Redis, RabbitMQ, etc.)
    /// </summary>
    protected async Task<string> GetResourceConnectionStringAsync(string resourceName)
    {
        EnsureInitialized();

        var resource = ResourceService!
            .GetResources()
            .FirstOrDefault(r => r.Name == resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found");

        if (resource is not IResourceWithConnectionString withConnStr)
            throw new InvalidOperationException($"Resource '{resourceName}' does not have a connection string");

        return await withConnStr.GetConnectionStringAsync();
    }

    /// <summary>
    /// Wait for a resource to report healthy status
    /// </summary>
    protected async Task WaitForResourceHealthyAsync(string resourceName)
    {
        EnsureInitialized();
        await NotificationService!.WaitForResourceHealthyAsync(resourceName);
    }

    /// <summary>
    /// Get a resource by name
    /// </summary>
    protected IResource? GetResource(string resourceName)
    {
        EnsureInitialized();
        return ResourceService!
            .GetResources()
            .FirstOrDefault(r => r.Name == resourceName);
    }

    public async Task InitializeAsync()
    {
        var options = new DistributedApplicationOptions
        {
            AssemblyName = GetType().Assembly.FullName ?? "test",
            DisableDashboard = true
        };
        var builder = DistributedApplication.CreateBuilder(options);

        // Configure resources
        ConfigureDistributedApp(builder);

        _app = builder.Build();
        NotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();
        ResourceService = _app.Services.GetRequiredService<ResourceService>();

        // Start the application
        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            if (_app is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _app.Dispose();
            }
        }
    }

    private void EnsureInitialized()
    {
        if (_app == null)
            throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");
    }
}
