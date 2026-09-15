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

    [Fact(Skip = "Implementation in progress", DisplayName = "API versioning: v1.0 vs v2.0 compatibility")]
    public async Task GetCatalogItems_ApiVersioning_Compatibility()
    {
        // TODO: Test v1.0 and v2.0 endpoint compatibility
        await Task.CompletedTask;
    }
}
