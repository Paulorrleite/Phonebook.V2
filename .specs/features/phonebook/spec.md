# Phonebook specification

## Problem statement

The project must replace the default Visual Studio template with a production-ready
phonebook application. Users need to create, list, edit, and delete contacts while
preserving domain rules for contact identity, phone numbers, addresses, and photos.

The system must use Clean Architecture, CQRS, PostgreSQL, a REST API, and a Vue
frontend. It must include domain, application, integration, and Playwright E2E
tests.

## Goals

- [ ] Users can manage contacts through a Vue frontend backed by REST endpoints.
- [ ] Contact data is persisted in PostgreSQL with explicit constraints and
      cascade delete behavior.
- [ ] Domain and application rules are enforced through domain models,
      MediatR handlers, and FluentValidation.
- [ ] Tests cover domain rules, application use cases, PostgreSQL integration,
      and browser workflows.
- [ ] Visual Studio template artifacts are removed or ignored safely.

## Out of scope

| Feature | Reason |
| --- | --- |
| Authentication and authorization | The current scope describes a single-user phonebook. |
| Full international postal-code validation | The user requested Brazil and United States validation first, with fallback validation for other countries. |
| Cloud/object photo storage | Photos must be stored in PostgreSQL. |
| Soft delete | The user explicitly requested hard delete. |
| Import/export contacts | Not part of the requested user workflows. |
| Native mobile app | The requested client is a Vue web application. |

## Assumptions and open questions

| Assumption or decision | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| Optional email uniqueness | Multiple contacts may have no email; non-null email values must be unique. | The user confirmed this behavior. | yes |
| Phone uniqueness | Phone number uniqueness applies within each contact, not globally. | The user confirmed uniqueness by contact. | yes |
| Display phone selection | Use favorite phone first; otherwise use `Mobile`, `Residential`, then `Commercial`. | The user confirmed this priority order. | yes |
| Pagination validation | Reject invalid page and page size values through validation instead of silently normalizing. | This keeps API behavior explicit and testable. | yes |
| Postal-code fallback | For countries other than Brazil and the United States, require only a non-empty string. | The user confirmed full international validation is not required now. | yes |
| Photo storage | Store photo metadata and binary content in a separate `ContactPhoto` table. | The user confirmed a separate table. | yes |
| API exposure | Swagger/OpenAPI is development-only. | The user confirmed this requirement. | yes |
| Git cleanup | Git index cleanup is conditional because this workspace currently has no `.git` repository. | The local project is not currently a Git repository. | yes |

Open questions: none. Invalid pagination values are rejected because the project
requires clear validation behavior.

## User stories

### P1: Create a contact

User story: As a phonebook user, I want to create a contact with at least one
phone number so that I can store people I need to reach.

Why P1: This is the core write workflow and establishes the domain model.

Acceptance criteria:

1. WHEN the user submits a contact with a first name and one valid phone number
   THEN the system SHALL persist the contact and return the created contact ID.
2. WHEN the user omits first name THEN the system SHALL reject the request with
   a validation error.
3. WHEN the user submits zero phone numbers THEN the system SHALL reject the
   request with a validation error.
4. WHEN the user submits more than 10 phone numbers THEN the system SHALL reject
   the request with a validation error.
5. WHEN the user submits duplicate phone numbers for the same contact THEN the
   system SHALL reject the request with a validation error.
6. WHEN the user submits a non-null email already used by another contact THEN
   the system SHALL reject the request.
7. WHEN the user omits last name, email, photo, and address THEN the system
   SHALL create the contact.

Independent test: Create a contact from the UI and verify it appears in the
list with the selected display phone.

### P1: List and search contacts

User story: As a phonebook user, I want to list and search contacts so that I
can find contact information quickly.

Why P1: Listing is the primary read workflow.

Acceptance criteria:

1. WHEN the user opens the contact list THEN the system SHALL show contacts
   using default pagination of `page = 1` and `pageSize = 20`.
2. WHEN a contact has a favorite phone THEN the list SHALL display the favorite
   phone.
3. WHEN a contact has no favorite phone THEN the list SHALL display the first
   available phone by type priority: `Mobile`, `Residential`, `Commercial`.
4. WHEN the user searches by first name, last name, email, or phone digits THEN
   the system SHALL return matching contacts.
5. WHEN the user searches text fields THEN matching SHALL be case-insensitive.
6. WHEN the user searches phone numbers THEN matching SHALL use normalized
   digits.
7. WHEN the list has no matching contacts THEN the UI SHALL show an empty state.

Independent test: Seed contacts with different names, emails, phone types, and
phone values; search each supported field and verify the expected rows.

### P1: Edit a contact

User story: As a phonebook user, I want to edit a contact so that I can keep
stored information current.

Why P1: Editing is required by the specification and validates read-for-edit
behavior.

Acceptance criteria:

1. WHEN the user opens the edit screen THEN the system SHALL retrieve the stored
   contact and display each value in its corresponding field.
2. WHEN the user updates valid contact data THEN the system SHALL persist the
   changes.
3. WHEN the user removes an existing photo THEN the system SHALL delete the
   related `ContactPhoto` row.
4. WHEN the user removes a phone number and at least one phone remains THEN the
   system SHALL persist the remaining phone numbers.
5. WHEN the user removes all phone numbers THEN the system SHALL reject the
   update with a validation error.
6. WHEN the user partially fills an address THEN the system SHALL reject the
   update until all address fields are present.

Independent test: Create a contact with all optional fields, edit each section,
save, reload the edit screen, and verify persisted values.

### P1: Delete a contact

User story: As a phonebook user, I want to delete a contact with confirmation
so that I can remove contacts intentionally.

Why P1: Deletion is explicitly required.

Acceptance criteria:

1. WHEN the user chooses delete THEN the UI SHALL show a confirmation dialog
   before sending the delete request.
2. WHEN the user confirms delete THEN the system SHALL hard-delete the contact.
3. WHEN a contact is deleted THEN the database SHALL cascade delete related
   photo, phone numbers, and address.
4. WHEN the user cancels delete THEN the system SHALL leave the contact
   unchanged.

Independent test: Delete a contact from the UI and verify it no longer appears
in the list and related rows are removed in integration tests.

### P2: Manage optional photo

User story: As a phonebook user, I want to upload or remove a contact photo so
that contacts are easier to identify.

Why P2: Photo is optional but required in the list data model.

Acceptance criteria:

1. WHEN the user uploads a photo up to 750 KB THEN the system SHALL store the
   binary content, content type, file name, and size in `ContactPhoto`.
2. WHEN the user uploads a photo larger than 750 KB THEN the system SHALL reject
   the request with a validation error.
3. WHEN a contact has no photo THEN the list UI SHALL show a stable fallback
   avatar.

Independent test: Upload a valid image and verify it appears after reload; try
an oversized file and verify inline validation.

### P2: Validate address by country

User story: As a phonebook user, I want address validation to match the selected
country where supported so that postal codes are stored consistently.

Why P2: Address is optional, but validation rules affect create and edit.

Acceptance criteria:

1. WHEN country is Brazil THEN the postal code SHALL match the configured Brazil
   rule.
2. WHEN country is United States THEN the postal code SHALL match the configured
   United States rule.
3. WHEN country is not Brazil or United States THEN postal code SHALL be a
   non-empty string.
4. WHEN any address field is provided THEN country, state, city, neighborhood,
   and postal code SHALL all be required.

Independent test: Submit addresses for Brazil, United States, and an unsupported
country and verify accepted and rejected postal-code values.

## Edge cases

- WHEN page is less than 1 THEN the query validator SHALL reject it.
- WHEN page size is less than 1 or greater than 100 THEN the query validator
  SHALL reject it.
- WHEN search contains punctuation in a phone number THEN the query SHALL
  normalize digits before matching.
- WHEN a contact has multiple favorite phones in a request THEN the system SHALL
  reject the request.
- WHEN PostgreSQL migrations cannot be applied in integration tests THEN the
  integration suite SHALL fail.
- WHEN EF Core model changes are not represented by a migration THEN the
  integration suite SHALL fail.
- WHEN Swagger/OpenAPI runs outside development THEN it SHALL not be exposed by
  default.

## Implicit-requirement dimensions sweep

| Dimension | Resolution |
| --- | --- |
| Input validation and bounds | Covered by FluentValidation and domain invariants for names, email, phone counts, phone type, photo size, address completeness, postal code, search, and pagination. |
| Failure and partial-failure states | Commands must validate before persistence and use one transaction per write use case. |
| Idempotency, retry, and duplicate handling | Duplicate email and duplicate phone rules are explicit. No idempotency keys are required for this local CRUD scope. |
| Auth boundaries and rate limits | N/A because authentication and authorization are out of scope. |
| Concurrency and ordering | Use database constraints for unique email and phone uniqueness. No optimistic concurrency token is required for MVP. |
| Data lifecycle and expiry | Hard delete contacts and cascade dependent rows. No archival or expiry. |
| Observability | Use normal ASP.NET Core logging. No custom metrics are required for MVP. |
| External-dependency failure | Local PostgreSQL is the only required external dependency. Integration tests fail when unavailable or migrations fail. |
| State-transition integrity | Contact create, update, and delete rules are guarded by domain methods, validators, and database constraints. |

## Requirement traceability

| Requirement ID | Story | Phase | Status |
| --- | --- | --- | --- |
| PB-01 | Create contact | Design | Complete |
| PB-02 | Required first name | Design | Complete |
| PB-03 | Phone count 1 to 10 | Design | Complete |
| PB-04 | Phone uniqueness by contact | Design | Complete |
| PB-05 | Optional unique email | Design | Complete |
| PB-06 | List contacts | Design | Complete |
| PB-07 | Search contacts | Design | Complete |
| PB-08 | Phone display selection | Design | Complete |
| PB-09 | Edit contact retrieval | Design | Complete |
| PB-10 | Update contact | Design | Complete |
| PB-11 | Remove photo | Design | Complete |
| PB-12 | Delete contact with cascade | Design | Complete |
| PB-13 | Photo storage and size limit | Design | Complete |
| PB-14 | Address completeness | Design | Complete |
| PB-15 | Postal-code validation | Design | Complete |
| PB-16 | Clean Architecture solution structure | Design | Complete |
| PB-17 | PostgreSQL migrations and integration tests | Design | Complete |
| PB-18 | Vue WebUi workflows | Design | Complete |
| PB-19 | Playwright E2E coverage | Design | Complete |
| PB-20 | Template cleanup | Design | Complete |
| PB-21 | Development-only OpenAPI | Design | Complete |

Coverage: 21 total, 21 mapped to planned tasks, 0 unmapped.

## Success criteria

- [ ] A user can create, list, search, edit, and delete contacts from the Vue UI.
- [ ] REST endpoints expose only the contact workflows in scope.
- [ ] Domain, application, integration, and E2E tests pass.
- [ ] Integration tests use local PostgreSQL, apply EF Core migrations, and fail
      on pending model changes.
- [ ] Template artifacts are removed or ignored.
