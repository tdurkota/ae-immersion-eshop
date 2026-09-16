namespace eShop.Sellers.API.Application.IntegrationEvents.EventHandling;

using eShop.EventBus.Abstractions;
using eShop.Sellers.API.Application.IntegrationEvents.Events;
using eShop.Sellers.API.Infrastructure.Repositories;
using eShop.Sellers.API.Model;

public class OrderCreatedIntegrationEventHandler(
    ISellerPayoutRepository payoutRepository,
    ILogger<OrderCreatedIntegrationEventHandler> logger) :
    IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    public async Task Handle(OrderCreatedIntegrationEvent @event)
    {
        logger.LogInformation(
            "Handling integration event: {IntegrationEventId} - Creating payouts for order {OrderId}",
            @event.Id, @event.OrderId);

        // Group line items by seller to ensure one payout per seller per order
        var sellerLineItems = @event.OrderLineItems
            .GroupBy(li => li.SellerId)
            .ToList();

        logger.LogInformation(
            "Order {OrderId} has {SellerCount} sellers with items",
            @event.OrderId, sellerLineItems.Count);

        foreach (var sellerGroup in sellerLineItems)
        {
            // For each seller in the order, create a payout entry for each line item
            foreach (var lineItem in sellerGroup)
            {
                var payout = new SellerPayout
                {
                    PayoutId = Guid.NewGuid(),
                    SellerId = ConvertSellerIdToGuid(lineItem.SellerId),
                    OrderId = @event.OrderId,
                    OrderLineItemId = lineItem.OrderLineItemId,
                    GrossAmount = lineItem.GrossAmount,
                    CommissionAmount = lineItem.CommissionAmount,
                    SellerAmount = lineItem.SellerAmount,
                    Status = SellerPayoutStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                await payoutRepository.CreatePayoutAsync(payout);

                logger.LogInformation(
                    "Created payout {PayoutId} for seller {SellerId}, " +
                    "gross amount: {GrossAmount}, commission: {CommissionAmount}, seller amount: {SellerAmount}",
                    payout.PayoutId, payout.SellerId, payout.GrossAmount,
                    payout.CommissionAmount, payout.SellerAmount);
            }
        }

        logger.LogInformation("Successfully created payouts for order {OrderId}", @event.OrderId);
    }

    private static Guid ConvertSellerIdToGuid(int sellerId)
    {
        // Convert int SellerId to Guid deterministically
        // This creates a GUID from the seller ID by padding it
        var bytes = new byte[16];
        BitConverter.GetBytes(sellerId).CopyTo(bytes, 0);
        return new Guid(bytes);
    }
}
