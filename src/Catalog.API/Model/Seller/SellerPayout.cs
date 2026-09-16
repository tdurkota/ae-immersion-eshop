using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.API.Model.Seller;

public class SellerPayout
{
    [Key]
    public Guid PayoutId { get; set; }

    [Required]
    public Guid SellerId { get; set; }

    [Required]
    public int OrderId { get; set; }

    [Required]
    public int OrderLineItemId { get; set; }

    [Required]
    public decimal GrossAmount { get; set; }

    [Required]
    public decimal CommissionAmount { get; set; }

    [Required]
    public decimal SellerAmount { get; set; }

    [Required]
    public SellerPayoutStatus Status { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public SellerPayout()
    {
        PayoutId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Status = SellerPayoutStatus.Pending;
    }

    public SellerPayout(Guid sellerId, int orderId, int orderLineItemId, decimal grossAmount, decimal commissionAmount, decimal sellerAmount)
        : this()
    {
        SellerId = sellerId;
        OrderId = orderId;
        OrderLineItemId = orderLineItemId;
        GrossAmount = grossAmount;
        CommissionAmount = commissionAmount;
        SellerAmount = sellerAmount;
    }
}
