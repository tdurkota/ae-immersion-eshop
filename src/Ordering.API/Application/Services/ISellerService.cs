namespace eShop.Ordering.API.Application.Services;

public interface ISellerService
{
    Task<bool> IsSellerActiveAsync(int sellerId, CancellationToken cancellationToken = default);
    Task<SellerValidationResult> ValidateSellerAsync(int sellerId, CancellationToken cancellationToken = default);
}

public record SellerValidationResult
{
    public bool IsValid { get; init; }
    public string ErrorMessage { get; init; }
    public decimal CommissionRate { get; init; }
}
