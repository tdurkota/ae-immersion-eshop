namespace eShop.Sellers.API.Responses;

public record SellerPayoutResponse(
    Guid PayoutId,
    int OrderId,
    int OrderLineItemId,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal SellerAmount,
    string Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);
