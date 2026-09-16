namespace eShop.Catalog.API.DTOs;

/// <summary>
/// Product response DTO with seller attribution information.
/// Includes seller_id and seller_name for each product.
/// </summary>
public record ProductResponseDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    string? PictureFileName,
    int CatalogTypeId,
    CatalogTypeDto? CatalogType,
    int CatalogBrandId,
    CatalogBrandDto? CatalogBrand,
    int AvailableStock,
    int RestockThreshold,
    int MaxStockThreshold,
    bool OnReorder,
    Guid? SellerId,
    string SellerName);

/// <summary>
/// Catalog brand DTO.
/// </summary>
public record CatalogBrandDto(int Id, string Brand);

/// <summary>
/// Catalog type DTO.
/// </summary>
public record CatalogTypeDto(int Id, string Type);
