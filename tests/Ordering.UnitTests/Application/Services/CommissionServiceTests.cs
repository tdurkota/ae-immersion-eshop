using eShop.Ordering.Application.Services;

namespace Ordering.UnitTests.Application.Services;

public class CommissionServiceTests
{
    private readonly CommissionService _commissionService;

    public CommissionServiceTests()
    {
        _commissionService = new CommissionService();
    }

    #region CalculateCommission Tests

    [Fact]
    public void CalculateCommission_WithValidInputs_ReturnsCorrectAmount()
    {
        // Arrange
        decimal grossAmount = 100m;
        decimal commissionRate = 0.15m;

        // Act
        var result = _commissionService.CalculateCommission(grossAmount, commissionRate);

        // Assert
        result.Should().Be(15m);
    }

    [Fact]
    public void CalculateCommission_WithZeroGrossAmount_ReturnsZero()
    {
        // Arrange
        decimal grossAmount = 0m;
        decimal commissionRate = 0.15m;

        // Act
        var result = _commissionService.CalculateCommission(grossAmount, commissionRate);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateCommission_WithZeroCommissionRate_ReturnsZero()
    {
        // Arrange
        decimal grossAmount = 100m;
        decimal commissionRate = 0m;

        // Act
        var result = _commissionService.CalculateCommission(grossAmount, commissionRate);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateCommission_WithMaxCommissionRate_ReturnsFullAmount()
    {
        // Arrange
        decimal grossAmount = 100m;
        decimal commissionRate = 1m;

        // Act
        var result = _commissionService.CalculateCommission(grossAmount, commissionRate);

        // Assert
        result.Should().Be(100m);
    }

    [Fact]
    public void CalculateCommission_WithDecimalPrecision_RoundsCorrectly()
    {
        // Arrange
        decimal grossAmount = 99.99m;
        decimal commissionRate = 0.15m;
        // 99.99 * 0.15 = 14.9985, should round to 15.00

        // Act
        var result = _commissionService.CalculateCommission(grossAmount, commissionRate);

        // Assert
        result.Should().Be(15.00m);
    }

    [Fact]
    public void CalculateCommission_WithSmallAmount_RoundsCorrectly()
    {
        // Arrange
        decimal grossAmount = 0.01m;
        decimal commissionRate = 0.15m;
        // 0.01 * 0.15 = 0.0015, should round to 0.00

        // Act
        var result = _commissionService.CalculateCommission(grossAmount, commissionRate);

        // Assert
        result.Should().Be(0.00m);
    }

    [Fact]
    public void CalculateCommission_WithNegativeGrossAmount_ThrowsArgumentException()
    {
        // Arrange
        decimal grossAmount = -100m;
        decimal commissionRate = 0.15m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _commissionService.CalculateCommission(grossAmount, commissionRate));
        exception.ParamName.Should().Be(nameof(grossAmount));
    }

    [Fact]
    public void CalculateCommission_WithCommissionRateBelowZero_ThrowsArgumentException()
    {
        // Arrange
        decimal grossAmount = 100m;
        decimal commissionRate = -0.1m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _commissionService.CalculateCommission(grossAmount, commissionRate));
        exception.ParamName.Should().Be(nameof(commissionRate));
    }

    [Fact]
    public void CalculateCommission_WithCommissionRateAboveOne_ThrowsArgumentException()
    {
        // Arrange
        decimal grossAmount = 100m;
        decimal commissionRate = 1.1m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _commissionService.CalculateCommission(grossAmount, commissionRate));
        exception.ParamName.Should().Be(nameof(commissionRate));
    }

    #endregion

    #region CalculateSellerAmount Tests

    [Fact]
    public void CalculateSellerAmount_WithValidInputs_ReturnsCorrectAmount()
    {
        // Arrange
        decimal grossAmount = 100m;
        decimal commissionRate = 0.15m;
        // Expected: 100 - (100 * 0.15) = 100 - 15 = 85

        // Act
        var result = _commissionService.CalculateSellerAmount(grossAmount, commissionRate);

        // Assert
        result.Should().Be(85m);
    }

    [Fact]
    public void CalculateSellerAmount_WithZeroCommissionRate_ReturnsFullAmount()
    {
        // Arrange
        decimal grossAmount = 100m;
        decimal commissionRate = 0m;

        // Act
        var result = _commissionService.CalculateSellerAmount(grossAmount, commissionRate);

        // Assert
        result.Should().Be(100m);
    }

    [Fact]
    public void CalculateSellerAmount_WithMaxCommissionRate_ReturnsZero()
    {
        // Arrange
        decimal grossAmount = 100m;
        decimal commissionRate = 1m;

        // Act
        var result = _commissionService.CalculateSellerAmount(grossAmount, commissionRate);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateSellerAmount_WithDecimalPrecision_RoundsCorrectly()
    {
        // Arrange
        decimal grossAmount = 99.99m;
        decimal commissionRate = 0.15m;
        // Gross: 99.99, Commission: 15.00, Seller: 84.99

        // Act
        var result = _commissionService.CalculateSellerAmount(grossAmount, commissionRate);

        // Assert
        result.Should().Be(84.99m);
    }

    #endregion

    #region CalculateTotalCommission Tests

    [Fact]
    public void CalculateTotalCommission_WithMultipleItems_ReturnsSumOfCommissions()
    {
        // Arrange
        var lineItems = new[]
        {
            (100m, 0.15m),  // Commission: 15
            (50m, 0.15m),   // Commission: 7.50
            (75m, 0.10m)    // Commission: 7.50
        };

        // Act
        var result = _commissionService.CalculateTotalCommission(lineItems);

        // Assert
        result.Should().Be(30m);
    }

    [Fact]
    public void CalculateTotalCommission_WithDifferentRates_CalculatesEachCorrectly()
    {
        // Arrange
        var lineItems = new[]
        {
            (100m, 0.15m),  // Commission: 15
            (100m, 0.10m),  // Commission: 10
            (100m, 0.20m)   // Commission: 20
        };

        // Act
        var result = _commissionService.CalculateTotalCommission(lineItems);

        // Assert
        result.Should().Be(45m);
    }

    [Fact]
    public void CalculateTotalCommission_WithEmptyCollection_ReturnsZero()
    {
        // Arrange
        var lineItems = Enumerable.Empty<(decimal, decimal)>();

        // Act
        var result = _commissionService.CalculateTotalCommission(lineItems);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateTotalCommission_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<(decimal, decimal)> lineItems = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _commissionService.CalculateTotalCommission(lineItems));
    }

    [Fact]
    public void CalculateTotalCommission_WithSingleItem_ReturnsSingleCommission()
    {
        // Arrange
        var lineItems = new[] { (100m, 0.15m) };

        // Act
        var result = _commissionService.CalculateTotalCommission(lineItems);

        // Assert
        result.Should().Be(15m);
    }

    [Fact]
    public void CalculateTotalCommission_WithDecimalPrecision_RoundsCorrectly()
    {
        // Arrange
        var lineItems = new[]
        {
            (99.99m, 0.15m),  // Commission: 14.9985 -> 15.00
            (99.99m, 0.15m)   // Commission: 14.9985 -> 15.00
        };

        // Act
        var result = _commissionService.CalculateTotalCommission(lineItems);

        // Assert
        result.Should().Be(30m);
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.5)]
    [InlineData(0.99)]
    public void CalculateCommission_WithVariousRates_ProducesValidResults(decimal commissionRate)
    {
        // Arrange
        decimal grossAmount = 100m;

        // Act
        var commission = _commissionService.CalculateCommission(grossAmount, commissionRate);
        var sellerAmount = _commissionService.CalculateSellerAmount(grossAmount, commissionRate);

        // Assert
        (commission + sellerAmount).Should().Be(grossAmount);
    }

    [Fact]
    public void CalculateCommission_WithLargeAmount_HandlesCorrectly()
    {
        // Arrange
        decimal grossAmount = 999999.99m;
        decimal commissionRate = 0.15m;

        // Act
        var result = _commissionService.CalculateCommission(grossAmount, commissionRate);

        // Assert
        result.Should().Be(149999.9985m);  // Will be rounded by the method
    }

    #endregion
}
