import type { PlaywrightTestConfig } from './WebUi/node_modules/@playwright/test'

const webUiPort = Number(process.env.WEBUI_PORT ?? 5173)
const host = 'localhost'
const webUiBaseURL = process.env.PLAYWRIGHT_BASE_URL ?? `http://${host}:${webUiPort}`
const apiPort = Number(process.env.PLAYWRIGHT_API_PORT ?? 5087)
const apiBaseURL = process.env.PLAYWRIGHT_API_BASE_URL ?? `http://${host}:${apiPort}`
const connectionString =
  process.env.PHONEBOOK_E2E_CONNECTION_STRING ??
  process.env.PHONEBOOK_TEST_CONNECTION_STRING ??
  process.env.PHONEBOOK_CONNECTION_STRING ??
  ''

const config: PlaywrightTestConfig = {
  testDir: './WebUi/e2e',
  fullyParallel: true,
  reporter: process.env.CI ? 'dot' : 'list',
  use: {
    baseURL: webUiBaseURL,
    trace: 'on-first-retry',
  },
  webServer: [
    {
      command: `dotnet run --project src/Phonebook.Api/Phonebook.Api.csproj --no-launch-profile --urls ${apiBaseURL}`,
      url: `${apiBaseURL}/openapi/v1.json`,
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        PHONEBOOK_CONNECTION_STRING: connectionString,
      },
    },
    {
      command: `npm --prefix WebUi run dev -- --host ${host} --port ${webUiPort}`,
      url: webUiBaseURL,
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        VITE_API_BASE_URL: apiBaseURL,
      },
    },
  ],
}

export default config
