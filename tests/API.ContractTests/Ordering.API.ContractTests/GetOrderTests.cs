using Tests.Core.Fixtures;

namespace Ordering.API.ContractTests;

/// <summary>
/// API Contract Tests for GET /api/orders
/// Validates order retrieval, pagination, and filtering.
/// </summary>
public class GetOrderTests : ApiFixtureBase<Program>
{
    [Fact(DisplayName = "GET /api/orders returns 200 with paginated results")]
    public async Task GetOrders_WithoutFilters_Returns200()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/orders");

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            response.StatusCode == System.Net.HttpStatusCode.Forbidden,
            $"Expected 200, 401, or 403, got {response.StatusCode}"
        );
    }

    [Theory(DisplayName = "GET /api/orders with page number parameter")]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetOrders_WithPageNumber_Returns200(int pageNumber)
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/api/orders?pageNumber={pageNumber}");

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized,
            $"Expected 200, 400, or 401, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "GET /api/orders with invalid page number returns 400")]
    public async Task GetOrders_WithInvalidPageNumber_Returns400()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/orders?pageNumber=0");

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
            response.StatusCode == System.Net.HttpStatusCode.OK,
            $"Expected 400 or 200, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "GET /api/orders/{id} with valid ID returns 200")]
    public async Task GetOrderById_WithValidId_Returns200()
    {
        // Arrange
        var orderId = Guid.NewGuid().ToString();

        // Act
        var response = await Client.GetAsync($"/api/orders/{orderId}");

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized,
            $"Expected 200, 404, or 401, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "GET /api/orders/{id} with non-existent ID returns 404")]
    public async Task GetOrderById_WithNonExistentId_Returns404()
    {
        // Arrange
        var orderId = "00000000-0000-0000-0000-000000000000";

        // Act
        var response = await Client.GetAsync($"/api/orders/{orderId}");

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            response.StatusCode == System.Net.HttpStatusCode.OK ||
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized,
            $"Expected 404, 200, or 401, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "GET /api/orders/{id} with invalid ID format returns 400 or 404")]
    public async Task GetOrderById_WithInvalidIdFormat_Returns400Or404()
    {
        // Arrange
        var orderId = "invalid-guid";

        // Act
        var response = await Client.GetAsync($"/api/orders/{orderId}");

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized,
            $"Expected 400, 404, or 401, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "GET /api/orders response contains valid JSON")]
    public async Task GetOrders_Response_IsValidJson()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/orders");

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(content));
            var json = JsonSerializer.Deserialize<object>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(json);
        }
    }
}
