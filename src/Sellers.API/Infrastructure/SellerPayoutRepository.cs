using Microsoft.EntityFrameworkCore;
using eShop.Sellers.API.Model;

namespace eShop.Sellers.API.Infrastructure;

public interface ISellerPayoutRepository
{
    Task<List<SellerPayout>> GetPayoutsBySellerAsync(Guid sellerId, CancellationToken cancellationToken = default);
    Task<List<SellerPayout>> GetPayoutsBySellerWithFiltersAsync(
        Guid sellerId, 
        SellerPayoutStatus? statusFilter = null, 
        DateTime? dateFrom = null, 
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default);
    Task<SellerPayoutSummary> GetPayoutSummaryAsync(Guid sellerId, CancellationToken cancellationToken = default);
    Task<SellerPayout?> GetPayoutByIdAsync(Guid payoutId, CancellationToken cancellationToken = default);
    Task AddPayoutAsync(SellerPayout payout, CancellationToken cancellationToken = default);
    Task UpdatePayoutAsync(SellerPayout payout, CancellationToken cancellationToken = default);
    Task DeletePayoutAsync(Guid payoutId, CancellationToken cancellationToken = default);
}

public class SellerPayoutRepository : ISellerPayoutRepository
{
    private readonly SellersContext _context;

    public SellerPayoutRepository(SellersContext context)
    {
        _context = context;
    }

    public async Task<List<SellerPayout>> GetPayoutsBySellerAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        return await _context.SellerPayouts
            .Where(p => p.SellerId == sellerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SellerPayout>> GetPayoutsBySellerWithFiltersAsync(
        Guid sellerId,
        SellerPayoutStatus? statusFilter = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SellerPayouts
            .Where(p => p.SellerId == sellerId);

        if (statusFilter.HasValue)
        {
            query = query.Where(p => p.Status == statusFilter.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= dateTo.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SellerPayoutSummary> GetPayoutSummaryAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        var payouts = await GetPayoutsBySellerAsync(sellerId, cancellationToken);

        var summary = new SellerPayoutSummary
        {
            TotalEarned = payouts.Sum(p => p.SellerAmount),
            TotalPending = payouts
                .Where(p => p.Status == SellerPayoutStatus.Pending)
                .Sum(p => p.SellerAmount),
            TotalProcessed = payouts
                .Where(p => p.Status == SellerPayoutStatus.Processed)
                .Sum(p => p.SellerAmount),
            TotalPaid = payouts
                .Where(p => p.Status == SellerPayoutStatus.Paid)
                .Sum(p => p.SellerAmount)
        };

        return summary;
    }

    public async Task<SellerPayout?> GetPayoutByIdAsync(Guid payoutId, CancellationToken cancellationToken = default)
    {
        return await _context.SellerPayouts.FindAsync(new object[] { payoutId }, cancellationToken);
    }

    public async Task AddPayoutAsync(SellerPayout payout, CancellationToken cancellationToken = default)
    {
        _context.SellerPayouts.Add(payout);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePayoutAsync(SellerPayout payout, CancellationToken cancellationToken = default)
    {
        _context.SellerPayouts.Update(payout);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePayoutAsync(Guid payoutId, CancellationToken cancellationToken = default)
    {
        var payout = await GetPayoutByIdAsync(payoutId, cancellationToken);
        if (payout != null)
        {
            _context.SellerPayouts.Remove(payout);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class SellerPayoutSummary
{
    public decimal TotalEarned { get; set; }
    public decimal TotalPending { get; set; }
    public decimal TotalProcessed { get; set; }
    public decimal TotalPaid { get; set; }
}
