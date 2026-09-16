import { test, expect } from '@playwright/test';

/**
 * Section 12.4: Performance test for catalog queries
 * 
 * Performance baseline:
 * - Test with 100k products across 50 sellers
 * - Measure query latency
 * - Verify latency is <200ms
 */

interface QueryMetrics {
  queryName: string;
  latencyMs: number;
  itemsReturned: number;
  passed: boolean;
}

const PERFORMANCE_TARGET_MS = 200;
const metrics: QueryMetrics[] = [];

test.describe('Catalog Performance Tests', () => {
  test('should load catalog page within 200ms', async ({ page }) => {
    const startTime = performance.now();
    
    await page.goto('/catalog', { waitUntil: 'networkidle' });
    
    const loadTime = performance.now() - startTime;
    const passed = loadTime < PERFORMANCE_TARGET_MS;
    
    metrics.push({
      queryName: 'Load catalog page',
      latencyMs: loadTime,
      itemsReturned: await page.locator('.catalog-product, [data-testid="product-card"]').count(),
      passed
    });
    
    console.log(`⏱️  Catalog page load: ${loadTime.toFixed(2)}ms (target: <${PERFORMANCE_TARGET_MS}ms) - ${passed ? '✓' : '✗'}`);
    expect(passed).toBe(true);
  });

  test('should search catalog with 100k products within 200ms', async ({ page }) => {
    // This test assumes the database is populated with test data
    // In real scenario, you would seed this data or use a test database
    
    const startTime = performance.now();
    
    // Perform catalog search
    const response = await page.context().request.get(
      '/api/catalog/products?pageNumber=1&pageSize=20'
    );
    
    const queryTime = performance.now() - startTime;
    const data = await response.json();
    const itemCount = data.data?.length || data.length || 0;
    const passed = queryTime < PERFORMANCE_TARGET_MS;
    
    metrics.push({
      queryName: 'Catalog search (page 1, 20 items)',
      latencyMs: queryTime,
      itemsReturned: itemCount,
      passed
    });
    
    console.log(`⏱️  Catalog search: ${queryTime.toFixed(2)}ms (${itemCount} items) - ${passed ? '✓' : '✗'}`);
    expect(passed).toBe(true);
  });

  test('should filter products by seller within 200ms', async ({ page }) => {
    const startTime = performance.now();
    
    // Filter by seller (assuming seller 1 exists)
    const response = await page.context().request.get(
      '/api/catalog/products?sellerId=1&pageNumber=1&pageSize=20'
    );
    
    const queryTime = performance.now() - startTime;
    const data = await response.json();
    const itemCount = data.data?.length || data.length || 0;
    const passed = queryTime < PERFORMANCE_TARGET_MS;
    
    metrics.push({
      queryName: 'Filter by seller',
      latencyMs: queryTime,
      itemsReturned: itemCount,
      passed
    });
    
    console.log(`⏱️  Filter by seller: ${queryTime.toFixed(2)}ms (${itemCount} items) - ${passed ? '✓' : '✗'}`);
    expect(passed).toBe(true);
  });

  test('should search products by keyword within 200ms', async ({ page }) => {
    const startTime = performance.now();
    
    const response = await page.context().request.get(
      '/api/catalog/products?search=test&pageNumber=1&pageSize=20'
    );
    
    const queryTime = performance.now() - startTime;
    const data = await response.json();
    const itemCount = data.data?.length || data.length || 0;
    const passed = queryTime < PERFORMANCE_TARGET_MS;
    
    metrics.push({
      queryName: 'Search by keyword',
      latencyMs: queryTime,
      itemsReturned: itemCount,
      passed
    });
    
    console.log(`⏱️  Search by keyword: ${queryTime.toFixed(2)}ms (${itemCount} items) - ${passed ? '✓' : '✗'}`);
    expect(passed).toBe(true);
  });

  test('should paginate through large result set within 200ms per page', async ({ page }) => {
    const pagesToTest = [1, 2, 5, 10];
    
    for (const page_ of pagesToTest) {
      const startTime = performance.now();
      
      const response = await page.context().request.get(
        `/api/catalog/products?pageNumber=${page_}&pageSize=50`
      );
      
      const queryTime = performance.now() - startTime;
      const data = await response.json();
      const itemCount = data.data?.length || data.length || 0;
      const passed = queryTime < PERFORMANCE_TARGET_MS;
      
      metrics.push({
        queryName: `Pagination page ${page_}`,
        latencyMs: queryTime,
        itemsReturned: itemCount,
        passed
      });
      
      console.log(`⏱️  Page ${page_}: ${queryTime.toFixed(2)}ms (${itemCount} items) - ${passed ? '✓' : '✗'}`);
      expect(passed).toBe(true);
    }
  });

  test('should get seller details within 200ms', async ({ page }) => {
    const testSellerIds = ['1', '2', '3']; // Assuming these sellers exist
    
    for (const sellerId of testSellerIds) {
      const startTime = performance.now();
      
      const response = await page.context().request.get(
        `/api/sellers/${sellerId}`
      );
      
      const queryTime = performance.now() - startTime;
      const passed = queryTime < PERFORMANCE_TARGET_MS;
      
      metrics.push({
        queryName: `Get seller ${sellerId}`,
        latencyMs: queryTime,
        itemsReturned: response.status() === 200 ? 1 : 0,
        passed
      });
      
      console.log(`⏱️  Get seller ${sellerId}: ${queryTime.toFixed(2)}ms - ${passed ? '✓' : '✗'}`);
      
      if (response.status() === 200) {
        expect(queryTime).toBeLessThan(PERFORMANCE_TARGET_MS);
      }
    }
  });

  test('should load seller products within 200ms', async ({ page }) => {
    const testSellerIds = ['1', '2', '3']; // Assuming these sellers exist
    
    for (const sellerId of testSellerIds) {
      const startTime = performance.now();
      
      const response = await page.context().request.get(
        `/api/catalog/sellers/${sellerId}/products`
      );
      
      const queryTime = performance.now() - startTime;
      const data = await response.json();
      const itemCount = data.data?.length || data.length || 0;
      const passed = queryTime < PERFORMANCE_TARGET_MS;
      
      metrics.push({
        queryName: `Load seller ${sellerId} products`,
        latencyMs: queryTime,
        itemsReturned: itemCount,
        passed
      });
      
      console.log(`⏱️  Seller ${sellerId} products: ${queryTime.toFixed(2)}ms (${itemCount} items) - ${passed ? '✓' : '✗'}`);
      
      if (response.status() === 200) {
        expect(queryTime).toBeLessThan(PERFORMANCE_TARGET_MS);
      }
    }
  });

  test.afterAll(async () => {
    // Print performance summary
    console.log('\n📊 Performance Test Summary:');
    console.log('================================');
    
    const totalTests = metrics.length;
    const passedTests = metrics.filter(m => m.passed).length;
    const failedTests = totalTests - passedTests;
    
    const avgLatency = metrics.reduce((sum, m) => sum + m.latencyMs, 0) / totalTests;
    const maxLatency = Math.max(...metrics.map(m => m.latencyMs));
    const minLatency = Math.min(...metrics.map(m => m.latencyMs));
    
    console.log(`Total Tests: ${totalTests}`);
    console.log(`Passed: ${passedTests} ✓`);
    console.log(`Failed: ${failedTests} ${failedTests > 0 ? '✗' : ''}`);
    console.log(`Average Latency: ${avgLatency.toFixed(2)}ms`);
    console.log(`Min Latency: ${minLatency.toFixed(2)}ms`);
    console.log(`Max Latency: ${maxLatency.toFixed(2)}ms`);
    console.log(`Target: <${PERFORMANCE_TARGET_MS}ms`);
    console.log('================================\n');
    
    // Print detailed results
    console.log('Detailed Results:');
    metrics.forEach(m => {
      console.log(`  ${m.queryName}: ${m.latencyMs.toFixed(2)}ms (${m.itemsReturned} items) ${m.passed ? '✓' : '✗'}`);
    });
  });
});
