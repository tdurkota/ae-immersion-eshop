namespace eShop.Sellers.UnitTests.Application.IntegrationEvents;

using eShop.Sellers.API.Application.IntegrationEvents.Events;
using eShop.Sellers.API.Application.IntegrationEvents.EventHandling;
using eShop.Sellers.API.Infrastructure.Repositories;

[TestClass]
public class OrderCreatedIntegrationEventHandlerTests
{
    private ISellerPayoutRepository _mockRepository;
    private OrderCreatedIntegrationEventHandler _handler;
    private ILogger<OrderCreatedIntegrationEventHandler> _mockLogger;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = Substitute.For<ISellerPayoutRepository>();
        _mockLogger = Substitute.For<ILogger<OrderCreatedIntegrationEventHandler>>();
        _handler = new OrderCreatedIntegrationEventHandler(_mockRepository, _mockLogger);
    }

    [TestMethod]
    public async Task Handle_WithSingleSellerOrder_ShouldCreateOnePayoutEntry()
    {
        // Arrange
        var lineItems = new List<OrderCreatedLineItem>
        {
            new OrderCreatedLineItem(
                orderLineItemId: 1,
                sellerId: 1,
                productId: 100,
                productName: "Product 1",
                unitPrice: 100m,
                units: 1,
                discount: 0m,
                grossAmount: 100m,
                commissionRate: 0.15m,
                commissionAmount: 15m,
                sellerAmount: 85m
            )
        };

        var @event = new OrderCreatedIntegrationEvent(
            orderId: 1,
            buyerName: "John Doe",
            buyerIdentityGuid: "buyer-123",
            orderLineItems: lineItems
        );

        _mockRepository.CreatePayoutAsync(Arg.Any<SellerPayout>()).Returns(new SellerPayout());

        // Act
        await _handler.Handle(@event);

        // Assert
        await _mockRepository.Received(1).CreatePayoutAsync(Arg.Any<SellerPayout>());
    }

    [TestMethod]
    public async Task Handle_WithMultiSellerOrder_ShouldCreatePayoutForEachSeller()
    {
        // Arrange
        var lineItems = new List<OrderCreatedLineItem>
        {
            new OrderCreatedLineItem(
                orderLineItemId: 1,
                sellerId: 1,
                productId: 100,
                productName: "Product 1",
                unitPrice: 100m,
                units: 1,
                discount: 0m,
                grossAmount: 100m,
                commissionRate: 0.15m,
                commissionAmount: 15m,
                sellerAmount: 85m
            ),
            new OrderCreatedLineItem(
                orderLineItemId: 2,
                sellerId: 2,
                productId: 200,
                productName: "Product 2",
                unitPrice: 50m,
                units: 1,
                discount: 0m,
                grossAmount: 50m,
                commissionRate: 0.20m,
                commissionAmount: 10m,
                sellerAmount: 40m
            ),
            new OrderCreatedLineItem(
                orderLineItemId: 3,
                sellerId: 3,
                productId: 300,
                productName: "Product 3",
                unitPrice: 75m,
                units: 1,
                discount: 0m,
                grossAmount: 75m,
                commissionRate: 0.15m,
                commissionAmount: 11.25m,
                sellerAmount: 63.75m
            )
        };

        var @event = new OrderCreatedIntegrationEvent(
            orderId: 1,
            buyerName: "John Doe",
            buyerIdentityGuid: "buyer-123",
            orderLineItems: lineItems
        );

        _mockRepository.CreatePayoutAsync(Arg.Any<SellerPayout>()).Returns(new SellerPayout());

        // Act
        await _handler.Handle(@event);

        // Assert
        await _mockRepository.Received(3).CreatePayoutAsync(Arg.Any<SellerPayout>());
    }

    [TestMethod]
    public async Task Handle_WithMultipleItemsFromSameSeller_ShouldCreatePayoutPerLineItem()
    {
        // Arrange - Same seller with 2 line items
        var lineItems = new List<OrderCreatedLineItem>
        {
            new OrderCreatedLineItem(
                orderLineItemId: 1,
                sellerId: 1,
                productId: 100,
                productName: "Product 1",
                unitPrice: 100m,
                units: 1,
                discount: 0m,
                grossAmount: 100m,
                commissionRate: 0.15m,
                commissionAmount: 15m,
                sellerAmount: 85m
            ),
            new OrderCreatedLineItem(
                orderLineItemId: 2,
                sellerId: 1, // Same seller
                productId: 101,
                productName: "Product 1B",
                unitPrice: 50m,
                units: 2,
                discount: 0m,
                grossAmount: 100m,
                commissionRate: 0.15m,
                commissionAmount: 15m,
                sellerAmount: 85m
            )
        };

        var @event = new OrderCreatedIntegrationEvent(
            orderId: 1,
            buyerName: "John Doe",
            buyerIdentityGuid: "buyer-123",
            orderLineItems: lineItems
        );

        _mockRepository.CreatePayoutAsync(Arg.Any<SellerPayout>()).Returns(new SellerPayout());

        // Act
        await _handler.Handle(@event);

        // Assert - Should create 2 payouts even though same seller
        await _mockRepository.Received(2).CreatePayoutAsync(Arg.Any<SellerPayout>());
    }

    [TestMethod]
    public async Task Handle_WithDifferentCommissionRates_ShouldCalculateCorrectAmounts()
    {
        // Arrange - Test commission calculations with different rates
        var lineItems = new List<OrderCreatedLineItem>
        {
            new OrderCreatedLineItem(
                orderLineItemId: 1,
                sellerId: 1,
                productId: 100,
                productName: "Product 1",
                unitPrice: 100m,
                units: 1,
                discount: 0m,
                grossAmount: 100m,
                commissionRate: 0.10m, // 10%
                commissionAmount: 10m,
                sellerAmount: 90m
            ),
            new OrderCreatedLineItem(
                orderLineItemId: 2,
                sellerId: 2,
                productId: 200,
                productName: "Product 2",
                unitPrice: 100m,
                units: 1,
                discount: 0m,
                grossAmount: 100m,
                commissionRate: 0.20m, // 20%
                commissionAmount: 20m,
                sellerAmount: 80m
            )
        };

        var @event = new OrderCreatedIntegrationEvent(
            orderId: 1,
            buyerName: "John Doe",
            buyerIdentityGuid: "buyer-123",
            orderLineItems: lineItems
        );

        SellerPayout? capturedPayout1 = null;
        SellerPayout? capturedPayout2 = null;

        _mockRepository.CreatePayoutAsync(Arg.Any<SellerPayout>())
            .Returns(x => { 
                var payout = x.Arg<SellerPayout>();
                if (capturedPayout1 == null) capturedPayout1 = payout;
                else capturedPayout2 = payout;
                return payout;
            });

        // Act
        await _handler.Handle(@event);

        // Assert
        Assert.IsNotNull(capturedPayout1);
        Assert.IsNotNull(capturedPayout2);
        
        Assert.AreEqual(100m, capturedPayout1.GrossAmount);
        Assert.AreEqual(10m, capturedPayout1.CommissionAmount);
        Assert.AreEqual(90m, capturedPayout1.SellerAmount);
        
        Assert.AreEqual(100m, capturedPayout2.GrossAmount);
        Assert.AreEqual(20m, capturedPayout2.CommissionAmount);
        Assert.AreEqual(80m, capturedPayout2.SellerAmount);
    }
}
