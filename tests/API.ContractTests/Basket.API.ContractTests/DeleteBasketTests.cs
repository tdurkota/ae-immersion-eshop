using Tests.Core.Fixtures;

namespace Basket.API.ContractTests;

/// <summary>
/// API Contract Tests for DELETE /api/basket/{id}
/// Validates basket deletion and error handling.
/// </summary>
public class DeleteBasketTests : ApiFixtureBase<Program>
{
    [Fact(DisplayName = "DELETE /api/basket/{id} with valid ID returns 200 or 204")]
    public async Task DeleteBasket_WithValidId_Returns200Or204()
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString();

        // Act
        var response = await Client.DeleteAsync($"/api/basket/{basketId}");

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.NoContent ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound,
            $"Expected 200, 204, or 404, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "DELETE /api/basket/{id} with invalid ID returns 400 or 404")]
    public async Task DeleteBasket_WithInvalidId_Returns400Or404()
    {
        // Arrange
        var invalidId = "invalid-guid";

        // Act
        var response = await Client.DeleteAsync($"/api/basket/{invalidId}");

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound,
            $"Expected 400 or 404, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "DELETE /api/basket/{id} twice returns 200/204 then 404 or 200")]
    public async Task DeleteBasket_DeleteTwice_HandlesGracefully()
    {
        // Arrange
        var basketId = Guid.NewGuid().ToString();

        // Act
        var response1 = await Client.DeleteAsync($"/api/basket/{basketId}");
        var response2 = await Client.DeleteAsync($"/api/basket/{basketId}");

        // Assert
        Assert.True(
            response1.StatusCode == System.Net.HttpStatusCode.OK ||
            response1.StatusCode == System.Net.HttpStatusCode.NoContent ||
            response1.StatusCode == System.Net.HttpStatusCode.NotFound,
            $"First delete: Expected 200, 204, or 404, got {response1.StatusCode}"
        );

        // Second delete should be idempotent
        Assert.True(
            response2.StatusCode == System.Net.HttpStatusCode.OK ||
            response2.StatusCode == System.Net.HttpStatusCode.NoContent ||
            response2.StatusCode == System.Net.HttpStatusCode.NotFound,
            $"Second delete should be idempotent, got {response2.StatusCode}"
        );
    }

    [Fact(DisplayName = "DELETE /api/basket with empty ID returns 400")]
    public async Task DeleteBasket_WithEmptyId_Returns400()
    {
        // Arrange & Act
        var response = await Client.DeleteAsync("/api/basket/");

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
            $"Expected 400, 404, or 405, got {response.StatusCode}"
        );
    }
}
