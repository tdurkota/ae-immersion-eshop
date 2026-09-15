using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.Core.Fixtures;

/// <summary>
/// Base fixture for API contract tests using WebApplicationFactory.
/// Provides HTTP client and common test utilities for API endpoint validation.
/// </summary>
public abstract class ApiFixtureBase<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
{
    protected HttpClient Client { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        Client = CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Helper: Assert response has expected status code
    /// </summary>
    protected static void AssertStatusCode(HttpStatusCode actual, HttpStatusCode expected, string? message = null)
    {
        Assert.True(actual == expected, message ?? $"Expected {expected}, got {actual}");
    }

    /// <summary>
    /// Helper: Deserialize JSON response to T
    /// </summary>
    protected static async Task<T> ReadAsJsonAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonSerializer.Deserialize<T>(content, 
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return json ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}
