namespace eShop.Sellers.API.Tests;

[TestClass]
public class SellersControllerTests
{
    private readonly SellersContext _context;
    private readonly SellersController _controller;
    private readonly ILogger<SellersController> _logger;
    private readonly ISellerPayoutRepository _payoutRepository;
    
    public SellersControllerTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<SellersContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SellersContext(options);
        _logger = Substitute.For<ILogger<SellersController>>();
        _payoutRepository = new SellerPayoutRepository(_context);
        _controller = new SellersController(_context, _payoutRepository, _logger);
    }

    #region CreateSeller Tests

    [TestMethod]
    [Description("Task 4.1: POST /api/sellers - Should create seller and return 201 Created")]
    public async Task CreateSeller_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateSellerRequest(
            Name: "Test Seller",
            Email: "test@example.com",
            Description: "A test seller",
            PhoneNumber: "555-1234",
            BankAccountInfo: "ACC123456",
            CommissionRate: 0.15m);

        // Act
        var result = await _controller.CreateSeller(request, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        var createdResult = Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
        Assert.IsNotNull(createdResult);
        Assert.AreEqual(nameof(SellersController.GetSeller), createdResult.ActionName);
        
        var returnedSeller = Assert.IsInstanceOfType<SellerResponse>(createdResult.Value);
        Assert.AreEqual("Test Seller", returnedSeller.Name);
        Assert.AreEqual("test@example.com", returnedSeller.Email);
        Assert.AreEqual(0.15m, returnedSeller.CommissionRate);

        // Verify seller was saved to database
        var savedSeller = await _context.Sellers.FirstOrDefaultAsync(s => s.SellerId == returnedSeller.SellerId);
        Assert.IsNotNull(savedSeller);
        Assert.AreEqual("Test Seller", savedSeller.Name);
        Assert.AreEqual("test@example.com", savedSeller.Email);
    }

    [TestMethod]
    [Description("Task 4.1: POST /api/sellers - Should return 400 when Name is missing")]
    public async Task CreateSeller_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSellerRequest(
            Name: string.Empty,
            Email: "test@example.com");

        // Act
        var result = await _controller.CreateSeller(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        Assert.IsNotNull(badRequestResult);
    }

    [TestMethod]
    [Description("Task 4.1: POST /api/sellers - Should return 400 when Email is missing")]
    public async Task CreateSeller_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSellerRequest(
            Name: "Test Seller",
            Email: string.Empty);

        // Act
        var result = await _controller.CreateSeller(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        Assert.IsNotNull(badRequestResult);
    }

    [TestMethod]
    [Description("Task 4.1: POST /api/sellers - Should return 400 when Email format is invalid")]
    public async Task CreateSeller_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSellerRequest(
            Name: "Test Seller",
            Email: "invalid-email");

        // Act
        var result = await _controller.CreateSeller(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        Assert.IsNotNull(badRequestResult);
    }

    [TestMethod]
    [Description("Task 4.1: POST /api/sellers - Should return 409 when Email already exists")]
    public async Task CreateSeller_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var request1 = new CreateSellerRequest(
            Name: "Seller 1",
            Email: "duplicate@example.com");
        var request2 = new CreateSellerRequest(
            Name: "Seller 2",
            Email: "duplicate@example.com");

        // Act - Create first seller
        await _controller.CreateSeller(request1, CancellationToken.None);
        
        // Act - Try to create second seller with same email
        var result = await _controller.CreateSeller(request2, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
        Assert.IsNotNull(conflictResult);
    }

    [TestMethod]
    [Description("Task 4.1: POST /api/sellers - Should return 400 when CommissionRate is invalid")]
    public async Task CreateSeller_WithInvalidCommissionRate_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSellerRequest(
            Name: "Test Seller",
            Email: "test@example.com",
            CommissionRate: 1.5m); // Invalid: > 1

        // Act
        var result = await _controller.CreateSeller(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        Assert.IsNotNull(badRequestResult);
    }

    [TestMethod]
    [Description("Task 4.1: POST /api/sellers - Should normalize email to lowercase")]
    public async Task CreateSeller_WithMixedCaseEmail_NormalizesEmail()
    {
        // Arrange
        var request = new CreateSellerRequest(
            Name: "Test Seller",
            Email: "Test@EXAMPLE.COM");

        // Act
        var result = await _controller.CreateSeller(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
        var returnedSeller = Assert.IsInstanceOfType<SellerResponse>(createdResult.Value);
        Assert.AreEqual("test@example.com", returnedSeller.Email);
    }

    #endregion

    #region GetSeller Tests

    [TestMethod]
    [Description("Task 4.2: GET /api/sellers/{id} - Should return seller data")]
    public async Task GetSeller_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var seller = new Seller
        {
            SellerId = Guid.NewGuid(),
            Name = "Test Seller",
            Email = "test@example.com",
            CommissionRate = 0.15m
        };
        _context.Sellers.Add(seller);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSeller(seller.SellerId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        Assert.IsNotNull(okResult);
        var returnedSeller = Assert.IsInstanceOfType<SellerResponse>(okResult.Value);
        Assert.AreEqual("Test Seller", returnedSeller.Name);
    }

    [TestMethod]
    [Description("Task 4.2: GET /api/sellers/{id} - Should return 404 when seller not found")]
    public async Task GetSeller_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _controller.GetSeller(nonExistentId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        Assert.IsNotNull(notFoundResult);
    }

    [TestMethod]
    [Description("Task 4.2: GET /api/sellers/{id} - Should not include private fields for non-owner")]
    public async Task GetSeller_ForNonOwner_ExcludesPrivateFields()
    {
        // Arrange
        var seller = new Seller
        {
            SellerId = Guid.NewGuid(),
            Name = "Test Seller",
            Email = "test@example.com",
            BankAccountInfo = "ACC123456",
            CommissionRate = 0.15m
        };
        _context.Sellers.Add(seller);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSeller(seller.SellerId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var returnedSeller = Assert.IsInstanceOfType<SellerResponse>(okResult.Value);
        Assert.IsNull(returnedSeller.Email);
        Assert.IsNull(returnedSeller.BankAccountInfo);
    }

    #endregion

    #region UpdateSeller Tests

    [TestMethod]
    [Description("Task 4.3: PUT /api/sellers/{id} - Should return 404 when seller not found")]
    public async Task UpdateSeller_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateSellerRequest(Name: "Updated Name");

        // Act
        var result = await _controller.UpdateSeller(Guid.NewGuid(), request, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        Assert.IsNotNull(notFoundResult);
    }

    [TestMethod]
    [Description("Task 4.3: PUT /api/sellers/{id} - Should return 403 when not authorized (no seller_id claim)")]
    public async Task UpdateSeller_WithoutAuthorization_ReturnsForbidden()
    {
        // Arrange
        var seller = new Seller
        {
            SellerId = Guid.NewGuid(),
            Name = "Test Seller",
            Email = "test@example.com",
            CommissionRate = 0.15m
        };
        _context.Sellers.Add(seller);
        await _context.SaveChangesAsync();

        var request = new UpdateSellerRequest(Name: "Updated Name");

        // Act - Controller has no seller_id claim in User context
        var result = await _controller.UpdateSeller(seller.SellerId, request, CancellationToken.None);

        // Assert
        var forbidResult = Assert.IsInstanceOfType<ForbidResult>(result.Result);
        Assert.IsNotNull(forbidResult);
    }

    [TestMethod]
    [Description("Task 4.3: PUT /api/sellers/{id} - Should return 403 when seller_id mismatch")]
    public async Task UpdateSeller_WithWrongSellerId_ReturnsForbidden()
    {
        // Arrange
        var seller = new Seller
        {
            SellerId = Guid.NewGuid(),
            Name = "Test Seller",
            Email = "test@example.com",
            CommissionRate = 0.15m
        };
        _context.Sellers.Add(seller);
        await _context.SaveChangesAsync();

        var request = new UpdateSellerRequest(Name: "Updated Name");

        // Simulate different seller_id in claim
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("seller_id", Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.UpdateSeller(seller.SellerId, request, CancellationToken.None);

        // Assert
        var forbidResult = Assert.IsInstanceOfType<ForbidResult>(result.Result);
        Assert.IsNotNull(forbidResult);
    }

    [TestMethod]
    [Description("Task 4.3: PUT /api/sellers/{id} - Should update seller profile successfully")]
    public async Task UpdateSeller_WithValidRequest_UpdatesSuccessfully()
    {
        // Arrange
        var sellerId = Guid.NewGuid();
        var seller = new Seller
        {
            SellerId = sellerId,
            Name = "Original Name",
            Email = "test@example.com",
            PhoneNumber = "555-1111",
            CommissionRate = 0.15m
        };
        _context.Sellers.Add(seller);
        await _context.SaveChangesAsync();

        var request = new UpdateSellerRequest(
            Name: "Updated Name",
            PhoneNumber: "555-2222");

        // Setup authorization claim
        var claims = new List<Claim>
        {
            new("seller_id", sellerId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.UpdateSeller(sellerId, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var returnedSeller = Assert.IsInstanceOfType<SellerResponse>(okResult.Value);
        Assert.AreEqual("Updated Name", returnedSeller.Name);
        Assert.AreEqual("555-2222", returnedSeller.PhoneNumber);

        // Verify changes persisted
        var updatedSeller = await _context.Sellers.FindAsync(sellerId);
        Assert.AreEqual("Updated Name", updatedSeller.Name);
        Assert.AreEqual("555-2222", updatedSeller.PhoneNumber);
    }

    [TestMethod]
    [Description("Task 4.3: PUT /api/sellers/{id} - Should only update specified fields")]
    public async Task UpdateSeller_WithPartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var sellerId = Guid.NewGuid();
        var seller = new Seller
        {
            SellerId = sellerId,
            Name = "Original Name",
            Email = "test@example.com",
            Description = "Original Description",
            PhoneNumber = "555-1111",
            CommissionRate = 0.15m
        };
        _context.Sellers.Add(seller);
        await _context.SaveChangesAsync();

        // Only update Name
        var request = new UpdateSellerRequest(Name: "Updated Name");

        // Setup authorization
        var claims = new List<Claim> { new("seller_id", sellerId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        await _controller.UpdateSeller(sellerId, request, CancellationToken.None);

        // Assert - Other fields should remain unchanged
        var updatedSeller = await _context.Sellers.FindAsync(sellerId);
        Assert.AreEqual("Updated Name", updatedSeller.Name);
        Assert.AreEqual("Original Description", updatedSeller.Description);
        Assert.AreEqual("555-1111", updatedSeller.PhoneNumber);
    }

    #endregion

    #region Validation Tests

    [TestMethod]
    [Description("Task 4.1: Should handle commission rate edge cases")]
    public async Task CreateSeller_WithEdgeCaseCommissionRates_ValidatesCorrectly()
    {
        // Test 0 (valid)
        var request0 = new CreateSellerRequest(
            Name: "Test0",
            Email: "test0@example.com",
            CommissionRate: 0m);
        var result0 = await _controller.CreateSeller(request0, CancellationToken.None);
        Assert.IsInstanceOfType<CreatedAtActionResult>(result0.Result);

        // Test 1 (valid)
        var request1 = new CreateSellerRequest(
            Name: "Test1",
            Email: "test1@example.com",
            CommissionRate: 1m);
        var result1 = await _controller.CreateSeller(request1, CancellationToken.None);
        Assert.IsInstanceOfType<CreatedAtActionResult>(result1.Result);

        // Test -0.01 (invalid)
        var requestNegative = new CreateSellerRequest(
            Name: "TestNeg",
            Email: "testneg@example.com",
            CommissionRate: -0.01m);
        var resultNegative = await _controller.CreateSeller(requestNegative, CancellationToken.None);
        Assert.IsInstanceOfType<BadRequestObjectResult>(resultNegative.Result);

        // Test 1.01 (invalid)
        var requestAboveOne = new CreateSellerRequest(
            Name: "TestAbove",
            Email: "testabove@example.com",
            CommissionRate: 1.01m);
        var resultAboveOne = await _controller.CreateSeller(requestAboveOne, CancellationToken.None);
        Assert.IsInstanceOfType<BadRequestObjectResult>(resultAboveOne.Result);
    }

    [TestMethod]
    [Description("Task 4.1: Should trim and normalize seller data")]
    public async Task CreateSeller_WithWhitespace_NormalizesData()
    {
        // Arrange
        var request = new CreateSellerRequest(
            Name: "  Test Seller  ",
            Email: "  test@example.com  ",
            Description: "  A test seller  ");

        // Act
        var result = await _controller.CreateSeller(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
        var returnedSeller = Assert.IsInstanceOfType<SellerResponse>(createdResult.Value);
        Assert.AreEqual("Test Seller", returnedSeller.Name);
        Assert.AreEqual("test@example.com", returnedSeller.Email);
        Assert.AreEqual("A test seller", returnedSeller.Description);
    }

    #endregion

    #region GetSellerOrders Tests (Task 10.1)

    [TestMethod]
    [Description("Task 10.1: GET /api/sellers/{id}/orders - Seller can view their orders")]
    public async Task GetSellerOrders_WithValidSeller_ReturnsOrdersList()
    {
        // Arrange
        var sellerId = Guid.NewGuid();
        var seller = new Seller
        {
            SellerId = sellerId,
            Name = "Test Seller",
            Email = "test@example.com",
            CommissionRate = 0.15m,
            Status = SellerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var payout1 = new SellerPayout
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

        var payout2 = new SellerPayout
        {
            PayoutId = Guid.NewGuid(),
            SellerId = sellerId,
            OrderId = 1,
            OrderLineItemId = 2,
            GrossAmount = 50m,
            CommissionAmount = 7.5m,
            SellerAmount = 42.5m,
            Status = SellerPayoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sellers.Add(seller);
        _context.SellerPayouts.AddRange(payout1, payout2);
        await _context.SaveChangesAsync();

        // Mock seller authentication
        var claims = new[] { new Claim("seller_id", sellerId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = principal };

        // Act
        var result = await _controller.GetSellerOrders(sellerId, 1, 20, CancellationToken.None);

        // Assert
        var okResult = Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        Assert.IsNotNull(okResult.Value);
        
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        Assert.IsNotNull(root);
        Assert.AreEqual(1, root.GetProperty("orders").GetArrayLength());
        Assert.AreEqual(1, root.GetProperty("total").GetInt32());
    }

    [TestMethod]
    [Description("Task 10.1: GET /api/sellers/{id}/orders - Unauthorized access returns 403")]
    public async Task GetSellerOrders_UnauthorizedSeller_ReturnsForbidden()
    {
        // Arrange
        var sellerId1 = Guid.NewGuid();
        var sellerId2 = Guid.NewGuid();
        
        var seller = new Seller
        {
            SellerId = sellerId1,
            Name = "Test Seller",
            Email = "test@example.com",
            CommissionRate = 0.15m,
            Status = SellerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Sellers.Add(seller);
        await _context.SaveChangesAsync();

        // Mock seller authentication for different seller
        var claims = new[] { new Claim("seller_id", sellerId2.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = principal };

        // Act
        var result = await _controller.GetSellerOrders(sellerId1, 1, 20, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<ForbidResult>(result.Result);
    }

    [TestMethod]
    [Description("Task 10.1: GET /api/sellers/{id}/orders - Seller only sees their own orders")]
    public async Task GetSellerOrders_DataIsolation_SellerSeesOnlyOwnOrders()
    {
        // Arrange
        var seller1Id = Guid.NewGuid();
        var seller2Id = Guid.NewGuid();

        var seller1 = new Seller
        {
            SellerId = seller1Id,
            Name = "Seller 1",
            Email = "seller1@example.com",
            CommissionRate = 0.15m,
            Status = SellerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var seller2 = new Seller
        {
            SellerId = seller2Id,
            Name = "Seller 2",
            Email = "seller2@example.com",
            CommissionRate = 0.15m,
            Status = SellerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var payout1 = new SellerPayout
        {
            PayoutId = Guid.NewGuid(),
            SellerId = seller1Id,
            OrderId = 1,
            OrderLineItemId = 1,
            GrossAmount = 100m,
            CommissionAmount = 15m,
            SellerAmount = 85m,
            Status = SellerPayoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var payout2 = new SellerPayout
        {
            PayoutId = Guid.NewGuid(),
            SellerId = seller2Id,
            OrderId = 2,
            OrderLineItemId = 1,
            GrossAmount = 50m,
            CommissionAmount = 7.5m,
            SellerAmount = 42.5m,
            Status = SellerPayoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sellers.AddRange(seller1, seller2);
        _context.SellerPayouts.AddRange(payout1, payout2);
        await _context.SaveChangesAsync();

        // Mock seller1 authentication
        var claims = new[] { new Claim("seller_id", seller1Id.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = principal };

        // Act
        var result = await _controller.GetSellerOrders(seller1Id, 1, 20, CancellationToken.None);

        // Assert
        var okResult = Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.AreEqual(1, root.GetProperty("total").GetInt32()); // Only seller1's order
    }

    #endregion

    #region GetSellerPayouts Tests (Task 10.2)

    [TestMethod]
    [Description("Task 10.2: GET /api/sellers/{id}/payouts - Seller can view their payouts")]
    public async Task GetSellerPayouts_WithValidSeller_ReturnsPayoutsList()
    {
        // Arrange
        var sellerId = Guid.NewGuid();
        var seller = new Seller
        {
            SellerId = sellerId,
            Name = "Test Seller",
            Email = "test@example.com",
            CommissionRate = 0.15m,
            Status = SellerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

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

        _context.Sellers.Add(seller);
        _context.SellerPayouts.Add(payout);
        await _context.SaveChangesAsync();

        // Mock seller authentication
        var claims = new[] { new Claim("seller_id", sellerId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = principal };

        // Act
        var result = await _controller.GetSellerPayouts(sellerId, null, null, null, 1, 20, CancellationToken.None);

        // Assert
        var okResult = Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.AreEqual(1, root.GetProperty("payouts").GetArrayLength());
        Assert.AreEqual(1, root.GetProperty("total").GetInt32());
    }

    [TestMethod]
    [Description("Task 10.2: GET /api/sellers/{id}/payouts - Filter by status works correctly")]
    public async Task GetSellerPayouts_FilterByStatus_ReturnsFilteredPayouts()
    {
        // Arrange
        var sellerId = Guid.NewGuid();
        var seller = new Seller
        {
            SellerId = sellerId,
            Name = "Test Seller",
            Email = "test@example.com",
            CommissionRate = 0.15m,
            Status = SellerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var payout1 = new SellerPayout
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

        var payout2 = new SellerPayout
        {
            PayoutId = Guid.NewGuid(),
            SellerId = sellerId,
            OrderId = 2,
            OrderLineItemId = 1,
            GrossAmount = 50m,
            CommissionAmount = 7.5m,
            SellerAmount = 42.5m,
            Status = SellerPayoutStatus.Paid,
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        };

        _context.Sellers.Add(seller);
        _context.SellerPayouts.AddRange(payout1, payout2);
        await _context.SaveChangesAsync();

        // Mock seller authentication
        var claims = new[] { new Claim("seller_id", sellerId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = principal };

        // Act
        var result = await _controller.GetSellerPayouts(sellerId, "Paid", null, null, 1, 20, CancellationToken.None);

        // Assert
        var okResult = Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.AreEqual(1, root.GetProperty("payouts").GetArrayLength());
        Assert.AreEqual(1, root.GetProperty("total").GetInt32());
    }

    [TestMethod]
    [Description("Task 10.2: GET /api/sellers/{id}/payouts - Filter by date range works correctly")]
    public async Task GetSellerPayouts_FilterByDateRange_ReturnsFilteredPayouts()
    {
        // Arrange
        var sellerId = Guid.NewGuid();
        var seller = new Seller
        {
            SellerId = sellerId,
            Name = "Test Seller",
            Email = "test@example.com",
            CommissionRate = 0.15m,
            Status = SellerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var today = DateTime.UtcNow.Date;
        var payout1 = new SellerPayout
        {
            PayoutId = Guid.NewGuid(),
            SellerId = sellerId,
            OrderId = 1,
            OrderLineItemId = 1,
            GrossAmount = 100m,
            CommissionAmount = 15m,
            SellerAmount = 85m,
            Status = SellerPayoutStatus.Pending,
            CreatedAt = today.AddDays(-1)
        };

        var payout2 = new SellerPayout
        {
            PayoutId = Guid.NewGuid(),
            SellerId = sellerId,
            OrderId = 2,
            OrderLineItemId = 1,
            GrossAmount = 50m,
            CommissionAmount = 7.5m,
            SellerAmount = 42.5m,
            Status = SellerPayoutStatus.Pending,
            CreatedAt = today.AddDays(1)
        };

        _context.Sellers.Add(seller);
        _context.SellerPayouts.AddRange(payout1, payout2);
        await _context.SaveChangesAsync();

        // Mock seller authentication
        var claims = new[] { new Claim("seller_id", sellerId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = principal };

        // Act - Filter for today
        var result = await _controller.GetSellerPayouts(sellerId, null, today, today.AddDays(1), 1, 20, CancellationToken.None);

        // Assert
        var okResult = Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.AreEqual(1, root.GetProperty("payouts").GetArrayLength());
    }

    [TestMethod]
    [Description("Task 10.2: GET /api/sellers/{id}/payouts - Unauthorized access returns 403")]
    public async Task GetSellerPayouts_UnauthorizedSeller_ReturnsForbidden()
    {
        // Arrange
        var sellerId1 = Guid.NewGuid();
        var sellerId2 = Guid.NewGuid();

        var seller = new Seller
        {
            SellerId = sellerId1,
            Name = "Test Seller",
            Email = "test@example.com",
            CommissionRate = 0.15m,
            Status = SellerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Sellers.Add(seller);
        await _context.SaveChangesAsync();

        // Mock seller authentication for different seller
        var claims = new[] { new Claim("seller_id", sellerId2.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = principal };

        // Act
        var result = await _controller.GetSellerPayouts(sellerId1, null, null, null, 1, 20, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<ForbidResult>(result.Result);
    }

    #endregion

    #region GetSellerPayoutSummary Tests (Task 10.3)

    [TestMethod]
    [Description("Task 10.3: GET /api/sellers/{id}/payouts/summary - Returns correct totals")]
    public async Task GetSellerPayoutSummary_WithMultiplePayouts_CalculatesTotalsCorrectly()
    {
        // Arrange
        var sellerId = Guid.NewGuid();
        var seller = new Seller
        {
            SellerId = sellerId,
            Name = "Test Seller",
            Email = "test@example.com",
            CommissionRate = 0.15m,
            Status = SellerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var payout1 = new SellerPayout
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

        var payout2 = new SellerPayout
        {
            PayoutId = Guid.NewGuid(),
            SellerId = sellerId,
            OrderId = 2,
            OrderLineItemId = 1,
            GrossAmount = 100m,
            CommissionAmount = 15m,
            SellerAmount = 85m,
            Status = SellerPayoutStatus.Processed,
            CreatedAt = DateTime.UtcNow
        };

        var payout3 = new SellerPayout
        {
            PayoutId = Guid.NewGuid(),
            SellerId = sellerId,
            OrderId = 3,
            OrderLineItemId = 1,
            GrossAmount = 100m,
            CommissionAmount = 15m,
            SellerAmount = 85m,
            Status = SellerPayoutStatus.Paid,
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        };

        _context.Sellers.Add(seller);
        _context.SellerPayouts.AddRange(payout1, payout2, payout3);
        await _context.SaveChangesAsync();

        // Mock seller authentication
        var claims = new[] { new Claim("seller_id", sellerId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = principal };

        // Act
        var result = await _controller.GetSellerPayoutSummary(sellerId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var json = JsonSerializer.Serialize(okResult.Value);
        var returnValue = JsonDocument.Parse(json);
        
        Assert.AreEqual(255m, (decimal)returnValue["totalEarned"]);     // 85 + 85 + 85
        Assert.AreEqual(85m, (decimal)returnValue["totalPending"]);     // 85
        Assert.AreEqual(85m, (decimal)returnValue["totalProcessed"]);   // 85
        Assert.AreEqual(85m, (decimal)returnValue["totalPaid"]);        // 85
    }

    [TestMethod]
    [Description("Task 10.3: GET /api/sellers/{id}/payouts/summary - Unauthorized access returns 403")]
    public async Task GetSellerPayoutSummary_UnauthorizedSeller_ReturnsForbidden()
    {
        // Arrange
        var sellerId1 = Guid.NewGuid();
        var sellerId2 = Guid.NewGuid();

        var seller = new Seller
        {
            SellerId = sellerId1,
            Name = "Test Seller",
            Email = "test@example.com",
            CommissionRate = 0.15m,
            Status = SellerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Sellers.Add(seller);
        await _context.SaveChangesAsync();

        // Mock seller authentication for different seller
        var claims = new[] { new Claim("seller_id", sellerId2.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = principal };

        // Act
        var result = await _controller.GetSellerPayoutSummary(sellerId1, CancellationToken.None);

        // Assert
        Assert.IsInstanceOfType<ForbidResult>(result.Result);
    }

    #endregion
}
