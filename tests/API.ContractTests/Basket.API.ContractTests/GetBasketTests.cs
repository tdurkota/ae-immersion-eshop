using Tests.Core.Fixtures;

namespace Basket.API.ContractTests;

/// <summary>
/// API Contract Tests for GET /api/basket/{id}
/// Validates response structure, status codes, and error handling.
/// </summary>
public class GetBasketTests : ApiFixtureBase<Program>
{
    [Fact(DisplayName = "GET /api/basket/{id} with valid ID returns 200")]
    public async Task GetBasket_WithValidId_Returns200()
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString();
        
        // Act
        var response = await Client.GetAsync($"/api/basket/{basketId}");
        
        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "GET /api/basket/{id} with invalid ID returns 400 or 404")]
    public async Task GetBasket_WithInvalidId_ReturnsBadRequestOrNotFound()
    {
        // Arrange
        var invalidId = "invalid-guid";
        
        // Act
        var response = await Client.GetAsync($"/api/basket/{invalidId}");
        
        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.BadRequest || 
            response.StatusCode == System.Net.HttpStatusCode.NotFound,
            $"Expected 400 or 404, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "GET /api/basket/{id} with empty/null ID returns 400")]
    public async Task GetBasket_WithEmptyId_Returns400()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/basket/");
        
        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.BadRequest || 
            response.StatusCode == System.Net.HttpStatusCode.NotFound,
            $"Expected 400 or 404, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "GET /api/basket/{id} returns valid JSON response")]
    public async Task GetBasket_Response_IsValidJson()
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString();
        
        // Act
        var response = await Client.GetAsync($"/api/basket/{basketId}");
        
        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content), "Response body should not be empty");
        
        // Verify it's valid JSON by attempting to deserialize
        var json = JsonSerializer.Deserialize<object>(content, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(json);
    }
}
