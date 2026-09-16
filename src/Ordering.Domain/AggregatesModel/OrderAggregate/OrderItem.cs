using System.ComponentModel.DataAnnotations;

namespace eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

public class OrderItem
    : Entity
{
    [Required]
    public string ProductName { get; private set; }
    
    public string PictureUrl { get; private set;}
    
    public decimal UnitPrice { get; private set;}
    
    public decimal Discount { get; private set; }
    
    public int Units { get; private set; }

    public int ProductId { get; private set; }

    public int SellerId { get; private set; }

    public decimal CommissionRate { get; private set; }

    protected OrderItem() { }

    public OrderItem(int productId, string productName, decimal unitPrice, decimal discount, string pictureUrl, int units = 1, int sellerId = 0, decimal commissionRate = 0)
    {
        if (units <= 0)
        {
            throw new OrderingDomainException("Invalid number of units");
        }

        if ((unitPrice * units) < discount)
        {
            throw new OrderingDomainException("The total of order item is lower than applied discount");
        }

        if (sellerId <= 0)
        {
            throw new OrderingDomainException("Invalid seller ID");
        }

        if (commissionRate < 0 || commissionRate > 1)
        {
            throw new OrderingDomainException("Commission rate must be between 0 and 1");
        }

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Discount = discount;
        Units = units;
        PictureUrl = pictureUrl;
        SellerId = sellerId;
        CommissionRate = commissionRate;
    }
    
    public void SetNewDiscount(decimal discount)
    {
        if (discount < 0)
        {
            throw new OrderingDomainException("Discount is not valid");
        }

        Discount = discount;
    }

    public void AddUnits(int units)
    {
        if (units < 0)
        {
            throw new OrderingDomainException("Invalid units");
        }

        Units += units;
    }
}
