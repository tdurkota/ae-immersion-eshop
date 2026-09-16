namespace eShop.Sellers.API.Controllers;

[ApiController]
[Route("api/sellers")]
public class SellersController(SellersContext context, ILogger<SellersController> logger) : ControllerBase
{
    private readonly SellersContext _context = context;
    private readonly ILogger<SellersController> _logger = logger;

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
