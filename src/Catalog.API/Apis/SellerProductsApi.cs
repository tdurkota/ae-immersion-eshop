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

    public static async Task<Ok<PaginatedItems<CatalogItem>>> GetSellerProducts(
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
            .OrderBy(c => c.Name)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        return TypedResults.Ok(new PaginatedItems<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage));
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
