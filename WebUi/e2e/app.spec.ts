import { expect, test } from '@playwright/test'

test('loads the contacts page shell', async ({ page }) => {
  await page.goto('/contacts')

  const main = page.locator('main')
  await expect(page.getByRole('heading', { name: 'Contacts' })).toBeVisible()
  await expect(main.getByRole('link', { name: 'Create Contact' })).toBeVisible()
})
