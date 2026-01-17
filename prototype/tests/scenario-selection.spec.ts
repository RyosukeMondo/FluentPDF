import { test, expect } from '@playwright/test';

test('selecting simple document shows PDF content', async ({ page }) => {
  // Navigate to the app
  await page.goto('http://localhost:5173');

  // Wait for app to load
  await page.waitForLoadState('networkidle');

  // Take screenshot of initial state
  await page.screenshot({ path: 'test-screenshots/01-initial.png' });

  // Click the "Open PDF Scenario" button
  const openButton = page.locator('button:has-text("📂 Open PDF Scenario")');
  await expect(openButton).toBeVisible();
  await openButton.click();

  // Wait for dialog to appear (check for dialog header)
  await page.waitForSelector('text=Open PDF Scenario', { state: 'visible', timeout: 5000 });
  await page.screenshot({ path: 'test-screenshots/02-dialog-open.png' });

  // Click "Simple Document" scenario
  const simpleDocButton = page.locator('button:has-text("Simple Document")');
  await expect(simpleDocButton).toBeVisible();
  await simpleDocButton.click();

  // Wait a bit for state to update
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-screenshots/03-after-selection.png' });

  // Check if dialog closed (header should not be visible)
  await expect(page.locator('h2:has-text("Open PDF Scenario")')).not.toBeVisible();

  // Check if page number is shown in toolbar (use first() to handle duplicates)
  await expect(page.locator('text=Page 1 of 5').first()).toBeVisible();

  // Check if PDF content text is visible
  await expect(page.locator('text=Lorem ipsum dolor sit amet')).toBeVisible();

  // Check if thumbnails are visible
  await expect(page.locator('text=Thumbnails')).toBeVisible();

  // Check if bookmarks panel shows expected message
  await expect(page.locator('text=No bookmarks available')).toBeVisible();

  console.log('✓ PDF scenario loaded successfully!');

  // Take final screenshot
  await page.screenshot({ path: 'test-screenshots/04-final.png' });
});
