namespace eShop.Sellers.API.Model;

public class Seller
{
    public Guid SellerId { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    public string? Description { get; set; }

    public string? PhoneNumber { get; set; }

    public string? BankAccountInfo { get; set; }

    public decimal CommissionRate { get; set; }

    public SellerStatus Status { get; set; } = SellerStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SellerPayout> Payouts { get; set; } = [];
}
