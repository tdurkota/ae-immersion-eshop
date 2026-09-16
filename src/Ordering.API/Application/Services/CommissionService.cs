namespace eShop.Ordering.Application.Services;

public class CommissionService
{
    /// <summary>
    /// Calculates commission amount based on gross amount and commission rate.
    /// </summary>
    /// <param name="grossAmount">The gross amount before commission</param>
    /// <param name="commissionRate">The commission rate as a decimal (e.g., 0.15 for 15%)</param>
    /// <returns>The calculated commission amount, rounded to 2 decimal places</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public decimal CalculateCommission(decimal grossAmount, decimal commissionRate)
    {
        ValidateInputs(grossAmount, commissionRate);

        var commission = grossAmount * commissionRate;
        return Math.Round(commission, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calculates the seller's amount (gross amount minus commission).
    /// </summary>
    /// <param name="grossAmount">The gross amount before commission</param>
    /// <param name="commissionRate">The commission rate as a decimal (e.g., 0.15 for 15%)</param>
    /// <returns>The seller's amount after commission, rounded to 2 decimal places</returns>
    public decimal CalculateSellerAmount(decimal grossAmount, decimal commissionRate)
    {
        ValidateInputs(grossAmount, commissionRate);

        var commission = CalculateCommission(grossAmount, commissionRate);
        var sellerAmount = grossAmount - commission;
        return Math.Round(sellerAmount, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calculates total commission for multiple line items.
    /// </summary>
    /// <param name="lineItems">Collection of line items with amount and commission rate</param>
    /// <returns>Total commission amount</returns>
    public decimal CalculateTotalCommission(IEnumerable<(decimal Amount, decimal CommissionRate)> lineItems)
    {
        if (lineItems == null)
        {
            throw new ArgumentNullException(nameof(lineItems));
        }

        var totalCommission = lineItems
            .Select(item => CalculateCommission(item.Amount, item.CommissionRate))
            .Aggregate(0m, (acc, commission) => acc + commission);

        return Math.Round(totalCommission, 2, MidpointRounding.AwayFromZero);
    }

    private void ValidateInputs(decimal grossAmount, decimal commissionRate)
    {
        if (grossAmount < 0)
        {
            throw new ArgumentException("Gross amount cannot be negative", nameof(grossAmount));
        }

        if (commissionRate < 0 || commissionRate > 1)
        {
            throw new ArgumentException("Commission rate must be between 0 and 1", nameof(commissionRate));
        }
    }
}
