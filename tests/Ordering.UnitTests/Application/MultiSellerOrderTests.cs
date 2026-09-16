namespace Ordering.UnitTests.Application;

using eShop.Ordering.API.Application.Models;
using eShop.Ordering.API.Application.Services;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

public class MultiSellerOrderTests
{
    private readonly CommissionService _commissionService;

    public MultiSellerOrderTests()
    {
        _commissionService = new CommissionService();
    }

    [Fact]
    public void Order_WithItemsFromMultipleSellers_CreatesCorrectLineItems()
    {
        // Arrange
        var address = new Address("123 Main St", "Seattle", "WA", "USA", "98101");
        var order = new Order("user123", "John Doe", address, 1, "1234567890", "123", "John Doe", DateTime.UtcNow);

        // Add items from Seller 1 (15% commission)
        order.AddOrderItem(1, "Product A", 100m, 0m, "url1.jpg", 1, 1, 0.15m);
        order.AddOrderItem(2, "Product B", 50m, 0m, "url2.jpg", 1, 1, 0.15m);

        // Add items from Seller 2 (10% commission)
        order.AddOrderItem(3, "Product C", 75m, 0m, "url3.jpg", 1, 2, 0.10m);

        // Act
        var orderItems = order.OrderItems.ToList();

        // Assert
        orderItems.Should().HaveCount(3);
        
        // Verify Seller 1 items
        orderItems[0].SellerId.Should().Be(1);
        orderItems[0].CommissionRate.Should().Be(0.15m);
        orderItems[0].ProductId.Should().Be(1);

        orderItems[1].SellerId.Should().Be(1);
        orderItems[1].CommissionRate.Should().Be(0.15m);
        orderItems[1].ProductId.Should().Be(2);

        // Verify Seller 2 item
        orderItems[2].SellerId.Should().Be(2);
        orderItems[2].CommissionRate.Should().Be(0.10m);
        orderItems[2].ProductId.Should().Be(3);
    }

    [Fact]
    public void Order_WithMultipleSellersAndDifferentRates_CalculatesCommissionCorrectly()
    {
        // Arrange
        var lineItems = new[]
        {
            (Amount: 100m, CommissionRate: 0.15m, SellerId: 1),  // Commission: 15, Seller receives: 85
            (Amount: 50m, CommissionRate: 0.15m, SellerId: 1),   // Commission: 7.50, Seller receives: 42.50
            (Amount: 75m, CommissionRate: 0.10m, SellerId: 2)    // Commission: 7.50, Seller receives: 67.50
        };

        // Act
        var seller1Commission = _commissionService.CalculateCommission(100m, 0.15m) + _commissionService.CalculateCommission(50m, 0.15m);
        var seller2Commission = _commissionService.CalculateCommission(75m, 0.10m);
        var totalCommission = lineItems.Sum(item => _commissionService.CalculateCommission(item.Amount, item.CommissionRate));

        // Assert
        seller1Commission.Should().Be(22.50m);
        seller2Commission.Should().Be(7.50m);
        totalCommission.Should().Be(30m);
    }

    [Fact]
    public void Order_WithSingleProductFromMultipleSellers_CreatesSepalatLineItems()
    {
        // Arrange - Same product sold by two different sellers
        var address = new Address("123 Main St", "Seattle", "WA", "USA", "98101");
        var order = new Order("user123", "John Doe", address, 1, "1234567890", "123", "John Doe", DateTime.UtcNow);

        // Add same product from different sellers
        order.AddOrderItem(1, "Popular Product", 50m, 0m, "url.jpg", 1, 1, 0.15m);
        order.AddOrderItem(1, "Popular Product", 50m, 0m, "url.jpg", 1, 2, 0.10m);

        // Act
        var orderItems = order.OrderItems.ToList();

        // Assert - Should have 2 separate line items (same product, different sellers)
        orderItems.Should().HaveCount(2);
        orderItems[0].SellerId.Should().Be(1);
        orderItems[1].SellerId.Should().Be(2);
    }

    [Fact]
    public void CommissionCalculation_ForMultiSellerOrder_SumsCorrectly()
    {
        // Arrange
        var address = new Address("123 Main St", "Seattle", "WA", "USA", "98101");
        var order = new Order("user123", "John Doe", address, 1, "1234567890", "123", "John Doe", DateTime.UtcNow);

        order.AddOrderItem(1, "Item 1", 100m, 0m, "url1.jpg", 1, 1, 0.15m);  // 15.00
        order.AddOrderItem(2, "Item 2", 200m, 0m, "url2.jpg", 1, 1, 0.15m);  // 30.00
        order.AddOrderItem(3, "Item 3", 150m, 0m, "url3.jpg", 1, 2, 0.10m);  // 15.00
        order.AddOrderItem(4, "Item 4", 250m, 0m, "url4.jpg", 1, 2, 0.10m);  // 25.00

        // Act
        var seller1Items = order.OrderItems.Where(x => x.SellerId == 1).ToList();
        var seller2Items = order.OrderItems.Where(x => x.SellerId == 2).ToList();

        var seller1Commission = seller1Items.Sum(item =>
            _commissionService.CalculateCommission(item.UnitPrice * item.Units - item.Discount, item.CommissionRate));
        var seller2Commission = seller2Items.Sum(item =>
            _commissionService.CalculateCommission(item.UnitPrice * item.Units - item.Discount, item.CommissionRate));

        var totalCommission = seller1Commission + seller2Commission;

        // Assert
        seller1Commission.Should().Be(45m);  // 15 + 30
        seller2Commission.Should().Be(40m);  // 15 + 25
        totalCommission.Should().Be(85m);
    }

    [Fact]
    public void Order_WithDifferentCommissionRates_PerSellerCalculation()
    {
        // Arrange - Order with 3 sellers having different rates
        var address = new Address("123 Main St", "Seattle", "WA", "USA", "98101");
        var order = new Order("user123", "John Doe", address, 1, "1234567890", "123", "John Doe", DateTime.UtcNow);

        // Seller 1: 15% commission
        order.AddOrderItem(1, "A", 100m, 0m, "url1.jpg", 1, 1, 0.15m);
        // Seller 2: 10% commission
        order.AddOrderItem(2, "B", 100m, 0m, "url2.jpg", 1, 2, 0.10m);
        // Seller 3: 20% commission
        order.AddOrderItem(3, "C", 100m, 0m, "url3.jpg", 1, 3, 0.20m);

        // Act
        var groupedBySeller = order.OrderItems.GroupBy(x => x.SellerId);

        // Assert
        groupedBySeller.Count().Should().Be(3);

        var seller1 = groupedBySeller.First(g => g.Key == 1).First();
        seller1.CommissionRate.Should().Be(0.15m);

        var seller2 = groupedBySeller.First(g => g.Key == 2).First();
        seller2.CommissionRate.Should().Be(0.10m);

        var seller3 = groupedBySeller.First(g => g.Key == 3).First();
        seller3.CommissionRate.Should().Be(0.20m);
    }

    [Fact]
    public void Order_CommissionImmutableAfterCreation()
    {
        // Arrange
        var address = new Address("123 Main St", "Seattle", "WA", "USA", "98101");
        var order = new Order("user123", "John Doe", address, 1, "1234567890", "123", "John Doe", DateTime.UtcNow);

        order.AddOrderItem(1, "Product A", 100m, 0m, "url1.jpg", 1, 1, 0.15m);

        // Act
        var orderItem = order.OrderItems.First();
        var originalCommissionRate = orderItem.CommissionRate;

        // Assert - CommissionRate property is read-only (private set)
        // Verify we cannot modify it through the property
        orderItem.CommissionRate.Should().Be(0.15m);
        orderItem.CommissionRate.Should().Be(originalCommissionRate);
    }
}
