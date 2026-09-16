namespace eShop.Sellers.API.Model;

public class SellerPayout
{
    public Guid PayoutId { get; set; }

    public Guid SellerId { get; set; }

    public int OrderId { get; set; }

    public int OrderLineItemId { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal CommissionAmount { get; set; }

    public decimal SellerAmount { get; set; }

    public SellerPayoutStatus Status { get; set; } = SellerPayoutStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public Seller? Seller { get; set; }
}
