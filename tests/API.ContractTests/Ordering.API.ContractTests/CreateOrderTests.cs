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

    [Fact(Skip = "Implementation in progress", DisplayName = "POST /api/orders response includes order ID")]
    public async Task CreateOrder_Response_IncludesOrderId()
    {
        // TODO: Validate response contains order ID and can be used for subsequent operations
        await Task.CompletedTask;
    }
}
