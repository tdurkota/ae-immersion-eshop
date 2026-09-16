namespace eShop.Ordering.API.Application.Commands;

using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Application.Services;

// Regular CommandHandler
public class CreateOrderCommandHandler
    : IRequestHandler<CreateOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IIdentityService _identityService;
    private readonly IMediator _mediator;
    private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private readonly CommissionService _commissionService;

    // Using DI to inject infrastructure persistence Repositories
    public CreateOrderCommandHandler(IMediator mediator,
        IOrderingIntegrationEventService orderingIntegrationEventService,
        IOrderRepository orderRepository,
        IIdentityService identityService,
        ILogger<CreateOrderCommandHandler> logger,
        CommissionService commissionService)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _orderingIntegrationEventService = orderingIntegrationEventService ?? throw new ArgumentNullException(nameof(orderingIntegrationEventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _commissionService = commissionService ?? throw new ArgumentNullException(nameof(commissionService));
    }

    public async Task<bool> Handle(CreateOrderCommand message, CancellationToken cancellationToken)
    {
        // Add Integration event to clean the basket
        var orderStartedIntegrationEvent = new OrderStartedIntegrationEvent(message.UserId);
        await _orderingIntegrationEventService.AddAndSaveEventAsync(orderStartedIntegrationEvent);

        // Add/Update the Buyer AggregateRoot
        // DDD patterns comment: Add child entities and value-objects through the Order Aggregate-Root
        // methods and constructor so validations, invariants and business logic 
        // make sure that consistency is preserved across the whole aggregate
        var address = new Address(message.Street, message.City, message.State, message.Country, message.ZipCode);
        var order = new Order(message.UserId, message.UserName, address, message.CardTypeId, message.CardNumber, message.CardSecurityNumber, message.CardHolderName, message.CardExpiration);

        foreach (var item in message.OrderItems)
        {
            order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.PictureUrl, item.Units, item.SellerId, item.CommissionRate);
        }

        _logger.LogInformation("Creating Order - Order: {@Order}", order);

        _orderRepository.Add(order);

        var result = await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        if (result)
        {
            // Publish OrderCreatedIntegrationEvent with seller info for each line item
            var lineItems = order.OrderItems.Select((orderItem, index) =>
            {
                var grossAmount = (orderItem.UnitPrice * orderItem.Units) - orderItem.Discount;
                var commissionAmount = _commissionService.CalculateCommission(grossAmount, orderItem.CommissionRate);
                var sellerAmount = _commissionService.CalculateSellerAmount(grossAmount, orderItem.CommissionRate);

                return new OrderCreatedLineItem(
                    orderLineItemId: index, // Using zero-based index as the line item ID
                    sellerId: orderItem.SellerId,
                    productId: orderItem.ProductId,
                    productName: orderItem.ProductName,
                    unitPrice: orderItem.UnitPrice,
                    units: orderItem.Units,
                    discount: orderItem.Discount,
                    grossAmount: grossAmount,
                    commissionRate: orderItem.CommissionRate,
                    commissionAmount: commissionAmount,
                    sellerAmount: sellerAmount
                );
            }).ToList();

            var orderCreatedIntegrationEvent = new OrderCreatedIntegrationEvent(
                orderId: order.Id,
                buyerName: message.UserName,
                buyerIdentityGuid: message.UserId,
                orderLineItems: lineItems
            );

            await _orderingIntegrationEventService.AddAndSaveEventAsync(orderCreatedIntegrationEvent);
        }

        return result;
    }
}


// Use for Idempotency in Command process
public class CreateOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CreateOrderCommand, bool>
{
    public CreateOrderIdentifiedCommandHandler(
        IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<CreateOrderCommand, bool>> logger)
        : base(mediator, requestManager, logger)
    {
    }

    protected override bool CreateResultForDuplicateRequest()
    {
        return true; // Ignore duplicate requests for creating order.
    }
}
