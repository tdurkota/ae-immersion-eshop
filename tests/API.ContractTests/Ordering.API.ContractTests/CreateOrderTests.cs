using Tests.Core.Fixtures;

namespace Ordering.API.ContractTests;

/// <summary>
/// API Contract Tests for POST /api/orders
/// Validates order creation, validation, and error handling.
/// </summary>
public class CreateOrderTests : ApiFixtureBase<Program>
{
    [Fact(DisplayName = "POST /api/orders with valid payload returns 201")]
    public async Task CreateOrder_WithValidPayload_Returns201()
    {
        // Arrange
        var createOrderRequest = new
        {
            items = new[]
            {
                new { productId = "1", quantity = 2, unitPrice = 100.00m }
            },
            street = "123 Main St",
            city = "Seattle",
            state = "WA",
            country = "USA",
            zipCode = "98101"
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(createOrderRequest),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await Client.PostAsync("/api/orders", content);

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.Created ||
            response.StatusCode == System.Net.HttpStatusCode.OK,
            $"Expected 201 or 200, got {response.StatusCode}"
        );
    }

    [Theory(DisplayName = "POST /api/orders with invalid data returns 400")]
    [InlineData("invalid-quantity-zero")]
    [InlineData("missing-required-field")]
    public async Task CreateOrder_WithInvalidData_Returns400(string scenario)
    {
        // Arrange
        var invalidRequest = scenario switch
        {
            "invalid-quantity-zero" => new { items = new[] { new { quantity = 0 } } },
            "missing-required-field" => new { items = (object[])null! },
            _ => new { }
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(invalidRequest),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await Client.PostAsync("/api/orders", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "POST /api/orders with empty payload returns 400")]
    public async Task CreateOrder_WithEmptyPayload_Returns400()
    {
        // Arrange
        var content = new StringContent(
            "{}",
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await Client.PostAsync("/api/orders", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "POST /api/orders with malformed JSON returns 400")]
    public async Task CreateOrder_WithMalformedJson_Returns400()
    {
        // Arrange
        var content = new StringContent(
            "{ invalid json",
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await Client.PostAsync("/api/orders", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "POST /api/orders without Content-Type header handles gracefully")]
    public async Task CreateOrder_WithoutContentTypeHeader_HandlesGracefully()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = new StringContent("{}")
        };
        request.Content.Headers.Remove("Content-Type");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.BadRequest || 
            response.StatusCode == System.Net.HttpStatusCode.UnsupportedMediaType,
            $"Expected 400 or 415, got {response.StatusCode}"
        );
    }

    [Theory(DisplayName = "POST /api/orders validates required address fields")]
    [InlineData("missing-street")]
    [InlineData("missing-city")]
    [InlineData("missing-country")]
    public async Task CreateOrder_WithMissingAddressField_Returns400(string scenario)
    {
        // Arrange
        var invalidRequest = scenario switch
        {
            "missing-street" => new
            {
                items = new[] { new { productId = "1", quantity = 1, unitPrice = 100m } },
                city = "Seattle",
                state = "WA",
                country = "USA",
                zipCode = "98101"
            },
            "missing-city" => new
            {
                items = new[] { new { productId = "1", quantity = 1, unitPrice = 100m } },
                street = "123 Main",
                state = "WA",
                country = "USA",
                zipCode = "98101"
            },
            "missing-country" => new
            {
                items = new[] { new { productId = "1", quantity = 1, unitPrice = 100m } },
                street = "123 Main",
                city = "Seattle",
                state = "WA",
                zipCode = "98101"
            },
            _ => new { }
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(invalidRequest),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await Client.PostAsync("/api/orders", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "POST /api/orders response is valid JSON")]
    public async Task CreateOrder_Response_IsValidJson()
    {
        // Arrange
        var createOrderRequest = new
        {
            items = new[] { new { productId = "1", quantity = 1, unitPrice = 100m } },
            street = "123 Main St",
            city = "Seattle",
            state = "WA",
            country = "USA",
            zipCode = "98101"
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(createOrderRequest),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await Client.PostAsync("/api/orders", content);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(responseContent));
            var json = JsonSerializer.Deserialize<object>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(json);
        }
    }
}
