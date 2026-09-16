namespace eShop.Sellers.API.Responses;

public record SellerPayoutSummaryResponse(
    decimal TotalEarned,
    decimal TotalPending,
    decimal TotalProcessed,
    decimal TotalPaid);
