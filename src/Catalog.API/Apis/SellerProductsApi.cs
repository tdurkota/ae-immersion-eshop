using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eShop.Catalog.API;

public static class SellerProductsApi
{
    public static IEndpointRouteBuilder MapSellerProductsApi(this IEndpointRouteBuilder app)
    {
        var api = app.NewVersionedApi("SellerProducts")
            .MapGroup("api/sellers/{sellerId:guid}/products")
            .HasApiVersion(1, 0);

        api.MapPost("", CreateSellerProduct)
            .WithName("CreateSellerProduct")
            .WithSummary("Seller adds a product")
            .WithDescription("Add a new product to the seller's catalog")
            .WithTags("SellerProducts")
            .RequireAuthorization();

        api.MapPut("{productId:int}", UpdateSellerProduct)
            .WithName("UpdateSellerProduct")
            .WithSummary("Seller updates their product")
            .WithDescription("Update a product in the seller's catalog")
            .WithTags("SellerProducts")
            .RequireAuthorization();

        api.MapDelete("{productId:int}", DeleteSellerProduct)
            .WithName("DeleteSellerProduct")
            .WithSummary("Seller deletes their product")
            .WithDescription("Delete a product from the seller's catalog")
            .WithTags("SellerProducts")
            .RequireAuthorization();

        api.MapGet("", GetSellerProducts)
            .WithName("GetSellerProducts")
            .WithSummary("Seller views their products")
            .WithDescription("Get a paginated list of products for a seller")
            .WithTags("SellerProducts")
            .RequireAuthorization();

        // Public storefront endpoint
        var storefront = app.NewVersionedApi("SellerStorefront")
            .MapGroup("api/sellers/{sellerId:guid}/storefront/products")
            .HasApiVersion(1, 0);

        storefront.MapGet("", GetSellerStorefront)
            .WithName("GetSellerStorefront")
            .WithSummary("Browse seller's storefront")
            .WithDescription("Get a paginated list of products from a seller's storefront, with optional sorting")
            .WithTags("SellerStorefront");

        return app;
    }

    public static async Task<IResult> CreateSellerProduct(
        [AsParameters] CatalogServices services,
        Guid sellerId,
        CreateSellerProductRequest request,
        HttpContext httpContext)
    {
        // TODO: Verify seller authorization (only the seller can add products for their ID)
        // TODO: Check seller status (not suspended)

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "Product name is required"
            });
        }

        var product = new CatalogItem(request.Name)
        {
            CatalogBrandId = request.CatalogBrandId,
            CatalogTypeId = request.CatalogTypeId,
            Description = request.Description,
            PictureFileName = request.PictureFileName,
            Price = request.Price,
            AvailableStock = request.AvailableStock,
            RestockThreshold = request.RestockThreshold,
            MaxStockThreshold = request.MaxStockThreshold,
            SellerId = sellerId
        };

        product.Embedding = await services.CatalogAI.GetEmbeddingAsync(product);

        services.Context.CatalogItems.Add(product);
        await services.Context.SaveChangesAsync();

        return TypedResults.Created($"/api/sellers/{sellerId}/products/{product.Id}", product);
    }

    public static async Task<IResult> UpdateSellerProduct(
        [AsParameters] CatalogServices services,
        Guid sellerId,
        int productId,
        UpdateSellerProductRequest request,
        HttpContext httpContext)
    {
        // TODO: Verify seller authorization (only the seller can update their products)

        var product = services.Context.CatalogItems
            .FirstOrDefault(x => x.Id == productId && x.SellerId == sellerId);

        if (product is null)
        {
            return TypedResults.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "Product name is required"
            });
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.CatalogBrandId = request.CatalogBrandId;
        product.CatalogTypeId = request.CatalogTypeId;
        product.PictureFileName = request.PictureFileName;
        product.AvailableStock = request.AvailableStock;
        product.RestockThreshold = request.RestockThreshold;
        product.MaxStockThreshold = request.MaxStockThreshold;

        if (!string.IsNullOrEmpty(product.Description))
        {
            product.Embedding = await services.CatalogAI.GetEmbeddingAsync(product);
        }

        services.Context.CatalogItems.Update(product);
        await services.Context.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<IResult> DeleteSellerProduct(
        [AsParameters] CatalogServices services,
        Guid sellerId,
        int productId,
        HttpContext httpContext)
    {
        // TODO: Verify seller authorization (only the seller can delete their products)

        var product = services.Context.CatalogItems
            .FirstOrDefault(x => x.Id == productId && x.SellerId == sellerId);

        if (product is null)
        {
            return TypedResults.NotFound();
        }

        services.Context.CatalogItems.Remove(product);
        await services.Context.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<Ok<PaginatedItems<ProductResponseDto>>> GetSellerProducts(
        [AsParameters] PaginationRequest paginationRequest,
        [AsParameters] CatalogServices services,
        Guid sellerId,
        HttpContext httpContext)
    {
        // TODO: Verify seller authorization (only the seller can view their own products)

        var pageSize = paginationRequest.PageSize;
        var pageIndex = paginationRequest.PageIndex;

        var root = services.Context.CatalogItems.Where(c => c.SellerId == sellerId);

        var totalItems = await root.LongCountAsync();

        var itemsOnPage = await root
            .Include(ci => ci.CatalogBrand)
            .Include(ci => ci.CatalogType)
            .Include(ci => ci.Seller)
            .OrderBy(c => c.Name)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        var dtoItems = itemsOnPage.Select(MapToProductResponseDto).ToList();

        return TypedResults.Ok(new PaginatedItems<ProductResponseDto>(pageIndex, pageSize, totalItems, dtoItems));
    }

    /// <summary>
    /// Public seller storefront endpoint - Browse a seller's products with pagination and sorting.
    /// </summary>
    public static async Task<Results<Ok<PaginatedItems<ProductResponseDto>>, NotFound>> GetSellerStorefront(
        [AsParameters] PaginationRequest paginationRequest,
        [AsParameters] CatalogServices services,
        Guid sellerId,
        [Description("Sort order: name (default), price_asc, price_desc")] string? sortBy = "name")
    {
        var pageSize = paginationRequest.PageSize;
        var pageIndex = paginationRequest.PageIndex;

        // First verify the seller exists
        var sellerExists = await services.Context.Sellers.AnyAsync(s => s.SellerId == sellerId);
        if (!sellerExists)
        {
            return TypedResults.NotFound();
        }

        var root = services.Context.CatalogItems.Where(c => c.SellerId == sellerId);

        var totalItems = await root.LongCountAsync();

        // Apply sorting
        IQueryable<CatalogItem> sortedRoot = sortBy?.ToLower() switch
        {
            "price_asc" => root.OrderBy(c => c.Price),
            "price_desc" => root.OrderByDescending(c => c.Price),
            _ => root.OrderBy(c => c.Name)
        };

        var itemsOnPage = await sortedRoot
            .Include(ci => ci.CatalogBrand)
            .Include(ci => ci.CatalogType)
            .Include(ci => ci.Seller)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        var dtoItems = itemsOnPage.Select(MapToProductResponseDto).ToList();

        return TypedResults.Ok(new PaginatedItems<ProductResponseDto>(pageIndex, pageSize, totalItems, dtoItems));
    }

    /// <summary>
    /// Maps a CatalogItem to a ProductResponseDto with seller attribution.
    /// </summary>
    private static ProductResponseDto MapToProductResponseDto(CatalogItem item)
    {
        var sellerName = item.SellerId.HasValue && item.Seller != null
            ? item.Seller.Name
            : "Official Store";

        var catalogBrandDto = item.CatalogBrand != null
            ? new CatalogBrandDto(item.CatalogBrand.Id, item.CatalogBrand.Brand)
            : null;

        var catalogTypeDto = item.CatalogType != null
            ? new CatalogTypeDto(item.CatalogType.Id, item.CatalogType.Type)
            : null;

        return new ProductResponseDto(
            Id: item.Id,
            Name: item.Name,
            Description: item.Description,
            Price: item.Price,
            PictureFileName: item.PictureFileName,
            CatalogTypeId: item.CatalogTypeId,
            CatalogType: catalogTypeDto,
            CatalogBrandId: item.CatalogBrandId,
            CatalogBrand: catalogBrandDto,
            AvailableStock: item.AvailableStock,
            RestockThreshold: item.RestockThreshold,
            MaxStockThreshold: item.MaxStockThreshold,
            OnReorder: item.OnReorder,
            SellerId: item.SellerId,
            SellerName: sellerName);
    }
}

public record CreateSellerProductRequest(
    [Required] string Name,
    string? Description,
    decimal Price,
    int CatalogTypeId,
    int CatalogBrandId,
    string? PictureFileName,
    int AvailableStock = 0,
    int RestockThreshold = 0,
    int MaxStockThreshold = 0);

public record UpdateSellerProductRequest(
    [Required] string Name,
    string? Description,
    decimal Price,
    int CatalogTypeId,
    int CatalogBrandId,
    string? PictureFileName,
    int AvailableStock = 0,
    int RestockThreshold = 0,
    int MaxStockThreshold = 0);
