# Authorization Flow

This diagram captures seller-scoped authorization using JWT claims, ownership checks, and an administrator bypass.

```mermaid
flowchart TD
    Request[Authenticated request<br/>update seller resource]
    Jwt[JWT bearer token]
    Claims[Claims extracted<br/>role, seller_id, sub]
    RoleCheck{Role present?}
    AdminCheck{Admin role?}
    SellerCheck{Seller role?}
    ResourceLoad[Load target seller_id<br/>from route/body/entity]
    MatchCheck{JWT seller_id<br/>matches target seller_id?}
    AllowSeller[Allow seller-scoped access]
    AllowAdmin[Allow admin bypass]
    DenyRole[403 Forbidden<br/>missing marketplace role]
    DenyMismatch[403 Forbidden<br/>seller_id mismatch]

    subgraph Hierarchy[Role hierarchy]
        AdminRole[Admin]
        SellerRole[Seller]
        BuyerRole[Buyer / User]
        AdminRole -->|can manage any seller resource| SellerRole
        SellerRole -->|can manage owned resources only| BuyerRole
    end

    Request --> Jwt --> Claims --> RoleCheck
    RoleCheck -->|No| DenyRole
    RoleCheck -->|Yes| AdminCheck
    AdminCheck -->|Yes| AllowAdmin
    AdminCheck -->|No| SellerCheck
    SellerCheck -->|No| DenyRole
    SellerCheck -->|Yes| ResourceLoad --> MatchCheck
    MatchCheck -->|Yes| AllowSeller
    MatchCheck -->|No| DenyMismatch

    Claims -. seller_id claim .-> MatchCheck
    AdminRole -. bypass .-> AllowAdmin
    SellerRole -. ownership required .-> AllowSeller
```

## Authorization Rules

- Seller tokens must include a `seller_id` claim and the `Seller` role.
- Requests targeting seller-owned resources compare the JWT `seller_id` with the route, payload, or loaded entity owner.
- If the seller IDs do not match, the request is rejected with `403 Forbidden`.
- `Admin` users bypass seller ownership checks and can operate on any seller resource.
