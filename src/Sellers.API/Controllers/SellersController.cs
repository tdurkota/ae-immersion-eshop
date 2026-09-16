namespace eShop.Sellers.API.Controllers;

using eShop.Sellers.API.Infrastructure;
using eShop.Sellers.API.Responses;

[ApiController]
[Route("api/sellers")]
public class SellersController(
    SellersContext context, 
    ILogger<SellersController> logger,
    ISellerPayoutRepository payoutRepository) : ControllerBase
{
    private readonly SellersContext _context = context;
    private readonly ILogger<SellersController> _logger = logger;
    private readonly ISellerPayoutRepository _payoutRepository = payoutRepository;

    /// <summary>
    /// Register a new seller
    /// </summary>
    /// <remarks>
    /// Task 4.1: POST endpoint for seller registration with validation
    /// - Validates required fields (Name, Email)
    /// - Validates email format
    /// - Ensures email is unique
    /// - Returns 201 Created with seller data
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SellerResponse>> CreateSeller(
        CreateSellerRequest request,
        CancellationToken cancellationToken)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required" });

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required" });

        // Normalize email before validation
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Validate email format
        if (!IsValidEmail(normalizedEmail))
            return BadRequest(new { error = "Email format is invalid" });

        // Validate commission rate
        if (request.CommissionRate < 0 || request.CommissionRate > 1)
            return BadRequest(new { error = "Commission rate must be between 0 and 1" });

        // Check for duplicate email
        var existingSeller = await _context.Sellers
            .FirstOrDefaultAsync(s => s.Email == normalizedEmail, cancellationToken);
        
        if (existingSeller != null)
            return Conflict(new { error = "Email already registered" });

        var seller = new Seller
        {
            SellerId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            Description = request.Description?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            BankAccountInfo = request.BankAccountInfo?.Trim(),
            CommissionRate = request.CommissionRate,
            Status = SellerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Sellers.Add(seller);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seller {SellerId} registered successfully with email {Email}", 
            seller.SellerId, seller.Email);

        return CreatedAtAction(nameof(GetSeller), new { id = seller.SellerId }, 
            MapToResponse(seller, isOwner: true));
    }

    /// <summary>
    /// Get seller profile
    /// </summary>
    /// <remarks>
    /// Task 4.2: GET endpoint for seller profile
    /// - Only owner can view private fields (Email, BankAccountInfo)
    /// - Non-owner gets public fields only (returns 403 for private access)
    /// - Requires seller_id claim in token for authorization
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SellerResponse>> GetSeller(
        Guid id,
        CancellationToken cancellationToken)
    {
        var seller = await _context.Sellers.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        
        if (seller == null)
            return NotFound(new { error = "Seller not found" });

        var isOwner = IsSellerOwner(id);
        
        return Ok(MapToResponse(seller, isOwner));
    }

    /// <summary>
    /// Update seller profile
    /// </summary>
    /// <remarks>
    /// Task 4.3: PUT endpoint for updating seller profile
    /// - Only owner can update their own profile
    /// - Returns 403 if not authorized
    /// - Can update Name, Description, PhoneNumber, BankAccountInfo
    /// - Email and CommissionRate cannot be updated
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SellerResponse>> UpdateSeller(
        Guid id,
        UpdateSellerRequest request,
        CancellationToken cancellationToken)
    {
        var seller = await _context.Sellers.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        
        if (seller == null)
            return NotFound(new { error = "Seller not found" });

        if (!IsSellerOwner(id))
            return Forbid("You can only update your own profile");

        // Update allowed fields
        if (!string.IsNullOrWhiteSpace(request.Name))
            seller.Name = request.Name.Trim();

        if (request.Description != null)
            seller.Description = request.Description.Trim();

        if (request.PhoneNumber != null)
            seller.PhoneNumber = request.PhoneNumber.Trim();

        if (request.BankAccountInfo != null)
            seller.BankAccountInfo = request.BankAccountInfo.Trim();

        seller.UpdatedAt = DateTime.UtcNow;

        _context.Sellers.Update(seller);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seller {SellerId} profile updated", id);

        return Ok(MapToResponse(seller, isOwner: true));
    }

    /// <summary>
    /// Get seller's orders
    /// </summary>
    /// <remarks>
    /// Task 10.1: GET endpoint for seller to view their orders
    /// - Returns orders containing seller's items
    /// - Requires seller authentication
    /// - Supports pagination
    /// </remarks>
    [HttpGet("{id:guid}/orders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetSellerOrders(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Verify seller exists and user is authorized
        var seller = await _context.Sellers.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        
        if (seller == null)
            return NotFound(new { error = "Seller not found" });

        if (!IsSellerOwner(id))
            return Forbid("You can only view your own orders");

        // Get all payouts for this seller
        var payouts = await _payoutRepository.GetPayoutsBySellerAsync(id, cancellationToken);

        // Group by order ID to get distinct orders
        var orderGroups = payouts
            .GroupBy(p => p.OrderId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var orders = orderGroups.Select(group => new SellerOrderResponse(
            OrderId: group.Key,
            Status: "Completed",
            OrderDate: group.Min(p => p.CreatedAt),
            TotalAmount: group.Sum(p => p.GrossAmount),
            Items: group.Select(p => new OrderItemDetail(
                OrderLineItemId: p.OrderLineItemId,
                UnitPrice: p.GrossAmount,
                Quantity: 1,
                ItemTotal: p.GrossAmount,
                SellerAmount: p.SellerAmount,
                PayoutStatus: p.Status.ToString()
            )).ToList()
        )).ToList();

        return Ok(new
        {
            orders,
            total = payouts.GroupBy(p => p.OrderId).Count(),
            page,
            pageSize
        });
    }

    /// <summary>
    /// Get seller's payouts
    /// </summary>
    /// <remarks>
    /// Task 10.2: GET endpoint for seller to view payout ledger
    /// - Filters by status (Pending, Processed, Paid)
    /// - Filters by date range
    /// - Returns paginated results
    /// - Requires seller authentication
    /// </remarks>
    [HttpGet("{id:guid}/payouts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetSellerPayouts(
        Guid id,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Verify seller exists and user is authorized
        var seller = await _context.Sellers.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        
        if (seller == null)
            return NotFound(new { error = "Seller not found" });

        if (!IsSellerOwner(id))
            return Forbid("You can only view your own payouts");

        // Parse status filter if provided
        SellerPayoutStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<SellerPayoutStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                statusFilter = parsedStatus;
            }
        }

        // Get filtered payouts
        var payouts = await _payoutRepository.GetPayoutsBySellerWithFiltersAsync(
            id, statusFilter, dateFrom, dateTo, cancellationToken);

        // Apply pagination
        var totalCount = payouts.Count;
        var paginatedPayouts = payouts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var responses = paginatedPayouts.Select(p => new SellerPayoutResponse(
            PayoutId: p.PayoutId,
            OrderId: p.OrderId,
            OrderLineItemId: p.OrderLineItemId,
            GrossAmount: p.GrossAmount,
            CommissionAmount: p.CommissionAmount,
            SellerAmount: p.SellerAmount,
            Status: p.Status.ToString(),
            CreatedAt: p.CreatedAt,
            ProcessedAt: p.PaidAt
        )).ToList();

        return Ok(new
        {
            payouts = responses,
            total = totalCount,
            page,
            pageSize
        });
    }

    /// <summary>
    /// Get seller's payout summary
    /// </summary>
    /// <remarks>
    /// Task 10.3: GET endpoint for seller to view payout summary
    /// - Calculates totals by status
    /// - Returns total_earned, total_pending, total_processed, total_paid
    /// - Requires seller authentication
    /// </remarks>
    [HttpGet("{id:guid}/payouts/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SellerPayoutSummaryResponse>> GetSellerPayoutSummary(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Verify seller exists and user is authorized
        var seller = await _context.Sellers.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        
        if (seller == null)
            return NotFound(new { error = "Seller not found" });

        if (!IsSellerOwner(id))
            return Forbid("You can only view your own payout summary");

        // Get payout summary
        var summary = await _payoutRepository.GetPayoutSummaryAsync(id, cancellationToken);

        return Ok(new SellerPayoutSummaryResponse(
            TotalEarned: summary.TotalEarned,
            TotalPending: summary.TotalPending,
            TotalProcessed: summary.TotalProcessed,
            TotalPaid: summary.TotalPaid
        ));
    }

    private bool IsSellerOwner(Guid sellerId)
    {
        // Extract seller_id claim from JWT token
        if (User?.Identity?.IsAuthenticated != true)
            return false;

        var sellerIdClaim = User.FindFirst("seller_id")?.Value;
        
        if (string.IsNullOrEmpty(sellerIdClaim))
            return false;

        return Guid.TryParse(sellerIdClaim, out var tokenSellerId) && tokenSellerId == sellerId;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static SellerResponse MapToResponse(Seller seller, bool isOwner = false)
    {
        return new SellerResponse(
            SellerId: seller.SellerId,
            Name: seller.Name,
            Description: seller.Description,
            PhoneNumber: seller.PhoneNumber,
            CommissionRate: seller.CommissionRate,
            Status: seller.Status.ToString(),
            CreatedAt: seller.CreatedAt,
            UpdatedAt: seller.UpdatedAt,
            Email: isOwner ? seller.Email : null,
            BankAccountInfo: isOwner ? seller.BankAccountInfo : null);
    }
}
