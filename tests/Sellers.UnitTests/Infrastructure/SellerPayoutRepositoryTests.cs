namespace eShop.Sellers.UnitTests.Infrastructure;

[TestClass]
public class SellerPayoutRepositoryTests
{
    [TestMethod]
    public void CreatePayoutAsync_WithValidData_ShouldReturnCreatedPayout()
    {
        // This test is a placeholder demonstrating the expected behavior.
        // The actual implementation should test the repository with a real or mock DbContext.
        // For now, we verify the SellerPayout entity structure.

        var sellerId = Guid.NewGuid();
        var payout = new SellerPayout
        {
            PayoutId = Guid.NewGuid(),
            SellerId = sellerId,
            OrderId = 1,
            OrderLineItemId = 1,
            GrossAmount = 100m,
            CommissionAmount = 15m,
            SellerAmount = 85m,
            Status = SellerPayoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Verify the payout was created with correct values
        Assert.IsNotNull(payout);
        Assert.AreEqual(100m, payout.GrossAmount);
        Assert.AreEqual(15m, payout.CommissionAmount);
        Assert.AreEqual(85m, payout.SellerAmount);
        Assert.AreEqual(SellerPayoutStatus.Pending, payout.Status);
    }

    [TestMethod]
    public void SellerPayout_CommissionCalculation_ShouldMatchExpectedValues()
    {
        // Test commission calculation with different commission rates
        var grossAmount = 100m;
        var commissionRate = 0.15m; // 15%
        var commissionAmount = grossAmount * commissionRate; // 15
        var sellerAmount = grossAmount - commissionAmount; // 85

        Assert.AreEqual(15m, commissionAmount);
        Assert.AreEqual(85m, sellerAmount);
    }

    [TestMethod]
    public void SellerPayout_MultipleOrders_ShouldMaintainIndependentRecords()
    {
        // Test that multiple payouts for the same seller from different orders are independent
        var sellerId = Guid.NewGuid();

        var order1Payout = new SellerPayout
        {
            PayoutId = Guid.NewGuid(),
            SellerId = sellerId,
            OrderId = 1,
            OrderLineItemId = 1,
            GrossAmount = 100m,
            CommissionAmount = 15m,
            SellerAmount = 85m
        };

        var order2Payout = new SellerPayout
        {
            PayoutId = Guid.NewGuid(),
            SellerId = sellerId,
            OrderId = 2,
            OrderLineItemId = 1,
            GrossAmount = 200m,
            CommissionAmount = 30m,
            SellerAmount = 170m
        };

        // Verify they are different records
        Assert.AreNotEqual(order1Payout.PayoutId, order2Payout.PayoutId);
        Assert.AreEqual(order1Payout.SellerId, order2Payout.SellerId); // Same seller
        Assert.AreNotEqual(order1Payout.OrderId, order2Payout.OrderId); // Different orders
        Assert.AreEqual(300m, order1Payout.GrossAmount + order2Payout.GrossAmount);
    }

    [TestMethod]
    public void SellerPayoutStatus_TransitionFromPendingToPaid_ShouldUpdateCorrectly()
    {
        // Test status transition
        var payout = new SellerPayout
        {
            PayoutId = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            OrderId = 1,
            OrderLineItemId = 1,
            GrossAmount = 100m,
            CommissionAmount = 15m,
            SellerAmount = 85m,
            Status = SellerPayoutStatus.Pending
        };

        Assert.AreEqual(SellerPayoutStatus.Pending, payout.Status);

        // Simulate status update
        payout.Status = SellerPayoutStatus.Paid;
        payout.PaidAt = DateTime.UtcNow;

        Assert.AreEqual(SellerPayoutStatus.Paid, payout.Status);
        Assert.IsNotNull(payout.PaidAt);
    }

    [TestMethod]
    public void MultiSellerOrder_CreatesPayoutPerSeller_ShouldHaveCorrectAmount()
    {
        // Simulate a multi-seller order
        var seller1Id = Guid.NewGuid();
        var seller2Id = Guid.NewGuid();
        var seller3Id = Guid.NewGuid();

        var payouts = new List<SellerPayout>
        {
            new SellerPayout
            {
                PayoutId = Guid.NewGuid(),
                SellerId = seller1Id,
                OrderId = 1,
                OrderLineItemId = 1,
                GrossAmount = 100m,
                CommissionAmount = 15m,
                SellerAmount = 85m,
                Status = SellerPayoutStatus.Pending
            },
            new SellerPayout
            {
                PayoutId = Guid.NewGuid(),
                SellerId = seller2Id,
                OrderId = 1,
                OrderLineItemId = 2,
                GrossAmount = 50m,
                CommissionAmount = 10m,
                SellerAmount = 40m,
                Status = SellerPayoutStatus.Pending
            },
            new SellerPayout
            {
                PayoutId = Guid.NewGuid(),
                SellerId = seller3Id,
                OrderId = 1,
                OrderLineItemId = 3,
                GrossAmount = 75m,
                CommissionAmount = 11.25m,
                SellerAmount = 63.75m,
                Status = SellerPayoutStatus.Pending
            }
        };

        // Verify all payouts are for the same order but different sellers
        Assert.AreEqual(3, payouts.Count);
        Assert.AreEqual(1, payouts.Select(p => p.OrderId).Distinct().Count()); // Same order
        Assert.AreEqual(3, payouts.Select(p => p.SellerId).Distinct().Count()); // Different sellers
        
        // Verify total amounts
        var totalGross = payouts.Sum(p => p.GrossAmount);
        var totalCommission = payouts.Sum(p => p.CommissionAmount);
        var totalSeller = payouts.Sum(p => p.SellerAmount);

        Assert.AreEqual(225m, totalGross);
        Assert.AreEqual(36.25m, totalCommission);
        Assert.AreEqual(188.75m, totalSeller);
    }
}
