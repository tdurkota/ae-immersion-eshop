namespace eShop.Sellers.API.Responses;

public record SellerResponse(
    Guid SellerId,
    string Name,
    string? Description,
    string? PhoneNumber,
    decimal CommissionRate,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    // Private fields only visible to owner
    string? Email = null,
    string? BankAccountInfo = null);
