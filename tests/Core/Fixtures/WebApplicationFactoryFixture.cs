using Aspire.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Tests.Core.Fixtures;

/// <summary>
/// Base fixture combining WebApplicationFactory with Aspire DistributedApplication.
/// Manages both the ASP.NET application and container dependencies (PostgreSQL, Redis, RabbitMQ).
/// 
/// Inherit from this class to:
/// - Test APIs with container dependencies
/// - Inject connection strings into the application
/// - Configure custom services or middleware
/// 
/// Example:
/// <code>
/// public class MyApiFixture : WebApplicationFactoryFixture<Program>
/// {
///     protected override void ConfigureDistributedApp(IDistributedApplicationBuilder builder)
///     {
///         var db = builder.AddPostgres("mydb");
///         builder.AddProject<Projects.MyApi>("my-api").WithReference(db);
///     }
/// }
/// </code>
/// </summary>
public abstract class WebApplicationFactoryFixture<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
{
    private IHost? _distributedApp;
    protected ResourceNotificationService? NotificationService { get; private set; }

    /// <summary>
    /// Override to configure Aspire distributed application resources.
    /// Called during InitializeAsync before the application starts.
    /// </summary>
    protected virtual void ConfigureDistributedApp(IDistributedApplicationBuilder builder)
    {
        // Override in derived classes to add resources
    }

    /// <summary>
    /// Override to configure host builder (connection strings, services, etc.).
    /// </summary>
    protected virtual void ConfigureHostBuilder(IHostBuilder builder)
    {
        // Override in derived classes to inject services
    }

    /// <summary>
    /// Get endpoint URL for a named resource (e.g., "http" or "https")
    /// </summary>
    protected async Task<Uri> GetResourceEndpointAsync(string resourceName, string? endpointName = "http")
    {
        if (_distributedApp == null)
            throw new InvalidOperationException("Application not initialized. Call InitializeAsync first.");

        var resource = _distributedApp.Services
            .GetRequiredService<ResourceService>()
            .GetResources()
            .FirstOrDefault(r => r.Name == resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found");

        var endpoint = resource.Annotations
            .OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == endpointName)
            ?? throw new InvalidOperationException($"Endpoint '{endpointName}' not found for resource '{resourceName}'");

        return endpoint.AllocatedEndpoint?.Uri
            ?? throw new InvalidOperationException($"Endpoint not allocated for '{resourceName}/{endpointName}'");
    }

    /// <summary>
    /// Get connection string for a resource
    /// </summary>
    protected async Task<string> GetResourceConnectionStringAsync(string resourceName)
    {
        if (_distributedApp == null)
            throw new InvalidOperationException("Application not initialized. Call InitializeAsync first.");

        var resource = _distributedApp.Services
            .GetRequiredService<ResourceService>()
            .GetResources()
            .FirstOrDefault(r => r.Name == resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found");

        if (resource is not IResourceWithConnectionString resourceWithConnStr)
            throw new InvalidOperationException($"Resource '{resourceName}' does not support connection strings");

        return await resourceWithConnStr.GetConnectionStringAsync();
    }

    /// <summary>
    /// Wait for a resource to be healthy
    /// </summary>
    protected async Task WaitForResourceHealthyAsync(string resourceName)
    {
        if (NotificationService == null)
            throw new InvalidOperationException("NotificationService not initialized");

        await NotificationService.WaitForResourceHealthyAsync(resourceName);
    }

    public async Task InitializeAsync()
    {
        var options = new DistributedApplicationOptions
        {
            AssemblyName = typeof(TProgram).Assembly.FullName ?? "test",
            DisableDashboard = true
        };
        var builder = DistributedApplication.CreateBuilder(options);

        // Allow derived classes to configure resources
        ConfigureDistributedApp(builder);

        _distributedApp = builder.Build();
        NotificationService = _distributedApp.Services.GetRequiredService<ResourceNotificationService>();

        // Start the distributed app
        await _distributedApp.StartAsync();

        // Override CreateHost to inject configuration
        await Task.CompletedTask;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Allow derived classes to configure the host
        ConfigureHostBuilder(builder);

        return base.CreateHost(builder);
    }

    public async Task DisposeAsync()
    {
        await base.DisposeAsync();

        if (_distributedApp is not null)
        {
            await _distributedApp.StopAsync();
            if (_distributedApp is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _distributedApp.Dispose();
            }
        }
    }
}
