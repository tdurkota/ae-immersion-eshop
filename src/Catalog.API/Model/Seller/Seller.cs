using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.API.Model.Seller;

public class Seller
{
    [Key]
    public Guid SellerId { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public required string Email { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    [MaxLength(500)]
    public required string BankAccountInfo { get; set; }

    [Range(0, 1)]
    public decimal CommissionRate { get; set; }

    [Required]
    public SellerStatus Status { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    public Seller()
    {
        SellerId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Status = SellerStatus.Active;
        Name = string.Empty;
        Email = string.Empty;
        BankAccountInfo = string.Empty;
    }

    public Seller(string name, string email, string bankAccountInfo, decimal commissionRate)
        : this()
    {
        Name = name;
        Email = email;
        BankAccountInfo = bankAccountInfo;
        CommissionRate = commissionRate;
    }
}
