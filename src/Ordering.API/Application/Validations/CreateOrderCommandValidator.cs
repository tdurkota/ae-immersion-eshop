namespace eShop.Ordering.API.Application.Validations;

using eShop.Ordering.API.Application.Services;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    private readonly ISellerService _sellerService;

    public CreateOrderCommandValidator(ILogger<CreateOrderCommandValidator> logger, ISellerService sellerService = null)
    {
        _sellerService = sellerService;

        RuleFor(command => command.City).NotEmpty();
        RuleFor(command => command.Street).NotEmpty();
        RuleFor(command => command.State).NotEmpty();
        RuleFor(command => command.Country).NotEmpty();
        RuleFor(command => command.ZipCode).NotEmpty();
        RuleFor(command => command.CardNumber).NotEmpty().Length(12, 19);
        RuleFor(command => command.CardHolderName).NotEmpty();
        RuleFor(command => command.CardExpiration).NotEmpty().Must(BeValidExpirationDate).WithMessage("Please specify a valid card expiration date");
        RuleFor(command => command.CardSecurityNumber).NotEmpty().Length(3);
        RuleFor(command => command.CardTypeId).NotEmpty();
        RuleFor(command => command.OrderItems).Must(ContainOrderItems).WithMessage("No order items found");

        // Add async validation for seller status if service is available
        if (_sellerService != null)
        {
            RuleFor(command => command.OrderItems)
                .MustAsync(ValidateAllSellersAreActive)
                .WithMessage("One or more sellers in this order are inactive or suspended");
        }

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("INSTANCE CREATED - {ClassName}", GetType().Name);
        }
    }

    private bool BeValidExpirationDate(DateTime dateTime)
    {
        return dateTime >= DateTime.UtcNow;
    }

    private bool ContainOrderItems(IEnumerable<OrderItemDTO> orderItems)
    {
        return orderItems.Any();
    }

    private async Task<bool> ValidateAllSellersAreActive(IEnumerable<OrderItemDTO> orderItems, CancellationToken cancellationToken)
    {
        if (_sellerService == null)
            return true;

        var sellerIds = orderItems.Select(item => item.SellerId).Distinct();

        foreach (var sellerId in sellerIds)
        {
            if (!await _sellerService.IsSellerActiveAsync(sellerId, cancellationToken))
            {
                return false;
            }
        }

        return true;
    }
}
