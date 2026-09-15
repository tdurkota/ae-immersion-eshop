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

    [Fact(Skip = "Implementation in progress", DisplayName = "GET /api/basket/{id} response schema validation")]
    public async Task GetBasket_ResponseSchema_IsValid()
    {
        // TODO: Validate response structure matches OpenAPI spec
        await Task.CompletedTask;
    }
}
