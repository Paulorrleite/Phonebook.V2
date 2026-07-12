# Phonebook V2

Phonebook V2 is a Clean Architecture contact-management application. It uses an
ASP.NET Core REST API, MediatR CQRS handlers, FluentValidation, EF Core with
PostgreSQL, and a Vue Vite WebUi.

## Prerequisites

- .NET 10 SDK
- Node.js and npm
- PostgreSQL running locally or on a reachable host
- Playwright Chromium browser payload

Install the WebUi dependencies and Playwright browser from the repository root:

```powershell
npm --prefix WebUi install
npm --prefix WebUi run playwright:install
```

## Connection strings

The application reads the database connection from `ConnectionStrings:Phonebook`
or `PHONEBOOK_CONNECTION_STRING`.

Integration tests read `PHONEBOOK_TEST_CONNECTION_STRING`. The integration test
base recreates the configured test database and applies EF Core migrations. It
does not use Docker and it does not use `EnsureCreated`.

E2E tests use this precedence for the API connection string:

1. `PHONEBOOK_E2E_CONNECTION_STRING`
2. `PHONEBOOK_TEST_CONNECTION_STRING`
3. `PHONEBOOK_CONNECTION_STRING`

Set the test connection string before running integration or E2E tests:

```powershell
$env:PHONEBOOK_TEST_CONNECTION_STRING='Host=localhost;Port=5432;Database=phonebook_v2_tests;Username=postgres;Password=<password>'
```

## Run the application

Start the API:

```powershell
$env:PHONEBOOK_CONNECTION_STRING='Host=localhost;Port=5432;Database=phonebook_v2;Username=postgres;Password=<password>'
dotnet run --project src/Phonebook.Api/Phonebook.Api.csproj
```

Start the WebUi:

```powershell
$env:VITE_API_BASE_URL='https://localhost:7069'
npm --prefix WebUi run dev
```

Open `http://localhost:5173`.

## Database migrations

Add a migration after changing the EF Core model:

```powershell
dotnet ef migrations add <MigrationName> --project src/Phonebook.Infrastructure --startup-project src/Phonebook.Api
```

Apply migrations to the configured application database:

```powershell
dotnet ef database update --project src/Phonebook.Infrastructure --startup-project src/Phonebook.Api
```

The integration suite applies migrations automatically to the test database and
fails when migrations cannot be applied or when the EF Core model has pending
changes that are not represented by a migration.

## Test commands

Run the backend build:

```powershell
dotnet build Phonebook.V2.slnx
```

Run domain tests:

```powershell
dotnet test tests/Phonebook.DomainTests/Phonebook.DomainTests.csproj
```

Run application tests:

```powershell
dotnet test tests/Phonebook.ApplicationTests/Phonebook.ApplicationTests.csproj
```

Run integration tests:

```powershell
$env:PHONEBOOK_TEST_CONNECTION_STRING='Host=localhost;Port=5432;Database=phonebook_v2_tests;Username=postgres;Password=<password>'
dotnet test tests/Phonebook.IntegrationTests/Phonebook.IntegrationTests.csproj
```

Build the WebUi:

```powershell
npm --prefix WebUi run build
```

Run Playwright E2E tests. The Playwright config starts the API on
`http://localhost:5087` and the WebUi on `http://localhost:5173`.

```powershell
$env:PHONEBOOK_TEST_CONNECTION_STRING='Host=localhost;Port=5432;Database=phonebook_v2_tests;Username=postgres;Password=<password>'
npm --prefix WebUi run e2e
```

The repository root also includes a Playwright config for the implementation
plan gate command:

```powershell
$env:PHONEBOOK_TEST_CONNECTION_STRING='Host=localhost;Port=5432;Database=phonebook_v2_tests;Username=postgres;Password=<password>'
npx --prefix WebUi playwright test
```

## API notes

Swagger/OpenAPI is enabled only in the development environment. It is not exposed
by default in production.
