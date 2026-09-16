# Identity.API - Seller Authentication Documentation

## Overview
Identity.API extends IdentityServer4/Duende.IdentityServer to support role-based seller authentication. This document describes the seller authentication flow and claims structure.

## Seller Roles

The system supports three user roles:
- **customer**: Regular customer users (default for users without SellerId)
- **seller**: Third-party sellers managing products
- **admin**: Administrative users (reserved for future use)

Role constants are defined in `Models/UserRoles.cs`:
```csharp
public static class UserRoles
{
    public const string Customer = "customer";
    public const string Seller = "seller";
    public const string Admin = "admin";
}
```

## Seller Authentication Flow

### 1. User Registration/Login
Users register or login through the standard OAuth2 flow:
- **Endpoint**: `POST /connect/token`
- **Grant Type**: `password` (Resource Owner Password Credentials)
- **Parameters**:
  ```
  grant_type=password
  username=seller@example.com
  password=<password>
  client_id=<client_id>
  client_secret=<client_secret>
  scope=openid profile
  ```

### 2. JWT Token Response
Upon successful authentication, the Identity API returns a JWT token containing seller role and seller_id claims.

#### Example JWT Payload for Seller User:
```json
{
  "sub": "user-id-uuid",
  "preferred_username": "seller@example.com",
  "name": "John",
  "last_name": "Doe",
  "email": "seller@example.com",
  "email_verified": true,
  "phone_number": "1234567890",
  "phone_number_verified": true,
  "role": "seller",
  "seller_id": "d4b5e2a1-4c8f-4e2a-8d9f-5c3e8a1b2d4f",
  "iat": 1694905200,
  "exp": 1694908800,
  "iss": "https://identity.example.com",
  "aud": "seller-api"
}
```

#### Example JWT Payload for Customer User:
```json
{
  "sub": "user-id-uuid",
  "preferred_username": "customer@example.com",
  "name": "Jane",
  "last_name": "Smith",
  "email": "customer@example.com",
  "email_verified": true,
  "phone_number": "0987654321",
  "phone_number_verified": true,
  "role": "customer",
  "iat": 1694905200,
  "exp": 1694908800,
  "iss": "https://identity.example.com",
  "aud": "customer-api"
}
```

## Token Claims

### Standard Claims
- `sub` (Subject): User ID (UUID)
- `preferred_username`: Username/email
- `email`: User email address
- `email_verified`: Boolean indicating email verification status
- `phone_number`: User phone number (if available)
- `phone_number_verified`: Boolean indicating phone verification status
- `name`: First name
- `last_name`: Last name
- `iat`: Issued at timestamp
- `exp`: Token expiration timestamp
- `iss`: Issuer (Identity API URL)
- `aud`: Audience (client/API scope)

### Seller-Specific Claims
- `role` (string): User role - "seller", "customer", or "admin"
  - Populated from `UserRoles` constants
  - Always present in JWT token
- `seller_id` (UUID string): Seller account ID
  - Only present if user has an associated seller account (SellerId is not null)
  - Used for authorization checks in seller-specific endpoints
  - Example value: `"d4b5e2a1-4c8f-4e2a-8d9f-5c3e8a1b2d4f"`

### User Profile Claims
- `card_number`: Encrypted credit card number
- `card_holder`: Cardholder name
- `card_security_number`: Card CVV (for payment processing)
- `card_expiration`: Card expiration date (MM/YY format)
- `address_street`: Street address
- `address_city`: City
- `address_state`: State/Province
- `address_country`: Country code
- `address_zip_code`: ZIP/Postal code

## Implementation Details

### ApplicationUser Model
The `ApplicationUser` class extends ASP.NET Core Identity's `IdentityUser` with a `SellerId` property:

```csharp
public class ApplicationUser : IdentityUser
{
    // ... existing properties ...
    
    /// <summary>
    /// Seller identity: nullable GUID to link user to seller account.
    /// NULL = customer user; UUID = seller user.
    /// </summary>
    public Guid? SellerId { get; set; }
}
```

### ProfileService - Claim Generation
The `ProfileService` class in `Services/ProfileService.cs` is responsible for generating JWT claims. The claim generation logic:

1. Always includes basic identity claims (subject, username, email, phone, etc.)
2. Checks if user has a `SellerId`:
   - **If seller**: Adds `role: "seller"` and `seller_id: <UUID>` claims
   - **If not seller**: Adds `role: "customer"` claim (no seller_id)
3. Returns complete claim set to IdentityServer for JWT token signing

### Database Migration
A migration file `20260916191410_AddSellerIdToApplicationUser.cs` adds the `SellerId` column to the `AspNetUsers` table:
- Type: `uuid` (PostgreSQL GUID)
- Nullable: Yes
- Existing users: NULL (treated as customers)
- Indexed: No (can be added later if needed)

## Authorization Pattern

Seller-specific endpoints use role-based authorization:

```csharp
// In Sellers.API or other protected services
[Authorize(Roles = "seller")]
public class SellerController : ControllerBase
{
    [HttpGet("{id}/products")]
    public async Task<IActionResult> GetSellerProducts(Guid id)
    {
        // Extract seller_id claim from JWT
        var sellerIdClaim = User.FindFirst("seller_id")?.Value;
        if (sellerIdClaim != id.ToString())
        {
            return Forbid(); // Seller can only access their own data
        }
        
        // Return seller's products
    }
}
```

## Example cURL Request

### Login as Seller
```bash
curl -X POST https://identity.example.com/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "username=seller@example.com" \
  -d "password=SecurePassword123!" \
  -d "client_id=sellers-api" \
  -d "client_secret=<client-secret>" \
  -d "scope=openid profile orders"
```

### Response
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6I...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "openid profile orders"
}
```

### Using Token with Seller API
```bash
curl -H "Authorization: Bearer <access_token>" \
  https://sellers.example.com/api/sellers/d4b5e2a1-4c8f-4e2a-8d9f-5c3e8a1b2d4f/products
```

## Testing

### Unit Tests
Run seller authentication unit tests:
```bash
dotnet test tests/Application.UnitTests/Application.UnitTests.csproj --filter "SellerAuthentication"
```

Test coverage:
- UserRoles constants (seller, customer, admin)
- ApplicationUser model with SellerId property
- ApplicationUser serialization with and without SellerId

### Integration Testing (Future)
Integration tests for the full authentication flow will verify:
- Seller login generates correct JWT
- JWT token includes seller role and seller_id claims
- Customer login generates token without seller_id claim
- Invalid login returns 401 Unauthorized

## Security Considerations

1. **Role Claims**: Role claims are generated server-side in ProfileService, not user-modifiable
2. **SellerId Binding**: SellerId is bound to specific user account in database; cannot be forged
3. **Token Signing**: All tokens are cryptographically signed by IdentityServer
4. **HTTPS Only**: All endpoints must use HTTPS in production
5. **Token Expiration**: Tokens expire after configured duration (default 1 hour); refresh tokens available for renewal

## Related Services

- **Sellers.API**: Uses seller role and seller_id claims for seller account management
- **Catalog.API**: Uses seller_id claim to filter products by seller
- **Ordering.API**: Uses seller_id claim for order routing and commission tracking

## Database Schema

### AspNetUsers Table Changes
```sql
ALTER TABLE "AspNetUsers" ADD COLUMN "SellerId" uuid NULL;
```

No indexes added at baseline; can be added in future if seller_id filtering becomes a performance bottleneck.
