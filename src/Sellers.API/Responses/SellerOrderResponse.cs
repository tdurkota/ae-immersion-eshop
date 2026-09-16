namespace eShop.Sellers.API.Responses;

public record SellerOrderResponse(
    int OrderId,
    string Status,
    DateTime OrderDate,
    decimal TotalAmount,
    List<OrderItemDetail> Items
);

public record OrderItemDetail(
    int OrderLineItemId,
    decimal UnitPrice,
    int Quantity,
    decimal ItemTotal,
    decimal SellerAmount,
    string PayoutStatus
);
