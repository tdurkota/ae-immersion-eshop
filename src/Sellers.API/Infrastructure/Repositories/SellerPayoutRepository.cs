namespace eShop.Sellers.API.Infrastructure.Repositories;

using eShop.Sellers.API.Model;

public interface ISellerPayoutRepository
{
    Task<SellerPayout> CreatePayoutAsync(SellerPayout payout, CancellationToken cancellationToken = default);
    Task<List<SellerPayout>> GetPayoutsBySellerAsync(Guid sellerId, CancellationToken cancellationToken = default);
    Task<SellerPayout?> GetPayoutAsync(Guid payoutId, CancellationToken cancellationToken = default);
    Task<int> UpdatePayoutStatusAsync(Guid payoutId, SellerPayoutStatus status, DateTime? paidAt = null, CancellationToken cancellationToken = default);
    Task<List<SellerPayout>> GetPayoutsBySellerAndStatusAsync(Guid sellerId, SellerPayoutStatus status, CancellationToken cancellationToken = default);
}

public class SellerPayoutRepository(SellersContext context) : ISellerPayoutRepository
{
    public async Task<SellerPayout> CreatePayoutAsync(SellerPayout payout, CancellationToken cancellationToken = default)
    {
        context.SellerPayouts.Add(payout);
        await context.SaveChangesAsync(cancellationToken);
        return payout;
    }

    public async Task<List<SellerPayout>> GetPayoutsBySellerAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        return await context.SellerPayouts
            .Where(p => p.SellerId == sellerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SellerPayout?> GetPayoutAsync(Guid payoutId, CancellationToken cancellationToken = default)
    {
        return await context.SellerPayouts
            .FirstOrDefaultAsync(p => p.PayoutId == payoutId, cancellationToken);
    }

    public async Task<int> UpdatePayoutStatusAsync(Guid payoutId, SellerPayoutStatus status, DateTime? paidAt = null, CancellationToken cancellationToken = default)
    {
        var payout = await context.SellerPayouts
            .FirstOrDefaultAsync(p => p.PayoutId == payoutId, cancellationToken);

        if (payout == null)
        {
            return 0;
        }

        payout.Status = status;
        if (paidAt.HasValue)
        {
            payout.PaidAt = paidAt;
        }

        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<SellerPayout>> GetPayoutsBySellerAndStatusAsync(Guid sellerId, SellerPayoutStatus status, CancellationToken cancellationToken = default)
    {
        return await context.SellerPayouts
            .Where(p => p.SellerId == sellerId && p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
