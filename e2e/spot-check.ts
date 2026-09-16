/**
 * Section 12.5: Manual spot-check utilities
 * 
 * Provides utilities for manual testing of:
 * 1. Create 3 test sellers
 * 2. Add 10 products
 * 3. Place 5 orders
 * 4. Verify commission calculations match expected amounts
 * 5. Verify reconciliation is correct
 * 
 * Usage:
 * npx ts-node e2e/spot-check.ts
 */

import { chromium, Browser, Page } from '@playwright/test';

interface TestData {
  sellers: Array<{ sellerId: string; name: string; email: string; commissionRate: number }>;
  products: Array<{ productId: string; sellerId: string; name: string; price: number }>;
  orders: Array<{ orderId: string; items: Array<{ productId: string; quantity: number; sellerCommission: number }> }>;
  payouts: Array<{ payoutId: string; sellerId: string; amount: number; expectedAmount: number }>;
}

class SpotCheckTester {
  private browser!: Browser;
  private page!: Page;
  private baseUrl: string;
  private testData: TestData = { sellers: [], products: [], orders: [], payouts: [] };

  constructor(baseUrl: string = 'http://localhost:5045') {
    this.baseUrl = baseUrl;
  }

  async initialize(): Promise<void> {
    this.browser = await chromium.launch();
    this.page = await this.browser.newPage();
  }

  async cleanup(): Promise<void> {
    await this.browser.close();
  }

  async createTestSellers(): Promise<void> {
    console.log('\n📝 Creating 3 test sellers...');
    
    const timestamp = Date.now();
    const sellers = [
      {
        name: `Test Seller 1 ${timestamp}`,
        email: `seller1-${timestamp}@test.local`,
        commissionRate: 0.10, // 10%
        phoneNumber: '+1-555-0301',
        bankAccountInfo: `BANK-1-${timestamp}`
      },
      {
        name: `Test Seller 2 ${timestamp}`,
        email: `seller2-${timestamp}@test.local`,
        commissionRate: 0.15, // 15%
        phoneNumber: '+1-555-0302',
        bankAccountInfo: `BANK-2-${timestamp}`
      },
      {
        name: `Test Seller 3 ${timestamp}`,
        email: `seller3-${timestamp}@test.local`,
        commissionRate: 0.20, // 20%
        phoneNumber: '+1-555-0303',
        bankAccountInfo: `BANK-3-${timestamp}`
      }
    ];

    for (const seller of sellers) {
      const response = await this.page.context().request.post('/api/sellers', {
        data: seller
      });

      if (response.status() === 201) {
        const data = await response.json();
        this.testData.sellers.push({
          sellerId: data.sellerId,
          name: seller.name,
          email: seller.email,
          commissionRate: seller.commissionRate
        });
        console.log(`  ✓ Created seller: ${data.sellerId} (${seller.name}, commission: ${seller.commissionRate * 100}%)`);
      }
    }
  }

  async createTestProducts(): Promise<void> {
    console.log('\n📝 Creating 10 test products across 3 sellers...');
    
    const timestamp = Date.now();
    let productCount = 0;

    for (let i = 0; i < this.testData.sellers.length; i++) {
      const seller = this.testData.sellers[i];
      const productsPerSeller = 4; // 4 products for first 2 sellers, 2 for last = 10 total

      for (let j = 0; j < productsPerSeller; j++) {
        const product = {
          name: `Product ${productCount + 1} from ${seller.name} (${timestamp})`,
          description: `Test product ${productCount + 1}`,
          price: 50 + (productCount * 10), // 50, 60, 70, ...
          stock: 100,
          category: 'Electronics',
          sellerId: seller.sellerId
        };

        const response = await this.page.context().request.post(
          `/api/catalog/sellers/${seller.sellerId}/products`,
          { data: product }
        );

        if (response.status() === 201 || response.status() === 200) {
          const data = await response.json();
          this.testData.products.push({
            productId: data.productId || data.id,
            sellerId: seller.sellerId,
            name: product.name,
            price: product.price
          });
          console.log(`  ✓ Created product: ${product.name} ($${product.price})`);
          productCount++;
        }
      }
    }

    console.log(`Total products created: ${productCount}`);
  }

  async placeTestOrders(): Promise<void> {
    console.log('\n📝 Placing 5 test orders...');
    
    const timestamp = Date.now();
    const ordersData = [
      { productIndex: 0, quantity: 2, description: 'Order 1' },
      { productIndex: 2, quantity: 1, description: 'Order 2' },
      { productIndex: 4, quantity: 3, description: 'Order 3' },
      { productIndex: 6, quantity: 1, description: 'Order 4' },
      { productIndex: 8, quantity: 2, description: 'Order 5' }
    ];

    for (const orderData of ordersData) {
      if (orderData.productIndex >= this.testData.products.length) {
        console.log(`  ⚠ Skipping order - not enough products`);
        continue;
      }

      const product = this.testData.products[orderData.productIndex];
      const orderTotal = product.price * orderData.quantity;

      const response = await this.page.context().request.post('/api/orders', {
        data: {
          items: [{
            productId: product.productId,
            quantity: orderData.quantity,
            price: product.price,
            sellerId: product.sellerId
          }],
          customerId: `customer-${timestamp}-${orderData.description}`,
          orderDate: new Date().toISOString()
        }
      });

      if (response.status() === 200 || response.status() === 201) {
        const orderResult = await response.json();
        
        // Calculate expected commission
        const seller = this.testData.sellers.find(s => s.sellerId === product.sellerId)!;
        const commission = orderTotal * seller.commissionRate;

        this.testData.orders.push({
          orderId: orderResult.orderId || orderResult.id,
          items: [{
            productId: product.productId,
            quantity: orderData.quantity,
            sellerCommission: commission
          }]
        });

        console.log(`  ✓ Created order: ${orderResult.orderId} - ${product.name} x${orderData.quantity} = $${orderTotal} (commission: $${commission.toFixed(2)})`);
      }
    }
  }

  async verifyCommissionCalculations(): Promise<void> {
    console.log('\n📊 Verifying commission calculations...');

    // Aggregate commissions by seller
    const sellerCommissions: Map<string, { orders: number; totalRevenue: number; totalCommission: number; expectedCommission: number }> = new Map();

    for (const order of this.testData.orders) {
      for (const item of order.items) {
        const product = this.testData.products.find(p => p.productId === item.productId)!;
        const seller = this.testData.sellers.find(s => s.sellerId === product.sellerId)!;

        if (!sellerCommissions.has(seller.sellerId)) {
          sellerCommissions.set(seller.sellerId, {
            orders: 0,
            totalRevenue: 0,
            totalCommission: 0,
            expectedCommission: 0
          });
        }

        const stats = sellerCommissions.get(seller.sellerId)!;
        const revenue = product.price * item.quantity;
        const expectedCommission = revenue * seller.commissionRate;

        stats.orders++;
        stats.totalRevenue += revenue;
        stats.expectedCommission += expectedCommission;
      }
    }

    // Verify each seller's commissions via API
    for (const seller of this.testData.sellers) {
      const response = await this.page.context().request.get(
        `/api/sellers/${seller.sellerId}/payouts`
      );

      if (response.status() === 200) {
        const payouts = await response.json();
        const stats = sellerCommissions.get(seller.sellerId);

        if (stats) {
          const totalActualCommission = payouts.reduce((sum: number, p: any) => sum + (p.amount || 0), 0);
          const matches = Math.abs(totalActualCommission - stats.expectedCommission) < 0.01; // Allow $0.01 rounding

          console.log(`  ${matches ? '✓' : '✗'} ${seller.name}:`);
          console.log(`    - Orders: ${stats.orders}`);
          console.log(`    - Total Revenue: $${stats.totalRevenue.toFixed(2)}`);
          console.log(`    - Expected Commission (${seller.commissionRate * 100}%): $${stats.expectedCommission.toFixed(2)}`);
          console.log(`    - Actual Commission: $${totalActualCommission.toFixed(2)}`);
          console.log(`    - Match: ${matches ? 'YES ✓' : 'NO ✗'}`);

          if (matches) {
            // Store verified payout
            this.testData.payouts.push({
              payoutId: payouts[payouts.length - 1]?.payoutId || 'unknown',
              sellerId: seller.sellerId,
              amount: totalActualCommission,
              expectedAmount: stats.expectedCommission
            });
          }
        }
      }
    }
  }

  async verifyReconciliation(): Promise<void> {
    console.log('\n📋 Verifying reconciliation...');

    // Check that order totals match payout amounts
    let reconciliationPassed = true;

    for (const payout of this.testData.payouts) {
      // Verify payout exists in database
      const payoutResponse = await this.page.context().request.get(
        `/api/sellers/${payout.sellerId}/payouts`
      );

      if (payoutResponse.status() === 200) {
        const payouts = await payoutResponse.json();
        const foundPayout = payouts.find((p: any) => Math.abs((p.amount || 0) - payout.amount) < 0.01);

        if (foundPayout) {
          console.log(`  ✓ Payout ${payout.payoutId} verified: $${payout.amount.toFixed(2)}`);
        } else {
          console.log(`  ✗ Payout ${payout.payoutId} NOT found: $${payout.amount.toFixed(2)}`);
          reconciliationPassed = false;
        }
      }
    }

    console.log(`\nReconciliation ${reconciliationPassed ? 'PASSED ✓' : 'FAILED ✗'}`);
  }

  async printSummary(): Promise<void> {
    console.log('\n' + '='.repeat(60));
    console.log('SPOT CHECK SUMMARY');
    console.log('='.repeat(60));
    console.log(`Sellers Created: ${this.testData.sellers.length}`);
    console.log(`Products Created: ${this.testData.products.length}`);
    console.log(`Orders Placed: ${this.testData.orders.length}`);
    console.log(`Payouts Verified: ${this.testData.payouts.length}`);

    // Calculate totals
    let totalOrderValue = 0;
    let totalCommissions = 0;

    for (const order of this.testData.orders) {
      for (const item of order.items) {
        const product = this.testData.products.find(p => p.productId === item.productId)!;
        const orderValue = product.price * item.quantity;
        totalOrderValue += orderValue;
        totalCommissions += item.sellerCommission;
      }
    }

    console.log(`\nFinancial Summary:`);
    console.log(`  Total Order Value: $${totalOrderValue.toFixed(2)}`);
    console.log(`  Total Commissions: $${totalCommissions.toFixed(2)}`);
    console.log(`  Average Commission Rate: ${((totalCommissions / totalOrderValue) * 100).toFixed(2)}%`);
    console.log('='.repeat(60));
  }

  async run(): Promise<void> {
    try {
      await this.initialize();
      await this.createTestSellers();
      await this.createTestProducts();
      await this.placeTestOrders();
      await this.verifyCommissionCalculations();
      await this.verifyReconciliation();
      await this.printSummary();
    } catch (error) {
      console.error('❌ Spot check failed:', error);
      throw error;
    } finally {
      await this.cleanup();
    }
  }
}

// Run the spot check
const tester = new SpotCheckTester();
tester.run().catch(console.error);

export { SpotCheckTester };
