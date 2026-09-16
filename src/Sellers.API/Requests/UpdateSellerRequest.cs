namespace eShop.Sellers.API.Requests;

public record UpdateSellerRequest(
    string? Name = null,
    string? Description = null,
    string? PhoneNumber = null,
    string? BankAccountInfo = null);
