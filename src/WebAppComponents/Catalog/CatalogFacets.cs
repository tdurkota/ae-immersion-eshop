namespace eShop.WebAppComponents.Catalog;

public record CatalogFacetCount(int Id, int Count);

public record SellerFacet(Guid Id, string Name, int Count);

public record CatalogFacets(
    IReadOnlyList<CatalogFacetCount> BrandCounts,
    IReadOnlyList<CatalogFacetCount> TypeCounts,
    IReadOnlyList<SellerFacet> SellerCounts,
    int BrandTotal,
    int TypeTotal);
