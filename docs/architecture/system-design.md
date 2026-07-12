# Phonebook system design

## Overview

Phonebook is a Clean Architecture web application for managing contacts. It
contains a REST API, a Vue frontend, PostgreSQL persistence, CQRS application
use cases, and layered automated tests.

This document summarizes the implementation plan. The traceable feature
specification and task plan live in `.specs/features/phonebook/`.

## Requirements summary

- Create, list, search, edit, and delete contacts.
- Require first name and at least one phone number.
- Allow optional last name, email, photo, and address.
- Enforce at most 10 phone numbers per contact.
- Enforce phone uniqueness within a contact.
- Enforce unique non-null email values.
- Display favorite phone first, or use `Mobile`, `Residential`, `Commercial`
  priority.
- Store photos in PostgreSQL in a separate one-to-one table.
- Validate photo size at 750 KB maximum.
- Require all address fields when any address field is present.
- Validate postal codes for Brazil and the United States, with fallback
  non-empty validation for other countries.
- Use PostgreSQL and EF Core migrations.
- Use local PostgreSQL for integration tests, without Docker.
- Use Playwright for browser E2E tests.

## Architecture

The solution uses separate projects for each architectural concern:

- `Phonebook.Domain`: domain entities, value objects, enums, and invariants.
- `Phonebook.Application`: CQRS requests, handlers, DTOs, validators, and
  persistence contracts.
- `Phonebook.Infrastructure`: EF Core, PostgreSQL, migrations, and contract
  implementations.
- `Phonebook.Api`: REST endpoints, API models, composition root, and
  development-only OpenAPI.
- `WebUi`: Vue and Vite frontend.
- `Phonebook.DomainTests`: domain unit tests.
- `Phonebook.ApplicationTests`: application unit tests.
- `Phonebook.IntegrationTests`: PostgreSQL and API integration tests.

Dependencies point inward. Domain has no dependency on other layers.
Application depends on Domain. Infrastructure depends on Application and Domain.
API depends on Application and Infrastructure. WebUi talks to the API only.

## Data model

Contact is the aggregate root. Phone numbers, address, and photo are dependent
data.

- `Contact` stores identity, first name, optional last name, optional email, and
  timestamps.
- `PhoneNumber` stores country code, area code, number, normalized number, type,
  and favorite flag.
- `Address` stores country, state, city, neighborhood, and postal code.
- `ContactPhoto` stores binary content, content type, file name, and size.

The database must enforce:

- Unique non-null contact email.
- Unique normalized phone number per contact.
- Cascade delete from contact to phone numbers, address, and photo.
- One-to-one relationship between contact and photo.

## API design

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/contacts` | List contacts with search and pagination. |
| `GET` | `/api/contacts/{id}` | Retrieve a contact for editing. |
| `POST` | `/api/contacts` | Create a contact. |
| `PUT` | `/api/contacts/{id}` | Update a contact. |
| `DELETE` | `/api/contacts/{id}` | Hard-delete a contact. |

The API returns validation failures as HTTP 400 and missing contacts as HTTP
404. It exposes Swagger/OpenAPI in development only.

## Frontend design

The frontend starts with the working application, not a landing page. The first
screen is the contact list.

Required screens:

- Contact list with search, pagination, photo or fallback avatar, name, selected
  phone, email, edit action, and delete action.
- Create contact screen using the shared contact form.
- Edit contact screen using the shared contact form and preloaded database
  values.
- Delete confirmation modal.

The UI must keep search and pagination in the URL query string. Forms must use
labels, inline errors, visible focus states, and accessible buttons.

## Testing strategy

Use the following test layers:

- Domain tests for business invariants.
- Application tests for validators, handlers, and DTO behavior.
- Integration tests against local PostgreSQL using
  `PHONEBOOK_TEST_CONNECTION_STRING`.
- Playwright E2E tests for browser workflows.

Integration tests must recreate the database and apply EF Core migrations. They
must not use `EnsureCreated`. They must fail when migrations cannot be applied
or when the EF Core model has pending changes not represented by a migration.

## Operational considerations

- Configure PostgreSQL connection strings through environment or app settings.
- Keep Swagger/OpenAPI disabled outside development.
- Keep uploaded photo size bounded to protect database storage.
- Avoid loading full entities in list queries. Project directly to DTOs.
- Use normalized phone digits for search and uniqueness checks.
- Keep WebUi API base URL configurable for local development and tests.

## Trade-offs

| Decision | Benefit | Cost |
| --- | --- | --- |
| Clean Architecture projects | Strong boundaries and focused tests. | More setup than a single project. |
| PostgreSQL photo storage | Simple deployment with one datastore. | Larger database rows and backups. |
| Local PostgreSQL integration tests | Tests real database behavior without Docker. | Requires developer machine setup. |
| Reject invalid pagination | Clear API contract. | Clients must handle validation errors. |
| Initial country postal rules | Practical MVP validation. | Not complete international coverage. |

## Implementation references

- Specification: `.specs/features/phonebook/spec.md`
- Design: `.specs/features/phonebook/design.md`
- Tasks: `.specs/features/phonebook/tasks.md`
- Frontend design system: `docs/design/DESIGN.md`
