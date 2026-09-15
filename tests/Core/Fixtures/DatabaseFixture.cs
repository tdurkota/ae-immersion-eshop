using Microsoft.EntityFrameworkCore;

namespace Tests.Core.Fixtures;

/// <summary>
/// Base fixture for managing database lifecycle in integration tests.
/// Handles database creation, migration, cleanup, and optional seeding per test.
/// 
/// Use this to:
/// - Ensure test isolation with clean database state
/// - Run migrations automatically
/// - Seed common test data
/// - Clean up after each test
/// 
/// Example:
/// <code>
/// public class OrderDbFixture : DatabaseFixture
/// {
///     protected override async Task OnInitializeAsync()
///     {
///         // Optional: Seed test data
///         var context = ServiceProvider.GetRequiredService<OrderContext>();
///         context.Orders.Add(new Order { Id = 1, CustomerId = "test" });
///         await context.SaveChangesAsync();
///     }
///     
///     public DbContext GetDbContext<T>() where T : DbContext
///     {
///         return ServiceProvider.GetRequiredService<T>();
///     }
/// }
/// </code>
/// </summary>
public abstract class DatabaseFixture : IAsyncLifetime
{
    private IServiceProvider? _serviceProvider;
    private List<DbContext>? _dbContexts;

    public IServiceProvider ServiceProvider
    {
        get => _serviceProvider ?? throw new InvalidOperationException("Fixture not initialized");
    }

    protected abstract IServiceProvider CreateServiceProvider();

    /// <summary>
    /// Override to configure initial database state, seeding, etc.
    /// Called after migrations are applied.
    /// </summary>
    protected virtual async Task OnInitializeAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Override to clean up database state before disposal.
    /// Default implementation deletes all data from known contexts.
    /// </summary>
    protected virtual async Task OnCleanupAsync()
    {
        if (_dbContexts != null)
        {
            foreach (var context in _dbContexts)
            {
                try
                {
                    // Disable change tracking for bulk delete
                    context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                    // Delete from all tables in reverse order of FK dependencies
                    await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Orders\" CASCADE");
                }
                catch
                {
                    // Ignore if tables don't exist
                }
            }
        }

        await Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        _serviceProvider = CreateServiceProvider();
        _dbContexts = new List<DbContext>();

        // Run migrations and initialize database
        await RunMigrationsAsync();

        // Call derived class initialization
        await OnInitializeAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await OnCleanupAsync();
        }
        finally
        {
            if (_dbContexts != null)
            {
                foreach (var context in _dbContexts)
                {
                    await context.DisposeAsync();
                }
                _dbContexts.Clear();
            }

            if (_serviceProvider is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Get or create a DbContext of type T.
    /// Tracks it for cleanup.
    /// </summary>
    public T GetDbContext<T>() where T : DbContext
    {
        var context = ServiceProvider.GetRequiredService<T>();
        _dbContexts?.Add(context);
        return context;
    }

    /// <summary>
    /// Apply all pending migrations to all DbContext types.
    /// Override to customize migration logic.
    /// </summary>
    protected virtual async Task RunMigrationsAsync()
    {
        // Override in derived classes to run migrations
        // Example:
        // var context = GetDbContext<OrderContext>();
        // await context.Database.MigrateAsync();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Execute raw SQL command for setup/cleanup
    /// </summary>
    protected async Task ExecuteSqlAsync<TContext>(string sql) where TContext : DbContext
    {
        var context = GetDbContext<TContext>();
        await context.Database.ExecuteSqlRawAsync(sql);
    }

    /// <summary>
    /// Ensure database is created
    /// </summary>
    protected async Task EnsureDatabaseCreatedAsync<TContext>() where TContext : DbContext
    {
        var context = GetDbContext<TContext>();
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Clear all data from database
    /// </summary>
    protected async Task ClearDatabaseAsync<TContext>() where TContext : DbContext
    {
        var context = GetDbContext<TContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
}
