namespace eShop.Catalog.API.Model;

/// <summary>
/// Seller domain model for catalog attribution.
/// This model allows Catalog.API to reference seller information from the shared database
/// without creating direct dependencies on Sellers.API.
/// </summary>
public class Seller
{
    public Guid SellerId { get; set; }

    public required string Name { get; set; }

    public ICollection<CatalogItem> Products { get; set; } = [];
}
