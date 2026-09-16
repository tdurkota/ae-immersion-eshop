namespace eShop.Sellers.API.Requests;

public record CreateSellerRequest(
    string Name,
    string Email,
    string? Description = null,
    string? PhoneNumber = null,
    string? BankAccountInfo = null,
    decimal CommissionRate = 0.15m);
