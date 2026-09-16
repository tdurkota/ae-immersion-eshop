namespace eShop.Sellers.API.Infrastructure;

public interface ISellerPayoutRepository
{
    Task<List<SellerPayout>> GetPayoutsBySellerAsync(Guid sellerId, CancellationToken cancellationToken = default);
    
    Task<List<SellerPayout>> GetPayoutsBySellerWithFiltersAsync(
        Guid sellerId, 
        SellerPayoutStatus? status = null, 
        DateTime? fromDate = null, 
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
    
    Task<SellerPayoutSummary> GetPayoutSummaryAsync(Guid sellerId, CancellationToken cancellationToken = default);
    
    Task<SellerPayout> CreatePayoutAsync(SellerPayout payout, CancellationToken cancellationToken = default);
    
    Task<SellerPayout> UpdatePayoutStatusAsync(Guid payoutId, SellerPayoutStatus status, CancellationToken cancellationToken = default);
}

public class SellerPayoutSummary
{
    public decimal TotalEarned { get; set; }
    public decimal TotalPending { get; set; }
    public decimal TotalProcessed { get; set; }
    public decimal TotalPaid { get; set; }
}

public class SellerPayoutRepository(SellersContext context) : ISellerPayoutRepository
{
    private readonly SellersContext _context = context;

    public async Task<List<SellerPayout>> GetPayoutsBySellerAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        return await _context.SellerPayouts
            .Where(p => p.SellerId == sellerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SellerPayout>> GetPayoutsBySellerWithFiltersAsync(
        Guid sellerId,
        SellerPayoutStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SellerPayouts.Where(p => p.SellerId == sellerId);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(p => p.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SellerPayoutSummary> GetPayoutSummaryAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        var payouts = await _context.SellerPayouts
            .Where(p => p.SellerId == sellerId)
            .ToListAsync(cancellationToken);

        return new SellerPayoutSummary
        {
            TotalEarned = payouts.Sum(p => p.SellerAmount),
            TotalPending = payouts.Where(p => p.Status == SellerPayoutStatus.Pending).Sum(p => p.SellerAmount),
            TotalProcessed = payouts.Where(p => p.Status == SellerPayoutStatus.Processed).Sum(p => p.SellerAmount),
            TotalPaid = payouts.Where(p => p.Status == SellerPayoutStatus.Paid).Sum(p => p.SellerAmount)
        };
    }

    public async Task<SellerPayout> CreatePayoutAsync(SellerPayout payout, CancellationToken cancellationToken = default)
    {
        _context.SellerPayouts.Add(payout);
        await _context.SaveChangesAsync(cancellationToken);
        return payout;
    }

    public async Task<SellerPayout> UpdatePayoutStatusAsync(Guid payoutId, SellerPayoutStatus status, CancellationToken cancellationToken = default)
    {
        var payout = await _context.SellerPayouts.FindAsync(new object[] { payoutId }, cancellationToken: cancellationToken);
        if (payout == null)
            throw new KeyNotFoundException($"Payout {payoutId} not found");

        payout.Status = status;
        if (status == SellerPayoutStatus.Paid)
            payout.PaidAt = DateTime.UtcNow;

        _context.SellerPayouts.Update(payout);
        await _context.SaveChangesAsync(cancellationToken);
        return payout;
    }
}
