namespace eShop.Ordering.API.Application.IntegrationEvents.Events;

/// <summary>
/// Event published when an order is successfully created, containing seller attribution and commission information for each line item.
/// This event is used to trigger payout ledger creation in the Sellers.API microservice.
/// </summary>
public record OrderCreatedIntegrationEvent : IntegrationEvent
{
    public int OrderId { get; }
    public string BuyerName { get; }
    public string BuyerIdentityGuid { get; }
    public IEnumerable<OrderCreatedLineItem> OrderLineItems { get; }

    public OrderCreatedIntegrationEvent(
        int orderId, string buyerName, string buyerIdentityGuid,
        IEnumerable<OrderCreatedLineItem> orderLineItems)
    {
        OrderId = orderId;
        BuyerName = buyerName;
        BuyerIdentityGuid = buyerIdentityGuid;
        OrderLineItems = orderLineItems ?? throw new ArgumentNullException(nameof(orderLineItems));
    }
}

/// <summary>
/// Represents a line item in an order with seller attribution and commission information.
/// This data is captured at order creation time and is immutable for historical accuracy.
/// </summary>
public record OrderCreatedLineItem
{
    /// <summary>Gets the order line item ID.</summary>
    public int OrderLineItemId { get; }

    /// <summary>Gets the seller ID who provided this item.</summary>
    public int SellerId { get; }

    /// <summary>Gets the product ID.</summary>
    public int ProductId { get; }

    /// <summary>Gets the product name.</summary>
    public string ProductName { get; }

    /// <summary>Gets the unit price of the product.</summary>
    public decimal UnitPrice { get; }

    /// <summary>Gets the number of units ordered.</summary>
    public int Units { get; }

    /// <summary>Gets the discount applied to this line item.</summary>
    public decimal Discount { get; }

    /// <summary>Gets the gross amount (UnitPrice * Units - Discount).</summary>
    public decimal GrossAmount { get; }

    /// <summary>Gets the commission rate as a decimal (e.g., 0.15 for 15%).</summary>
    public decimal CommissionRate { get; }

    /// <summary>Gets the calculated commission amount.</summary>
    public decimal CommissionAmount { get; }

    /// <summary>Gets the seller's amount after commission (GrossAmount - CommissionAmount).</summary>
    public decimal SellerAmount { get; }

    public OrderCreatedLineItem(
        int orderLineItemId, int sellerId, int productId, string productName,
        decimal unitPrice, int units, decimal discount, decimal grossAmount,
        decimal commissionRate, decimal commissionAmount, decimal sellerAmount)
    {
        OrderLineItemId = orderLineItemId;
        SellerId = sellerId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Units = units;
        Discount = discount;
        GrossAmount = grossAmount;
        CommissionRate = commissionRate;
        CommissionAmount = commissionAmount;
        SellerAmount = sellerAmount;
    }
}
