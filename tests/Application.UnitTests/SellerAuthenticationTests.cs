using eShop.Identity.API.Models;

namespace eShop.Application.UnitTests;

[TestClass]
public class SellerAuthenticationTests
{
    [TestMethod]
    public void UserRoles_SellerRoleConstant_IsCorrect()
    {
        // Assert
        Assert.AreEqual("seller", UserRoles.Seller);
    }

    [TestMethod]
    public void UserRoles_CustomerRoleConstant_IsCorrect()
    {
        // Assert
        Assert.AreEqual("customer", UserRoles.Customer);
    }

    [TestMethod]
    public void UserRoles_AdminRoleConstant_IsCorrect()
    {
        // Assert
        Assert.AreEqual("admin", UserRoles.Admin);
    }

    [TestMethod]
    public void ApplicationUser_WithSellerId_CanBeConstructed()
    {
        // Arrange
        var sellerId = Guid.NewGuid();

        // Act
        var user = new ApplicationUser
        {
            Id = "test-id",
            UserName = "seller@test.com",
            Email = "seller@test.com",
            Name = "Test",
            LastName = "Seller",
            SellerId = sellerId,
            CardNumber = "4111111111111111",
            SecurityNumber = "123",
            Expiration = "12/25",
            CardHolderName = "Test Seller",
            CardType = 1,
            Street = "123 Main St",
            City = "Test City",
            State = "TS",
            Country = "US",
            ZipCode = "12345"
        };

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(sellerId, user.SellerId);
        Assert.AreEqual("seller@test.com", user.Email);
    }

    [TestMethod]
    public void ApplicationUser_WithoutSellerId_DefaultsToNull()
    {
        // Act
        var user = new ApplicationUser
        {
            Id = "test-id",
            UserName = "customer@test.com",
            Email = "customer@test.com",
            Name = "Test",
            LastName = "Customer",
            CardNumber = "4111111111111111",
            SecurityNumber = "123",
            Expiration = "12/25",
            CardHolderName = "Test Customer",
            CardType = 1,
            Street = "123 Main St",
            City = "Test City",
            State = "TS",
            Country = "US",
            ZipCode = "12345"
        };

        // Assert
        Assert.IsNull(user.SellerId);
    }

    [TestMethod]
    public void ApplicationUser_SellerIdProperty_IsNullableGuid()
    {
        // Arrange
        var user = new ApplicationUser { SellerId = Guid.NewGuid() };
        
        // Act & Assert
        Assert.IsNotNull(user.SellerId);
        Assert.IsInstanceOfType(user.SellerId, typeof(Guid?));
    }
}
