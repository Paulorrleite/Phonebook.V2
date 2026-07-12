import { expect, type APIRequestContext, type Page, test } from '@playwright/test'

type PhoneType = 'Mobile' | 'Residential' | 'Commercial'

interface PhoneNumberRequest {
  readonly countryCode: string
  readonly areaCode: string
  readonly number: string
  readonly type: PhoneType
  readonly isFavorite: boolean
}

interface AddressRequest {
  readonly country: string
  readonly state: string
  readonly city: string
  readonly neighborhood: string
  readonly postalCode: string
}

interface PhotoRequest {
  readonly content: string
  readonly contentType: string
  readonly fileName: string
  readonly size: number
}

interface ContactRequest {
  readonly firstName: string
  readonly lastName: string | null
  readonly email: string | null
  readonly phoneNumbers: readonly PhoneNumberRequest[]
  readonly address: AddressRequest | null
  readonly photo: PhotoRequest | null
  readonly removePhoto: boolean
}

interface CreateContactResponse {
  readonly id: string
}

interface ContactEditResponse {
  readonly id: string
  readonly firstName: string
  readonly lastName: string | null
  readonly email: string | null
  readonly photo: {
    readonly fileName: string
    readonly size: number
  } | null
}

const apiBaseURL = process.env.PLAYWRIGHT_API_BASE_URL ?? 'http://localhost:5087'

test('creates a contact and returns to the list', async ({ page }) => {
  const token = uniqueToken()
  const name = `Create ${token}`

  await page.goto('/contacts/new')
  await fillRequiredContactFields(page, {
    firstName: name,
    lastName: 'Contact',
    email: `create.${token}@example.test`,
    number: `555${token.slice(-7)}`,
  })
  await page.getByRole('button', { name: 'Save Contact' }).click()

  await expect(page).toHaveURL(/\/contacts$/)
  await page.getByRole('searchbox', { name: 'Search' }).fill(name)
  await expect(page.getByRole('heading', { name: `${name} Contact` })).toBeVisible()
  await expect(page.getByText(`create.${token}@example.test`)).toBeVisible()
})

test('shows create validation errors for required fields, oversized photo, and partial address', async ({ page }) => {
  await page.goto('/contacts/new')
  await page.getByLabel('Country', { exact: true }).fill('Brazil')
  await page.getByLabel('Upload Photo').setInputFiles({
    name: 'too-large.png',
    mimeType: 'image/png',
    buffer: Buffer.alloc((750 * 1024) + 1),
  })
  await page.getByRole('button', { name: 'Save Contact' }).click()

  await expect(page.getByText('Enter a first name.')).toBeVisible()
  await expect(page.getByText('Complete country code, area code, and phone number.')).toBeVisible()
  await expect(page.getByText('Photo must be 750 KB or smaller.')).toBeVisible()
  await expect(page.getByText('Complete every address field or leave address blank.')).toBeVisible()
})

test('searches contacts and paginates matching results', async ({ page, request }) => {
  const token = uniqueToken()

  for (let index = 1; index <= 21; index++) {
    await createContact(request, {
      firstName: `Search ${token} ${index.toString().padStart(2, '0')}`,
      email: `search.${token}.${index}@example.test`,
      number: `7400${index.toString().padStart(4, '0')}`,
    })
  }

  await page.goto('/contacts')
  await page.getByRole('searchbox', { name: 'Search' }).fill(`Search ${token}`)

  await expect(page.getByText('Page 1 of 2')).toBeVisible()
  await expect(page.getByRole('heading', { name: `Search ${token} 01` })).toBeVisible()

  await page.getByRole('button', { name: 'Next' }).click()

  await expect(page.getByText('Page 2 of 2')).toBeVisible()
  await expect(page.getByRole('heading', { name: `Search ${token} 21` })).toBeVisible()
})

test('loads edit values, updates data, and removes an existing photo', async ({ page, request }) => {
  const token = uniqueToken()
  const id = await createContact(request, {
    firstName: `Edit ${token}`,
    lastName: 'Before',
    email: `edit.${token}@example.test`,
    number: `661${token.slice(-7)}`,
    address: {
      country: 'Brazil',
      state: 'SP',
      city: 'Sao Paulo',
      neighborhood: 'Centro',
      postalCode: '01000-000',
    },
    photo: {
      content: Buffer.from([1, 2, 3]).toString('base64'),
      contentType: 'image/png',
      fileName: `edit-${token}.png`,
      size: 3,
    },
  })

  await page.goto(`/contacts/${id}/edit`)

  await expect(page.getByLabel('First Name')).toHaveValue(`Edit ${token}`)
  await expect(page.getByLabel('Last Name')).toHaveValue('Before')
  await expect(page.getByLabel('Email')).toHaveValue(`edit.${token}@example.test`)
  await expect(page.getByLabel('Country Code')).toHaveValue('+55')
  await expect(page.getByLabel('Area Code')).toHaveValue('11')
  await expect(page.getByRole('textbox', { name: 'Number', exact: true })).toHaveValue(`661${token.slice(-7)}`)
  await expect(page.getByLabel('Type')).toHaveValue('Mobile')
  await expect(page.getByLabel('Favorite')).toBeChecked()
  await expect(page.getByLabel('Country', { exact: true })).toHaveValue('Brazil')
  await expect(page.getByLabel('ZIP Code')).toHaveValue('01000-000')
  await expect(page.getByText(`edit-${token}.png`)).toBeVisible()

  await page.getByLabel('Last Name').fill('After')
  await page.getByRole('button', { name: 'Remove Photo' }).click()
  await page.getByRole('button', { name: 'Save Contact' }).click()

  await expect(page).toHaveURL(/\/contacts$/)

  const saved = await getContact(request, id)
  expect(saved.lastName).toBe('After')
  expect(saved.photo).toBeNull()
})

test('requires delete confirmation before removing a contact', async ({ page, request }) => {
  const token = uniqueToken()
  const name = `Delete ${token}`
  const id = await createContact(request, {
    firstName: name,
    email: `delete.${token}@example.test`,
    number: `772${token.slice(-7)}`,
  })

  await page.goto('/contacts')
  await page.getByRole('searchbox', { name: 'Search' }).fill(name)

  await expect(page.getByRole('heading', { name })).toBeVisible()
  await page.getByRole('button', { name: `Delete ${name}` }).click()
  await expect(page.getByRole('dialog', { name: 'Delete Contact' })).toBeVisible()

  await page.getByRole('button', { name: 'Cancel' }).click()
  await expect(page.getByRole('heading', { name })).toBeVisible()

  await page.getByRole('button', { name: `Delete ${name}` }).click()
  await page.getByRole('button', { name: 'Delete Contact' }).click()

  await expect(page.getByRole('heading', { name })).toBeHidden()
  const response = await request.get(`${apiBaseURL}/api/contacts/${id}`)
  expect(response.status()).toBe(404)
})

async function fillRequiredContactFields(
  page: Page,
  values: {
    readonly firstName: string
    readonly lastName?: string
    readonly email: string
    readonly number: string
  },
): Promise<void> {
  await page.getByLabel('First Name').fill(values.firstName)

  if (values.lastName) {
    await page.getByLabel('Last Name').fill(values.lastName)
  }

  await page.getByLabel('Email').fill(values.email)
  await page.getByLabel('Area Code').fill('11')
  await page.getByRole('textbox', { name: 'Number', exact: true }).fill(values.number)
}

async function createContact(
  request: APIRequestContext,
  values: {
    readonly firstName: string
    readonly lastName?: string
    readonly email: string
    readonly number: string
    readonly address?: AddressRequest
    readonly photo?: PhotoRequest
  },
): Promise<string> {
  const response = await request.post(`${apiBaseURL}/api/contacts`, {
    data: contactRequest(values),
  })
  const responseText = await response.text()
  expect(response.status(), responseText).toBe(201)

  const body = JSON.parse(responseText) as CreateContactResponse
  return body.id
}

async function getContact(request: APIRequestContext, id: string): Promise<ContactEditResponse> {
  const response = await request.get(`${apiBaseURL}/api/contacts/${id}`)
  expect(response.ok()).toBeTruthy()

  return await response.json() as ContactEditResponse
}

function contactRequest(values: {
  readonly firstName: string
  readonly lastName?: string
  readonly email: string
  readonly number: string
  readonly address?: AddressRequest
  readonly photo?: PhotoRequest
}): ContactRequest {
  return {
    firstName: values.firstName,
    lastName: values.lastName ?? null,
    email: values.email,
    phoneNumbers: [
      {
        countryCode: '+55',
        areaCode: '11',
        number: values.number,
        type: 'Mobile',
        isFavorite: true,
      },
    ],
    address: values.address ?? null,
    photo: values.photo ?? null,
    removePhoto: false,
  }
}

function uniqueToken(): string {
  return `${Date.now()}${test.info().parallelIndex}${Math.floor(Math.random() * 1000)}`
}
