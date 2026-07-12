import { defineConfig, devices } from '@playwright/test'

const port = Number(process.env.WEBUI_PORT ?? 5173)
const host = 'localhost'
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? `http://${host}:${port}`
const apiPort = Number(process.env.PLAYWRIGHT_API_PORT ?? 5087)
const apiBaseURL = process.env.PLAYWRIGHT_API_BASE_URL ?? `http://${host}:${apiPort}`
const connectionString =
  process.env.PHONEBOOK_E2E_CONNECTION_STRING ??
  process.env.PHONEBOOK_TEST_CONNECTION_STRING ??
  process.env.PHONEBOOK_CONNECTION_STRING ??
  ''

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  reporter: process.env.CI ? 'dot' : 'list',
  use: {
    baseURL,
    trace: 'on-first-retry',
  },
  webServer: [
    {
      command: `dotnet run --project ../src/Phonebook.Api/Phonebook.Api.csproj --no-launch-profile --urls ${apiBaseURL}`,
      url: `${apiBaseURL}/openapi/v1.json`,
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        PHONEBOOK_CONNECTION_STRING: connectionString,
      },
    },
    {
      command: `npm run dev -- --host ${host} --port ${port}`,
      url: baseURL,
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        VITE_API_BASE_URL: apiBaseURL,
      },
    },
  ],
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
})
