namespace eShop.Sellers.API.Controllers;

/// <summary>
/// Sellers API
/// 
/// Manages seller registration, profile management, and seller-specific business operations including
/// order tracking and payout ledger management. Requires seller role and seller_id claim for most endpoints.
/// </summary>
[ApiController]
[Route("api/sellers")]
[Produces("application/json")]
public class SellersController(SellersContext context, ISellerPayoutRepository payoutRepository, ILogger<SellersController> logger) : ControllerBase
{
    private readonly SellersContext _context = context;
    private readonly ISellerPayoutRepository _payoutRepository = payoutRepository;
    private readonly ILogger<SellersController> _logger = logger;

    /// <summary>
    /// Register a new seller (Public)
    /// </summary>
    /// <remarks>
    /// Creates a new seller account. No authentication required for registration.
    /// 
    /// **Success Example:**
    /// - **Request:** POST /api/sellers
    /// - **Body:** { "name": "Premium Electronics", "email": "seller@example.com", "description": "High-quality electronics", "commissionRate": 0.15 }
    /// - **Response (201 Created):** { "sellerId": "550e8400-e29b-41d4-a716-446655440000", "name": "Premium Electronics", "email": "seller@example.com", "commissionRate": 0.15, "status": "Active", "createdAt": "2026-09-16T12:00:00Z" }
    /// 
    /// **Validation Examples:**
    /// - Email already registered → **409 Conflict**: { "error": "Email already registered" }
    /// - Invalid email format → **400 Bad Request**: { "error": "Email format is invalid" }
    /// - Commission rate > 1 → **400 Bad Request**: { "error": "Commission rate must be between 0 and 1" }
    /// - Missing required field → **400 Bad Request**: { "error": "Name is required" }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(SellerResponse))]
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
    /// Get seller profile (Authenticated)
    /// </summary>
    /// <remarks>
    /// Retrieves seller profile information. Requires seller_id claim in JWT token.
    /// 
    /// **Authorization:**
    /// - Only the seller owner can view sensitive fields (Email, BankAccountInfo)
    /// - Other authenticated users see public fields only
    /// 
    /// **Success Example:**
    /// - **Request:** GET /api/sellers/550e8400-e29b-41d4-a716-446655440000
    /// - **Response (200 OK):** { "sellerId": "550e8400-e29b-41d4-a716-446655440000", "name": "Premium Electronics", "description": "High-quality electronics", "commissionRate": 0.15, "status": "Active", "createdAt": "2026-09-16T12:00:00Z", "email": "seller@example.com" (if owner) }
    /// 
    /// **Error Examples:**
    /// - Seller not found → **404 Not Found**: { "error": "Seller not found" }
    /// - Unauthorized access to private data → **403 Forbidden** (if requesting private fields without authorization)
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SellerResponse))]
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
    /// Update seller profile (Authenticated - Seller only)
    /// </summary>
    /// <remarks>
    /// Updates seller profile information. Only the seller owner can update their profile.
    /// 
    /// **Authorization:** Requires seller_id claim matching the {id} parameter
    /// 
    /// **Updatable Fields:**
    /// - Name, Description, PhoneNumber, BankAccountInfo
    /// 
    /// **Non-updatable Fields:**
    /// - Email, CommissionRate (require admin intervention)
    /// 
    /// **Success Example:**
    /// - **Request:** PUT /api/sellers/550e8400-e29b-41d4-a716-446655440000
    /// - **Body:** { "name": "Premium Electronics Co.", "description": "Updated description", "phoneNumber": "+1-555-0123" }
    /// - **Response (200 OK):** { "sellerId": "550e8400-e29b-41d4-a716-446655440000", "name": "Premium Electronics Co.", "description": "Updated description", "phoneNumber": "+1-555-0123", "status": "Active", "updatedAt": "2026-09-16T13:00:00Z" }
    /// 
    /// **Error Examples:**
    /// - Unauthorized (different seller) → **403 Forbidden**: "You can only update your own profile"
    /// - Seller not found → **404 Not Found**: { "error": "Seller not found" }
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SellerResponse))]
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
    /// Get orders for a seller (Authenticated - Seller only)
    /// </summary>
    /// <remarks>
    /// Task 10.1: Retrieves all orders containing items sold by this seller.
    /// - Only the seller owner can view their orders
    /// - Returns order details with seller-specific line items and commission breakdown
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

        // Get all distinct order IDs for this seller's payouts
        var orderIds = await _context.SellerPayouts
            .Where(p => p.SellerId == id)
            .Select(p => p.OrderId)
            .Distinct()
            .OrderByDescending(o => o)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Build order responses by grouping payouts by order ID
        var orders = new List<object>();
        var allPayouts = await _context.SellerPayouts
            .Where(p => p.SellerId == id && orderIds.Contains(p.OrderId))
            .ToListAsync(cancellationToken);

        foreach (var orderId in orderIds)
        {
            var orderPayouts = allPayouts.Where(p => p.OrderId == orderId).ToList();
            if (!orderPayouts.Any())
                continue;

            // Group payouts by order to get totals
            var grossTotal = orderPayouts.Sum(p => p.GrossAmount);
            var commissionTotal = orderPayouts.Sum(p => p.CommissionAmount);
            var sellerTotal = orderPayouts.Sum(p => p.SellerAmount);

            var orderResponse = new
            {
                orderId = orderId,
                orderDate = orderPayouts.Min(p => p.CreatedAt),
                status = "Completed", // TODO: Get actual order status from Ordering.API when OrderItem has SellerId
                grossAmount = grossTotal,
                commissionAmount = commissionTotal,
                sellerAmount = sellerTotal,
                items = orderPayouts.Select(p => new
                {
                    orderLineItemId = p.OrderLineItemId,
                    grossAmount = p.GrossAmount,
                    commissionAmount = p.CommissionAmount,
                    sellerAmount = p.SellerAmount
                }).ToList()
            };

            orders.Add(orderResponse);
        }

        var totalOrders = await _context.SellerPayouts
            .Where(p => p.SellerId == id)
            .Select(p => p.OrderId)
            .Distinct()
            .CountAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} orders for seller {SellerId}", orders.Count, id);

        return Ok(new
        {
            orders = orders,
            page,
            pageSize,
            total = totalOrders
        });
    }

    /// <summary>
    /// Get seller payout ledger (Authenticated - Seller only)
    /// </summary>
    /// <remarks>
    /// Retrieves the payout ledger for a seller showing all earnings with status tracking.
    /// Includes filtering by status (Pending, Processed, Paid) and date range.
    /// 
    /// **Authorization:** Requires seller_id claim matching the {id} parameter
    /// 
    /// **Query Parameters:**
    /// - status: Optional (Pending|Processed|Paid) - filter by payout status
    /// - fromDate: Optional (ISO 8601) - earliest payout date to include
    /// - toDate: Optional (ISO 8601) - latest payout date to include
    /// - page: Optional (default 1) - page number for pagination
    /// - pageSize: Optional (default 20, max 100) - results per page
    /// 
    /// **Success Example:**
    /// - **Request:** GET /api/sellers/550e8400-e29b-41d4-a716-446655440000/payouts?status=Pending&page=1&pageSize=20
    /// - **Response (200 OK):** 
    /// ```
    /// {
    ///   "payouts": [
    ///     { "payoutId": "...", "orderId": "...", "grossAmount": 100.00, "commissionAmount": 15.00, "sellerAmount": 85.00, "status": "Pending", "createdAt": "2026-09-16T12:00:00Z" },
    ///     { "payoutId": "...", "orderId": "...", "grossAmount": 50.00, "commissionAmount": 7.50, "sellerAmount": 42.50, "status": "Pending", "createdAt": "2026-09-16T13:00:00Z" }
    ///   ],
    ///   "page": 1,
    ///   "pageSize": 20,
    ///   "total": 2
    /// }
    /// ```
    /// 
    /// **Error Examples:**
    /// - Unauthorized → **403 Forbidden**: "You can only view your own payouts"
    /// - Seller not found → **404 Not Found**: { "error": "Seller not found" }
    /// </remarks>
    [HttpGet("{id:guid}/payouts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetSellerPayouts(
        Guid id,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var seller = await _context.Sellers.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        if (seller == null)
            return NotFound(new { error = "Seller not found" });

        if (!IsSellerOwner(id))
            return Forbid("You can only view your own payouts");

        var query = _context.SellerPayouts.AsQueryable().Where(p => p.SellerId == id);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status.ToString() == status);

        if (fromDate.HasValue)
            query = query.Where(p => p.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.CreatedAt <= toDate.Value);

        var total = await query.CountAsync(cancellationToken);
        var payouts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} payouts for seller {SellerId}", payouts.Count, id);

        return Ok(new
        {
            payouts = payouts.Select(MapPayoutToResponse),
            page,
            pageSize,
            total
        });
    }

    /// <summary>
    /// Get seller payout summary (Authenticated - Seller only)
    /// </summary>
    /// <remarks>
    /// Retrieves summary statistics for a seller's earnings broken down by payout status.
    /// Shows totals for Pending, Processed, and Paid amounts.
    /// 
    /// **Authorization:** Requires seller_id claim matching the {id} parameter
    /// 
    /// **Success Example:**
    /// - **Request:** GET /api/sellers/550e8400-e29b-41d4-a716-446655440000/payouts/summary
    /// - **Response (200 OK):**
    /// ```
    /// {
    ///   "sellerId": "550e8400-e29b-41d4-a716-446655440000",
    ///   "totalEarned": 542.50,
    ///   "totalPending": 127.50,
    ///   "totalProcessed": 0.00,
    ///   "totalPaid": 415.00,
    ///   "pendingPayoutCount": 3,
    ///   "lastPaymentDate": "2026-09-15T10:30:00Z"
    /// }
    /// ```
    /// </remarks>
    [HttpGet("{id:guid}/payouts/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetSellerPayoutSummary(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var seller = await _context.Sellers.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        if (seller == null)
            return NotFound(new { error = "Seller not found" });

        if (!IsSellerOwner(id))
            return Forbid("You can only view your own payout summary");

        var payouts = await _context.SellerPayouts
            .Where(p => p.SellerId == id)
            .ToListAsync(cancellationToken);

        var summary = new
        {
            sellerId = id,
            totalEarned = payouts.Sum(p => p.SellerAmount),
            totalPending = payouts.Where(p => p.Status == SellerPayoutStatus.Pending).Sum(p => p.SellerAmount),
            totalProcessed = payouts.Where(p => p.Status == SellerPayoutStatus.Processed).Sum(p => p.SellerAmount),
            totalPaid = payouts.Where(p => p.Status == SellerPayoutStatus.Paid).Sum(p => p.SellerAmount),
            pendingPayoutCount = payouts.Count(p => p.Status == SellerPayoutStatus.Pending),
            lastPaymentDate = payouts.Where(p => p.Status == SellerPayoutStatus.Paid).OrderByDescending(p => p.PaidAt).FirstOrDefault()?.PaidAt
        };

        _logger.LogInformation("Retrieved payout summary for seller {SellerId}", id);
        return Ok(summary);
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

    private static object MapPayoutToResponse(SellerPayout payout)
    {
        return new
        {
            payoutId = payout.PayoutId,
            sellerId = payout.SellerId,
            orderId = payout.OrderId,
            lineItemId = payout.OrderLineItemId,
            grossAmount = payout.GrossAmount,
            commissionAmount = payout.CommissionAmount,
            sellerAmount = payout.SellerAmount,
            status = payout.Status.ToString(),
            createdAt = payout.CreatedAt,
            paidAt = payout.PaidAt
        };
    }
}

