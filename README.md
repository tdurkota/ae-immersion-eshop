# eShop Reference Application - "AdventureWorks"

A reference .NET application implementing an e-commerce website using a services-based architecture with [Aspire](https://aspire.dev/).

![eShop Reference Application architecture diagram](img/eshop_architecture.png)

![eShop homepage screenshot](img/eshop_homepage.png)

## Getting Started

This version of eShop is based on .NET 10.

Previous eShop versions:

* [.NET 8](https://github.com/dotnet/eShop/tree/release/8.0)

### Prerequisites

1. Install a [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) that satisfies [`global.json`](global.json).
2. Install the [Aspire CLI](https://aspire.dev/get-started/install-cli/) and verify that it is available:

    ```console
    aspire --version
    ```

3. Install and start an OCI-compatible container runtime. [Docker Desktop](https://www.docker.com/products/docker-desktop/) is the recommended default. [Podman](https://podman.io/docs/installation) is also supported; follow the [Aspire prerequisites](https://aspire.dev/get-started/prerequisites/) to configure it.
4. Clone the repository:

    ```console
    git clone https://github.com/dotnet/eShop.git
    cd eShop
    ```

No separate Aspire workload or Visual Studio component is required; the AppHost SDK and hosting integrations are referenced by the projects in this repository.

#### Optional IDE setup

- [Visual Studio](https://visualstudio.microsoft.com/vs/) with the `ASP.NET and web development` workload.
- [Visual Studio Code with C# Dev Kit](https://code.visualstudio.com/docs/csharp/get-started) and the [Aspire extension](https://aspire.dev/get-started/aspire-vscode-extension/).
- The [.NET MAUI workload](https://learn.microsoft.com/dotnet/maui/get-started/installation) if you want to run the client apps.

### Running the solution

> [!WARNING]
> Ensure that your container runtime is running before starting eShop.

#### From the terminal

From the repository root, run:

```console
aspire run
```

The root [`aspire.config.json`](aspire.config.json) selects `src/eShop.AppHost/eShop.AppHost.csproj`, avoiding ambiguity with the test AppHosts in the repository. When startup completes, the CLI prints a dashboard URL similar to:

```text
Dashboard: https://localhost:<port>/login?t=<token>
```

Press <kbd>Ctrl</kbd>+<kbd>C</kbd> to stop the AppHost. See the [`aspire run` command](https://aspire.dev/reference/cli/commands/aspire-run/) for additional options.

To run the AppHost in the background instead:

```console
aspire start
aspire ps
```

When you are finished, run `aspire stop`. See the [`aspire start` command](https://aspire.dev/reference/cli/commands/aspire-start/) for details.

#### From Visual Studio

1. Open `eShop.Web.slnf`.
2. Set `src/eShop.AppHost/eShop.AppHost.csproj` as the startup project.
3. Press <kbd>Ctrl</kbd>+<kbd>F5</kbd> to start eShop and open the Aspire dashboard.

### Running tests

Run the server tests:

```powershell
dotnet test --solution eShop.Web.slnf
```

Run the Playwright browser journeys. Playwright starts the AppHost automatically, so ensure your container runtime is running first.

First, create a `.env` file from the template:
```powershell
cp .env.example .env
```

Then update `.env` with actual test credentials. Default test users seeded by the Identity API:
- **Username:** `alice` or `bob`
- **Password:** `Pass123$`
- See [src/Identity.API/UsersSeed.cs](src/Identity.API/UsersSeed.cs) for seed configuration

Run E2E tests:
```powershell
npm ci
npx playwright install chromium
npm run test:e2e          # headless
npm run test:e2e:headed   # with visible browser
npm run test:e2e:report   # view HTML report
```

To test against a custom URL (e.g., remote deployment):
```powershell
PLAYWRIGHT_BASE_URL=https://localhost:7298 npm run test:e2e
```

### Optional: AI Chatbot with Microsoft Foundry

This option provisions a Microsoft Foundry resource during local development, so first authenticate to Azure and configure the subscription and location:

```powershell
az login
aspire secret set "Azure:SubscriptionId" "<subscription-id>"
aspire secret set "Azure:Location" "eastus"
```

Then enable Foundry and start eShop:

```powershell
$env:UseFoundry = "true"
aspire run
```

Aspire provisions the `gpt-4.1-mini` and `text-embedding-3-small` deployments and injects their connection information into the consuming projects. The Foundry hosting integration currently uses a preview package. See [local Azure provisioning](https://aspire.dev/integrations/cloud/azure/local-provisioning/) and the [Microsoft Foundry hosting integration](https://aspire.dev/integrations/cloud/azure/azure-ai-foundry/azure-ai-foundry-host/) for details.

### Deploy to Azure Container Apps

The AppHost is already configured with an Azure Container Apps environment, so the Aspire CLI can deploy directly from the application model. See the [Aspire Azure Container Apps deployment guide](https://aspire.dev/deployment/azure/container-apps/) for details.

> [!WARNING]
> This sample deploys PostgreSQL, Redis, and RabbitMQ as containers in Azure Container Apps. This configuration is intended for evaluation and demonstrations, not production data.

Prerequisites:

- The prerequisites listed above, including a running container runtime.
- The [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli), an active Azure subscription, and permission to create resources.

Sign in, optionally preview the deployment pipeline, and deploy:

```console
az login
aspire deploy --list-steps
aspire deploy
```

For local interactive use, `aspire deploy` prompts for missing Azure settings. For non-interactive use, provide them explicitly:

```powershell
$env:Azure__SubscriptionId = "<subscription-id>"
$env:Azure__Location = "eastus"
$env:Azure__ResourceGroup = "rg-eshop-demo"
aspire deploy --non-interactive
```

Use [`aspire publish`](https://aspire.dev/reference/cli/commands/aspire-publish/) when you need deployment artifacts for inspection or another deployment tool. Running it first is not required: `aspire deploy` invokes the deployment pipeline and its dependencies directly rather than consuming an earlier publish output.

When you no longer need the deployment, run [`aspire destroy`](https://aspire.dev/reference/cli/commands/aspire-destroy/). This deletes the entire configured resource group, including resources that Aspire did not create, so review the target carefully before confirming.

## Third-Party Seller Marketplace

The eShop application includes a built-in third-party seller marketplace feature, allowing independent sellers to list and sell products alongside platform-managed offerings.

### Seller Architecture

The marketplace implementation consists of:

- **Sellers.API**: Dedicated microservice for seller registration, profile management, and payout tracking
- **Extended Identity.API**: Support for seller roles and seller_id JWT claims
- **Extended Catalog.API**: Seller attribution on products and seller-specific product management
- **Extended Ordering.API**: Commission tracking and multi-seller order support
- **Event-Driven Payout Ledger**: Automatic payout entry creation on order completion

### Getting Started as a Seller

#### Step 1: Register as a Seller

Send a POST request to the Sellers.API to register:

```bash
curl -X POST https://localhost:5001/api/sellers \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Premium Electronics",
    "email": "seller@example.com",
    "description": "High-quality electronics and accessories",
    "phoneNumber": "+1-555-0123",
    "commissionRate": 0.15
  }'
```

**Response:**
```json
{
  "sellerId": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Premium Electronics",
  "email": "seller@example.com",
  "description": "High-quality electronics and accessories",
  "phoneNumber": "+1-555-0123",
  "commissionRate": 0.15,
  "status": "Active",
  "createdAt": "2026-09-16T12:00:00Z",
  "updatedAt": "2026-09-16T12:00:00Z"
}
```

**Important:**
- `sellerId`: Save this value; use it in all subsequent API calls
- `commissionRate`: A decimal between 0 and 1 (e.g., 0.15 = 15% commission to the platform)
- `status`: Initially "Active"; can be set to "Suspended" by admins

#### Step 2: Authenticate as a Seller

Use your seller credentials to log in via Identity.API to get a JWT token:

```bash
curl -X POST https://localhost:5000/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "seller@example.com",
    "password": "your-secure-password"
  }'
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "sellerId": "550e8400-e29b-41d4-a716-446655440000",
  "role": "seller"
}
```

Store the `accessToken` and use it in the `Authorization: Bearer <token>` header for subsequent requests.

#### Step 3: View Your Seller Profile

Retrieve your seller profile:

```bash
curl -X GET https://localhost:5001/api/sellers/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer <your-token>"
```

**Authorized response (private fields visible):**
```json
{
  "sellerId": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Premium Electronics",
  "description": "High-quality electronics and accessories",
  "email": "seller@example.com",
  "phoneNumber": "+1-555-0123",
  "bankAccountInfo": "...",
  "commissionRate": 0.15,
  "status": "Active",
  "createdAt": "2026-09-16T12:00:00Z",
  "updatedAt": "2026-09-16T12:00:00Z"
}
```

### Managing Products

Sellers can list products in the catalog through Catalog.API. Each product is tagged with the seller's ID, allowing them to manage their own inventory.

**Add a product:**
```bash
curl -X POST https://localhost:5002/api/sellers/550e8400-e29b-41d4-a716-446655440000/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your-token>" \
  -d '{
    "name": "USB-C Cable 10ft",
    "description": "High-quality, certified USB-C cable",
    "price": 12.99,
    "stock": 100,
    "pictureUrl": "https://cdn.example.com/cable.jpg"
  }'
```

**View your products:**
```bash
curl -X GET https://localhost:5002/api/sellers/550e8400-e29b-41d4-a716-446655440000/products \
  -H "Authorization: Bearer <your-token>"
```

### Tracking Orders and Earnings

#### View Your Orders

Sellers can view all orders containing their products:

```bash
curl -X GET "https://localhost:5001/api/sellers/550e8400-e29b-41d4-a716-446655440000/orders?status=Pending&page=1&pageSize=20" \
  -H "Authorization: Bearer <your-token>"
```

**Response:**
```json
{
  "orders": [
    {
      "orderId": "order-12345",
      "customerId": "cust-67890",
      "orderDate": "2026-09-16T12:30:00Z",
      "status": "Processing",
      "items": [
        {
          "lineItemId": "lineitem-1",
          "productId": "prod-1",
          "productName": "USB-C Cable 10ft",
          "quantity": 2,
          "unitPrice": 12.99,
          "grossTotal": 25.98,
          "commissionRate": 0.15,
          "commissionAmount": 3.90,
          "sellerAmount": 22.08
        }
      ]
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 1
}
```

#### View Payout Ledger

Sellers can track their earnings and payout status:

```bash
curl -X GET "https://localhost:5001/api/sellers/550e8400-e29b-41d4-a716-446655440000/payouts?status=Pending" \
  -H "Authorization: Bearer <your-token>"
```

**Response:**
```json
{
  "payouts": [
    {
      "payoutId": "payout-1",
      "orderId": "order-12345",
      "lineItemId": "lineitem-1",
      "grossAmount": 25.98,
      "commissionAmount": 3.90,
      "sellerAmount": 22.08,
      "status": "Pending",
      "createdAt": "2026-09-16T12:30:00Z",
      "paidAt": null
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 1
}
```

#### View Payout Summary

Get a quick overview of your earnings:

```bash
curl -X GET https://localhost:5001/api/sellers/550e8400-e29b-41d4-a716-446655440000/payouts/summary \
  -H "Authorization: Bearer <your-token>"
```

**Response:**
```json
{
  "sellerId": "550e8400-e29b-41d4-a716-446655440000",
  "totalEarned": 542.50,
  "totalPending": 127.50,
  "totalProcessed": 0.00,
  "totalPaid": 415.00,
  "pendingPayoutCount": 3,
  "lastPaymentDate": "2026-09-15T10:30:00Z"
}
```

### Multi-Seller Checkout

Customers can purchase products from multiple sellers in a single checkout. The platform:
- Collects payment once from the customer
- Splits revenue based on seller commission rates
- Creates separate payout entries for each seller
- Provides each seller visibility into their portion of the order

### API Documentation

Complete API documentation is available in the Swagger UI:

**When running locally:**
- Sellers.API Swagger: https://localhost:5001/swagger/ui
- Catalog.API Swagger: https://localhost:5002/swagger/ui
- Ordering.API Swagger: https://localhost:5003/swagger/ui

Each endpoint includes request/response examples and authorization requirements.

## Contributing

For more information on contributing to this repo, read [the contribution documentation](./CONTRIBUTING.md) and [the Code of Conduct](CODE-OF-CONDUCT.md).


### Sample data

The sample catalog data is defined in [catalog.json](https://github.com/dotnet/eShop/blob/main/src/Catalog.API/Setup/catalog.json). Those product names, descriptions, and brand names are fictional and were generated using [GPT-35-Turbo](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/chatgpt), and the corresponding [product images](https://github.com/dotnet/eShop/tree/main/src/Catalog.API/Pics) were generated using [DALL·E 3](https://openai.com/dall-e-3).

## eShop on Azure

For a version of this app configured for deployment on Azure, please view [the eShop on Azure](https://github.com/Azure-Samples/eShopOnAzure) repo.
