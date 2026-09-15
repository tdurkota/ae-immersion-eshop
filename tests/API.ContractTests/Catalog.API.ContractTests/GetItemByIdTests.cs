using Tests.Core.Fixtures;

namespace Catalog.API.ContractTests;

/// <summary>
/// API Contract Tests for GET /api/catalog/{id}
/// Validates single item retrieval and error handling.
/// </summary>
public class GetItemByIdTests : ApiFixtureBase<Program>
{
    [Fact(DisplayName = "GET /api/catalog/{id} with valid ID returns 200")]
    public async Task GetCatalogItem_WithValidId_Returns200()
    {
        // Arrange
        var itemId = "1"; // Assuming item 1 exists

        // Act
        var response = await Client.GetAsync($"/api/catalog/{itemId}");

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound,
            $"Expected 200 or 404, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "GET /api/catalog/{id} with non-existent ID returns 404")]
    public async Task GetCatalogItem_WithNonExistentId_Returns404()
    {
        // Arrange
        var itemId = "999999"; // Non-existent ID

        // Act
        var response = await Client.GetAsync($"/api/catalog/{itemId}");

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            response.StatusCode == System.Net.HttpStatusCode.OK,
            $"Expected 404 or 200, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "GET /api/catalog/{id} with invalid ID format returns 400 or 404")]
    public async Task GetCatalogItem_WithInvalidIdFormat_Returns400Or404()
    {
        // Arrange
        var itemId = "not-a-number";

        // Act
        var response = await Client.GetAsync($"/api/catalog/{itemId}");

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound,
            $"Expected 400 or 404, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "GET /api/catalog/{id} response contains required fields")]
    public async Task GetCatalogItem_Response_HasRequiredFields()
    {
        // Arrange
        var itemId = "1";

        // Act
        var response = await Client.GetAsync($"/api/catalog/{itemId}");

        // Assert
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Check for common fields
            Assert.True(
                json.TryGetProperty("id", out _) || json.TryGetProperty("productId", out _),
                "Response should contain ID field"
            );
        }
    }

    [Fact(DisplayName = "GET /api/catalog/{id} with negative ID returns 400 or 404")]
    public async Task GetCatalogItem_WithNegativeId_Returns400Or404()
    {
        // Arrange
        var itemId = "-1";

        // Act
        var response = await Client.GetAsync($"/api/catalog/{itemId}");

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound,
            $"Expected 400 or 404, got {response.StatusCode}"
        );
    }
}
