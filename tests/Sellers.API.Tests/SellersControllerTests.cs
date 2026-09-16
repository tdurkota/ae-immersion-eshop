namespace eShop.Sellers.API.Tests;

[TestClass]
public class SellersControllerTests
{
    private readonly SellersContext _context;
    private readonly SellersController _controller;
    private readonly ILogger<SellersController> _logger;
    
    public SellersControllerTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<SellersContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SellersContext(options);
        _logger = Substitute.For<ILogger<SellersController>>();
        _controller = new SellersController(_context, _logger);
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
}
