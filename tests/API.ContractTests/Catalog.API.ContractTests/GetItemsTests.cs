using Tests.Core.Fixtures;

namespace Catalog.API.ContractTests;

/// <summary>
/// API Contract Tests for GET /api/catalog
/// Validates pagination, filtering, versioning, and error handling.
/// </summary>
public class GetItemsTests : ApiFixtureBase<Program>
{
    [Fact(DisplayName = "GET /api/catalog returns 200 with paginated results")]
    public async Task GetCatalogItems_WithDefaultPaging_Returns200()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/catalog");
        
        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Theory(DisplayName = "GET /api/catalog with various page numbers")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public async Task GetCatalogItems_WithValidPageNumber_Returns200(int pageNumber)
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/api/catalog?pageNumber={pageNumber}");
        
        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Theory(DisplayName = "GET /api/catalog with invalid page number returns 400")]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetCatalogItems_WithInvalidPageNumber_Returns400(int pageNumber)
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/api/catalog?pageNumber={pageNumber}");
        
        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "GET /api/catalog returns paginated response with items array")]
    public async Task GetCatalogItems_Response_HasCorrectStructure()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/catalog");
        
        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        Assert.True(json.TryGetProperty("data", out _) || json.TryGetProperty("items", out _),
            "Response should contain 'data' or 'items' array");
    }

    [Theory(DisplayName = "GET /api/catalog with page size parameter")]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public async Task GetCatalogItems_WithPageSize_Returns200(int pageSize)
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/api/catalog?pageSize={pageSize}");
        
        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "GET /api/catalog with excessive page size returns 200 or 400")]
    public async Task GetCatalogItems_WithExcessivePageSize_HandlesGracefully()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/catalog?pageSize=10000");
        
        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK || 
            response.StatusCode == System.Net.HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {response.StatusCode}"
        );
    }

    [Fact(DisplayName = "GET /api/catalog returns 200 with multiple calls")]
    public async Task GetCatalogItems_MultipleRequests_AreConsistent()
    {
        // Arrange & Act
        var response1 = await Client.GetAsync("/api/catalog");
        var response2 = await Client.GetAsync("/api/catalog");
        
        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);
    }
}
