namespace eShop.Sellers.API.Responses;

public record SellerOrderResponse(
    int OrderId,
    DateTime OrderDate,
    string Status,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal SellerAmount,
    List<OrderItemDetail> Items);

public record OrderItemDetail(
    int OrderLineItemId,
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Units,
    decimal GrossTotal,
    decimal CommissionAmount,
    decimal SellerAmount);
