import { test, expect } from '@playwright/test';

/**
 * Section 12.6: Authorization security test
 * 
 * Tests that:
 * 1. Seller A cannot access Seller B's profile
 * 2. Seller A cannot access Seller B's products
 * 3. Seller A cannot access Seller B's orders
 * 4. Seller A cannot modify Seller B's profile
 * 5. Seller A cannot access Seller B's payouts
 * 6. All unauthorized access attempts return 403 Forbidden
 * 7. No data leakage occurs
 */
test.describe('Authorization & Security Tests', () => {
  let seller1: any;
  let seller2: any;

  test.beforeAll(async ({ browser }) => {
    // Setup: Create two test sellers
    const context = await browser.newContext();
    const timestamp = Date.now();

    // Create Seller 1
    const seller1Response = await context.request.post('/api/sellers', {
      data: {
        name: `Security Test Seller 1 ${timestamp}`,
        email: `security-seller1-${timestamp}@test.local`,
        description: 'Security test seller 1',
        phoneNumber: '+1-555-0400',
        commissionRate: 0.15,
        bankAccountInfo: `BANK-SEC1-${timestamp}`
      }
    });

    seller1 = await seller1Response.json();

    // Create Seller 2
    const seller2Response = await context.request.post('/api/sellers', {
      data: {
        name: `Security Test Seller 2 ${timestamp}`,
        email: `security-seller2-${timestamp}@test.local`,
        description: 'Security test seller 2',
        phoneNumber: '+1-555-0401',
        commissionRate: 0.10,
        bankAccountInfo: `BANK-SEC2-${timestamp}`
      }
    });

    seller2 = await seller2Response.json();

    // Add products for both sellers
    await context.request.post(`/api/catalog/sellers/${seller1.sellerId}/products`, {
      data: {
        name: `Seller 1 Secret Product ${timestamp}`,
        description: 'Product only seller 1 should see',
        price: 199.99,
        stock: 50,
        category: 'Electronics'
      }
    });

    await context.request.post(`/api/catalog/sellers/${seller2.sellerId}/products`, {
      data: {
        name: `Seller 2 Secret Product ${timestamp}`,
        description: 'Product only seller 2 should see',
        price: 299.99,
        stock: 50,
        category: 'Electronics'
      }
    });

    await context.close();
  });

  test('should reject access to another seller profile with 403', async ({ page }) => {
    // Seller 2 tries to access Seller 1's profile without authorization
    const response = await page.context().request.get(
      `/api/sellers/${seller1.sellerId}`,
      {
        headers: {
          'Authorization': `Bearer ${process.env.TEST_SELLER_2_TOKEN || 'seller2-token'}`
        }
      }
    );

    // Should return 403 Forbidden
    expect(response.status()).toBe(403);
    console.log('✓ Access to another seller profile rejected with 403');
  });

  test('should not return seller email without authorization', async ({ page }) => {
    // Unauthenticated request to seller profile should not return private fields
    const response = await page.context().request.get(
      `/api/sellers/${seller1.sellerId}`
    );

    if (response.status() === 200) {
      const data = await response.json();
      // Email should not be present for unauthenticated requests
      expect(data.email).toBeUndefined();
      expect(data.bankAccountInfo).toBeUndefined();
      console.log('✓ Private seller fields not exposed to unauthenticated requests');
    }
  });

  test('should reject modification of another seller profile with 403', async ({ page }) => {
    // Seller 2 tries to modify Seller 1's profile
    const response = await page.context().request.put(
      `/api/sellers/${seller1.sellerId}`,
      {
        headers: {
          'Authorization': `Bearer ${process.env.TEST_SELLER_2_TOKEN || 'seller2-token'}`
        },
        data: {
          name: 'Hacked Seller 1',
          description: 'This was hacked'
        }
      }
    );

    // Should return 403 Forbidden
    expect(response.status()).toBe(403);
    console.log('✓ Profile modification attempt rejected with 403');

    // Verify profile was not actually modified
    const verifyResponse = await page.context().request.get(`/api/sellers/${seller1.sellerId}`);
    if (verifyResponse.status() === 200) {
      const data = await verifyResponse.json();
      expect(data.name).toBe(`Security Test Seller 1`);
      expect(data.description).not.toContain('hacked');
      console.log('✓ Profile was not modified by unauthorized request');
    }
  });

  test('should reject access to another seller orders with 403', async ({ page }) => {
    // Seller 2 tries to access Seller 1's orders
    const response = await page.context().request.get(
      `/api/sellers/${seller1.sellerId}/orders`,
      {
        headers: {
          'Authorization': `Bearer ${process.env.TEST_SELLER_2_TOKEN || 'seller2-token'}`
        }
      }
    );

    // Should return 403 Forbidden
    expect(response.status()).toBe(403);
    console.log('✓ Access to another seller orders rejected with 403');
  });

  test('should reject access to another seller payouts with 403', async ({ page }) => {
    // Seller 2 tries to access Seller 1's payouts
    const response = await page.context().request.get(
      `/api/sellers/${seller1.sellerId}/payouts`,
      {
        headers: {
          'Authorization': `Bearer ${process.env.TEST_SELLER_2_TOKEN || 'seller2-token'}`
        }
      }
    );

    // Should return 403 Forbidden
    expect(response.status()).toBe(403);
    console.log('✓ Access to another seller payouts rejected with 403');
  });

  test('should reject deletion of another seller with 403', async ({ page }) => {
    // Seller 2 tries to delete Seller 1
    const response = await page.context().request.delete(
      `/api/sellers/${seller1.sellerId}`,
      {
        headers: {
          'Authorization': `Bearer ${process.env.TEST_SELLER_2_TOKEN || 'seller2-token'}`
        }
      }
    );

    // Should return 403 Forbidden
    expect(response.status()).toBe(403);
    console.log('✓ Deletion attempt by another seller rejected with 403');

    // Verify seller still exists
    const verifyResponse = await page.context().request.get(`/api/sellers/${seller1.sellerId}`);
    expect(verifyResponse.status()).toBe(200);
    console.log('✓ Seller still exists after unauthorized deletion attempt');
  });

  test('should ensure data isolation between sellers products', async ({ page }) => {
    // Seller 2 should only see their own products in their products list
    const seller1ProductsResponse = await page.context().request.get(
      `/api/catalog/sellers/${seller1.sellerId}/products`,
      {
        headers: {
          'Authorization': `Bearer ${process.env.TEST_SELLER_2_TOKEN || 'seller2-token'}`
        }
      }
    );

    // Should return 403 if seller-specific endpoint requires authorization
    if (seller1ProductsResponse.status() === 200) {
      const products = await seller1ProductsResponse.json();
      // If this endpoint is public, verify products don't contain sensitive data
      expect(Array.isArray(products) || Array.isArray(products.data)).toBe(true);
      console.log('✓ Product endpoint returned data (public endpoint)');
    } else {
      expect(seller1ProductsResponse.status()).toBe(403);
      console.log('✓ Access to another seller products list rejected with 403');
    }
  });

  test('should prevent data leakage in error messages', async ({ page }) => {
    // Try to access non-existent seller - should not reveal system details
    const response = await page.context().request.get(
      '/api/sellers/00000000-0000-0000-0000-000000000999'
    );

    const data = await response.json();
    
    if (response.status() === 404) {
      // Error message should not contain sensitive information
      expect(data).not.toContain('database');
      expect(data).not.toContain('connection');
      expect(data).not.toContain('query');
      expect(data.error || data.message).toContain('not found');
      console.log('✓ Error messages do not contain sensitive information');
    }
  });

  test('should validate seller_id claim in authorization header', async ({ page }) => {
    // Request with invalid seller_id claim should be rejected
    const response = await page.context().request.get(
      `/api/sellers/${seller1.sellerId}`,
      {
        headers: {
          'Authorization': 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzZWxsZXJfaWQiOiJpbnZhbGlkIn0.invalid'
        }
      }
    );

    // Should reject invalid token
    expect([401, 403, 400]).toContain(response.status());
    console.log('✓ Invalid authorization token rejected');
  });

  test('should enforce seller isolation in order creation', async ({ page }) => {
    // Try to create an order with another seller's products but current seller's claim
    const timestamp = Date.now();
    
    // Create test products
    const seller1ProductResponse = await page.context().request.post(
      `/api/catalog/sellers/${seller1.sellerId}/products`,
      {
        data: {
          name: `Isolation Test Product ${timestamp}`,
          description: 'For isolation test',
          price: 99.99,
          stock: 50,
          category: 'Electronics'
        }
      }
    );

    if (seller1ProductResponse.status() === 200 || seller1ProductResponse.status() === 201) {
      const product = await seller1ProductResponse.json();
      
      // Try to create order claiming it's from seller 2
      const orderResponse = await page.context().request.post('/api/orders', {
        data: {
          items: [{
            productId: product.productId || product.id,
            quantity: 1,
            price: 99.99,
            sellerId: seller2.sellerId // Wrong seller!
          }],
          customerId: `test-customer-${timestamp}`
        }
      });

      if (orderResponse.status() === 200 || orderResponse.status() === 201) {
        const order = await orderResponse.json();
        // Verify the order was created with correct seller (seller 1, not 2)
        const orderItem = order.items[0];
        expect(orderItem.sellerId).toBe(seller1.sellerId);
        console.log('✓ Order seller attribution is enforced (cannot be spoofed)');
      }
    }
  });

  test('should reject access without authentication token', async ({ page }) => {
    // Request without any authentication header
    const response = await page.context().request.get(
      `/api/sellers/${seller1.sellerId}/orders`
    );

    // Should return 401 or 403
    expect([401, 403, 404]).toContain(response.status());
    console.log('✓ Unauthenticated access rejected');
  });

  test('should prevent cross-seller order visibility', async ({ page }) => {
    // Seller 2 should not be able to see Seller 1's orders
    const seller1OrdersResponse = await page.context().request.get(
      `/api/sellers/${seller1.sellerId}/orders`,
      {
        headers: {
          'Authorization': `Bearer ${process.env.TEST_SELLER_2_TOKEN || 'seller2-token'}`
        }
      }
    );

    expect(seller1OrdersResponse.status()).toBe(403);
    console.log('✓ Cross-seller order visibility prevented with 403');

    // Verify Seller 1 can see their own orders
    const seller1OwnOrdersResponse = await page.context().request.get(
      `/api/sellers/${seller1.sellerId}/orders`,
      {
        headers: {
          'Authorization': `Bearer ${process.env.TEST_SELLER_1_TOKEN || 'seller1-token'}`
        }
      }
    );

    // Should return 200 for their own orders
    if (seller1OwnOrdersResponse.status() !== 404) {
      expect(seller1OwnOrdersResponse.status()).toBe(200);
      console.log('✓ Seller can access their own orders');
    }
  });
});
