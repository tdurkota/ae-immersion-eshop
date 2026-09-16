/**
 * Common test utilities for Section 12 E2E tests
 * 
 * Provides helper functions for:
 * - Creating test sellers
 * - Creating test products
 * - Placing test orders
 * - Authentication
 * - Assertions
 */

import { Page } from '@playwright/test';

export interface TestSeller {
  sellerId: string;
  name: string;
  email: string;
  commissionRate: number;
  status: string;
}

export interface TestProduct {
  productId: string;
  sellerId: string;
  name: string;
  description: string;
  price: number;
  stock: number;
}

export interface TestOrder {
  orderId: string;
  items: Array<{
    productId: string;
    sellerId: string;
    quantity: number;
    price: number;
  }>;
  total: number;
  customerId: string;
}

/**
 * Create a test seller
 */
export async function createTestSeller(
  page: Page,
  name: string,
  email: string,
  commissionRate: number = 0.15
): Promise<TestSeller> {
  const response = await page.context().request.post('/api/sellers', {
    data: {
      name,
      email,
      description: `Test seller - ${name}`,
      phoneNumber: '+1-555-0000',
      commissionRate,
      bankAccountInfo: `TEST-BANK-${Date.now()}`
    }
  });

  if (response.status() !== 201) {
    throw new Error(`Failed to create seller: ${response.status()}`);
  }

  const seller = await response.json();
  return {
    sellerId: seller.sellerId,
    name: seller.name,
    email: seller.email,
    commissionRate: seller.commissionRate,
    status: seller.status
  };
}

/**
 * Create a test product for a seller
 */
export async function createTestProduct(
  page: Page,
  sellerId: string,
  name: string,
  price: number,
  stock: number = 100,
  description: string = 'Test product'
): Promise<TestProduct> {
  const response = await page.context().request.post(
    `/api/catalog/sellers/${sellerId}/products`,
    {
      data: {
        name,
        description,
        price,
        stock,
        category: 'Electronics',
        sellerId
      }
    }
  );

  if (![200, 201].includes(response.status())) {
    throw new Error(`Failed to create product: ${response.status()}`);
  }

  const product = await response.json();
  return {
    productId: product.productId || product.id,
    sellerId,
    name: product.name,
    description: product.description || description,
    price: product.price || price,
    stock: product.stock || stock
  };
}

/**
 * Place a test order
 */
export async function createTestOrder(
  page: Page,
  items: Array<{ productId: string; sellerId: string; quantity: number; price: number }>,
  customerId: string = `customer-${Date.now()}`
): Promise<TestOrder> {
  const total = items.reduce((sum, item) => sum + (item.price * item.quantity), 0);

  const response = await page.context().request.post('/api/orders', {
    data: {
      items,
      customerId,
      orderDate: new Date().toISOString()
    }
  });

  if (![200, 201].includes(response.status())) {
    throw new Error(`Failed to create order: ${response.status()}`);
  }

  const order = await response.json();
  return {
    orderId: order.orderId || order.id,
    items,
    total,
    customerId
  };
}

/**
 * Get seller by ID
 */
export async function getSeller(page: Page, sellerId: string, token?: string): Promise<TestSeller> {
  const headers = token ? { 'Authorization': `Bearer ${token}` } : {};
  
  const response = await page.context().request.get(
    `/api/sellers/${sellerId}`,
    { headers }
  );

  if (response.status() !== 200) {
    throw new Error(`Failed to get seller: ${response.status()}`);
  }

  const seller = await response.json();
  return {
    sellerId: seller.sellerId,
    name: seller.name,
    email: seller.email,
    commissionRate: seller.commissionRate,
    status: seller.status
  };
}

/**
 * Get seller products
 */
export async function getSellerProducts(
  page: Page,
  sellerId: string,
  token?: string
): Promise<TestProduct[]> {
  const headers = token ? { 'Authorization': `Bearer ${token}` } : {};

  const response = await page.context().request.get(
    `/api/catalog/sellers/${sellerId}/products`,
    { headers }
  );

  if (response.status() !== 200) {
    throw new Error(`Failed to get seller products: ${response.status()}`);
  }

  const data = await response.json();
  const products = Array.isArray(data) ? data : (data.data || data.products || []);

  return products.map((p: any) => ({
    productId: p.productId || p.id,
    sellerId: p.sellerId,
    name: p.name,
    description: p.description,
    price: p.price,
    stock: p.stock
  }));
}

/**
 * Get seller orders
 */
export async function getSellerOrders(
  page: Page,
  sellerId: string,
  token: string
): Promise<TestOrder[]> {
  const response = await page.context().request.get(
    `/api/sellers/${sellerId}/orders`,
    {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    }
  );

  if (response.status() !== 200) {
    throw new Error(`Failed to get seller orders: ${response.status()}`);
  }

  const data = await response.json();
  const orders = Array.isArray(data) ? data : (data.data || data.orders || []);

  return orders.map((o: any) => ({
    orderId: o.orderId || o.id,
    items: o.items || [],
    total: o.total || 0,
    customerId: o.customerId
  }));
}

/**
 * Get seller payouts
 */
export async function getSellerPayouts(
  page: Page,
  sellerId: string,
  token: string
): Promise<Array<{ payoutId: string; amount: number; status: string }>> {
  const response = await page.context().request.get(
    `/api/sellers/${sellerId}/payouts`,
    {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    }
  );

  if (response.status() !== 200) {
    throw new Error(`Failed to get seller payouts: ${response.status()}`);
  }

  const data = await response.json();
  const payouts = Array.isArray(data) ? data : (data.data || data.payouts || []);

  return payouts.map((p: any) => ({
    payoutId: p.payoutId || p.id,
    amount: p.amount || 0,
    status: p.status || 'Pending'
  }));
}

/**
 * Calculate expected commission
 */
export function calculateExpectedCommission(
  orderTotal: number,
  commissionRate: number
): number {
  return Math.round(orderTotal * commissionRate * 100) / 100; // Round to 2 decimals
}

/**
 * Verify commission calculation
 */
export function verifyCommissionCalculation(
  orderTotal: number,
  commissionRate: number,
  actualCommission: number,
  tolerance: number = 0.01
): boolean {
  const expected = calculateExpectedCommission(orderTotal, commissionRate);
  return Math.abs(expected - actualCommission) <= tolerance;
}

/**
 * Generate unique test email
 */
export function generateTestEmail(prefix: string = 'test'): string {
  return `${prefix}-${Date.now()}@test.local`;
}

/**
 * Generate unique test name
 */
export function generateTestName(prefix: string = 'Test'): string {
  return `${prefix} ${Date.now()}`;
}

/**
 * Wait for condition with timeout
 */
export async function waitFor(
  condition: () => Promise<boolean>,
  timeout: number = 5000,
  interval: number = 100
): Promise<void> {
  const startTime = Date.now();
  
  while (Date.now() - startTime < timeout) {
    if (await condition()) {
      return;
    }
    await new Promise(resolve => setTimeout(resolve, interval));
  }
  
  throw new Error(`Condition not met within ${timeout}ms`);
}

/**
 * Assert API response status
 */
export function assertStatus(
  actual: number,
  expected: number | number[],
  message?: string
): void {
  const expectedStatuses = Array.isArray(expected) ? expected : [expected];
  
  if (!expectedStatuses.includes(actual)) {
    throw new Error(
      `${message || 'Status mismatch'}: expected ${expectedStatuses.join(' or ')}, got ${actual}`
    );
  }
}

/**
 * Assert seller has access
 */
export async function assertSellerHasAccess(
  page: Page,
  sellerId: string,
  token: string
): Promise<void> {
  const response = await page.context().request.get(
    `/api/sellers/${sellerId}/orders`,
    {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    }
  );

  assertStatus(response.status(), 200, `Seller ${sellerId} should have access to their orders`);
}

/**
 * Assert seller denied access
 */
export async function assertSellerDeniedAccess(
  page: Page,
  sellerId: string,
  token: string,
  endpoint: string = '/orders'
): Promise<void> {
  const response = await page.context().request.get(
    `/api/sellers/${sellerId}${endpoint}`,
    {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    }
  );

  assertStatus(response.status(), 403, `Seller should be denied access to ${endpoint}`);
}

/**
 * Format currency for display
 */
export function formatCurrency(amount: number): string {
  return `$${amount.toFixed(2)}`;
}

/**
 * Parse currency from string
 */
export function parseCurrency(value: string): number {
  return parseFloat(value.replace(/[^\d.-]/g, ''));
}
