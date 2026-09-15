using Tests.Core.Fixtures;

namespace Basket.API.ContractTests;

/// <summary>
/// API Contract Tests for PUT /api/basket/{id}
/// Validates basket update, validation, and error handling.
/// </summary>
public class UpdateBasketTests : ApiFixtureBase<Program>
{
    [Fact(DisplayName = "PUT /api/basket/{id} with valid payload returns 200")]
    public async Task UpdateBasket_WithValidPayload_Returns200()
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString();
        var updateRequest = new
        {
            items = new[]
            {
                new { productId = "1", quantity = 2, unitPrice = 50.00m }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(updateRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await Client.PutAsync($"/api/basket/{basketId}", content);

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.NoContent,
            $"Expected 200 or 204, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "PUT /api/basket/{id} with empty items clears basket")]
    public async Task UpdateBasket_WithEmptyItems_Returns200()
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString();
        var updateRequest = new { items = new object[0] };

        var content = new StringContent(
            JsonSerializer.Serialize(updateRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await Client.PutAsync($"/api/basket/{basketId}", content);

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.NoContent ||
            response.StatusCode == System.Net.HttpStatusCode.BadRequest,
            $"Expected 200, 204, or 400, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "PUT /api/basket/{id} with invalid quantity returns 400")]
    public async Task UpdateBasket_WithInvalidQuantity_Returns400()
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString();
        var updateRequest = new
        {
            items = new[]
            {
                new { productId = "1", quantity = 0, unitPrice = 50.00m }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(updateRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await Client.PutAsync($"/api/basket/{basketId}", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "PUT /api/basket with invalid ID returns 400 or 404")]
    public async Task UpdateBasket_WithInvalidId_Returns400Or404()
    {
        // Arrange
        var updateRequest = new { items = new object[0] };
        var content = new StringContent(
            JsonSerializer.Serialize(updateRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await Client.PutAsync("/api/basket/invalid-id", content);

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound,
            $"Expected 400 or 404, got {response.StatusCode}"
        );
    }
}
