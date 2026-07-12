# Phonebook tasks

## Execution protocol

Implement these tasks with the `tlc-spec-driven` skill: activate it by name and
follow its Execute flow and Critical Rules. Do not proceed without the skill.

**Design**: `.specs/features/phonebook/design.md`
**Status**: Draft

## Test coverage matrix

Generated from `AGENTS.md`, the requested test strategy, and the empty current
test suite. Existing test samples: none.

| Code layer | Required test type | Coverage expectation | Location pattern | Run command |
| --- | --- | --- | --- | --- |
| Domain | unit | All domain invariants, all branches, and every related acceptance criterion. | `tests/Phonebook.DomainTests/**/*Tests.cs` | `dotnet test tests/Phonebook.DomainTests/Phonebook.DomainTests.csproj` |
| Application | unit | Validators and handlers cover success, validation failures, and query projection behavior. | `tests/Phonebook.ApplicationTests/**/*Tests.cs` | `dotnet test tests/Phonebook.ApplicationTests/Phonebook.ApplicationTests.csproj` |
| Infrastructure and persistence | integration | Migrations, constraints, cascade delete, search, pagination, and PostgreSQL behavior. | `tests/Phonebook.IntegrationTests/**/*Tests.cs` | `dotnet test tests/Phonebook.IntegrationTests/Phonebook.IntegrationTests.csproj` |
| API | integration | Every endpoint in scope has happy path, validation, and not-found coverage. | `tests/Phonebook.IntegrationTests/**/*Tests.cs` | `dotnet test tests/Phonebook.IntegrationTests/Phonebook.IntegrationTests.csproj` |
| WebUi | E2E | List, create, edit, delete confirmation, validation, photo removal, search, and pagination flows. | `WebUi/e2e/**/*.spec.ts` | `npx playwright test` |
| Project configuration | build | Builds, package restore, migrations, and generated-file cleanup are verified by build and test gates. | Project files | `dotnet build Phonebook.V2.slnx` |

## Gate check commands

| Gate level | When to use | Command |
| --- | --- | --- |
| Quick domain | Domain-only tasks | `dotnet test tests/Phonebook.DomainTests/Phonebook.DomainTests.csproj` |
| Quick application | Application-only tasks | `dotnet test tests/Phonebook.ApplicationTests/Phonebook.ApplicationTests.csproj` |
| Integration | Persistence or API tasks | `dotnet test tests/Phonebook.IntegrationTests/Phonebook.IntegrationTests.csproj` |
| WebUi | Frontend tasks | `npm --prefix WebUi run build && npx --prefix WebUi playwright test` |
| Full | Phase completion | `dotnet build Phonebook.V2.slnx && dotnet test Phonebook.V2.slnx && npm --prefix WebUi run build && npx --prefix WebUi playwright test` |

## Execution plan

Phases run sequentially. Tasks within each phase run in order.

```text
Phase 1: T01 -> T02 -> T03 -> T04
Phase 2: T05 -> T06 -> T07 -> T08 -> T09
Phase 3: T10 -> T11 -> T12 -> T13 -> T14
Phase 4: T15 -> T16 -> T17 -> T18
Phase 5: T19 -> T20 -> T21 -> T22
Phase 6: T23 -> T24 -> T25
```

This plan has more than 8 tasks. During Execute, offer batch sub-agents before
implementation, then run batches sequentially if the user accepts.

## Task breakdown

### T01: Create solution structure

**What**: Create `src`, `tests`, project files, references, and solution entries.
**Where**: Project root, `src/`, `tests/`
**Depends on**: None
**Requirement**: PB-16

Done when:

- [x] Domain, Application, Infrastructure, Api, DomainTests,
      ApplicationTests, and IntegrationTests projects exist.
- [x] Project references follow Clean Architecture dependency direction.
- [x] `dotnet build Phonebook.V2.slnx` succeeds.

**Tests**: build
**Gate**: Full

### T02: Remove template artifacts and add ignore rules

**What**: Remove Visual Studio weather template files and generated output from
the repository working tree.
**Where**: Project root
**Depends on**: T01
**Requirement**: PB-20

Done when:

- [x] Weather template files are removed.
- [x] `.gitignore` covers `bin/`, `obj/`, `.vs/`, Node artifacts, Playwright
      output, local env files, and coverage output.
- [x] Git index cleanup runs only when the workspace is inside a Git repository.

**Tests**: build
**Gate**: Full

### T03: Configure package dependencies

**What**: Add MediatR, FluentValidation, EF Core PostgreSQL, test packages, and
OpenAPI dependencies.
**Where**: Project files
**Depends on**: T01
**Requirement**: PB-16, PB-17, PB-21

Done when:

- [x] Required backend packages are referenced by the correct projects.
- [x] Test packages are referenced only by test projects.
- [x] Package restore and build succeed.

**Tests**: build
**Gate**: Full

### T04: Configure API composition root

**What**: Wire API services, MediatR, FluentValidation, Infrastructure, CORS,
controllers, and development-only OpenAPI.
**Where**: `src/Phonebook.Api/Program.cs`
**Depends on**: T03
**Requirement**: PB-16, PB-21

Done when:

- [x] OpenAPI maps only in development.
- [x] Application and Infrastructure services register through extension
      methods.
- [x] Build succeeds.

**Tests**: integration
**Gate**: Integration

### T05: Implement domain contact model

**What**: Create `Contact`, `PhoneNumber`, `Address`, `ContactPhoto`, and
`PhoneType`.
**Where**: `src/Phonebook.Domain`
**Depends on**: T01
**Requirement**: PB-01, PB-03, PB-04, PB-08, PB-13, PB-14

Done when:

- [x] Domain model enforces 1 to 10 phone numbers.
- [x] Domain model rejects duplicate phone numbers within a contact.
- [x] Domain model supports favorite phone selection.
- [x] Domain model supports optional address and photo.
- [x] Domain tests cover each invariant.

**Tests**: unit
**Gate**: Quick domain

### T06: Implement domain update and delete behavior

**What**: Add domain methods for updating contact details, replacing phones,
setting/removing address, and setting/removing photo.
**Where**: `src/Phonebook.Domain`
**Depends on**: T05
**Requirement**: PB-09, PB-10, PB-11, PB-12, PB-14

Done when:

- [x] Update methods preserve contact invariants.
- [x] Removing all phones is rejected.
- [x] Removing photo clears photo state.
- [x] Domain tests cover update and removal cases.

**Tests**: unit
**Gate**: Quick domain

### T07: Implement application contracts and DTOs

**What**: Create persistence contracts, command/query DTOs, list item DTOs, and
edit DTOs.
**Where**: `src/Phonebook.Application`
**Depends on**: T05
**Requirement**: PB-01, PB-06, PB-09, PB-16

Done when:

- [x] Application contracts do not depend on Infrastructure.
- [x] Query DTOs do not expose persistence entities.
- [x] Build succeeds.

**Tests**: unit
**Gate**: Quick application

### T08: Implement application validators

**What**: Add FluentValidation validators for commands and queries.
**Where**: `src/Phonebook.Application`
**Depends on**: T07
**Requirement**: PB-02, PB-03, PB-04, PB-05, PB-13, PB-14, PB-15

Done when:

- [x] Validators cover required first name, email format, phone count, phone
      type, favorite count, photo size, address completeness, postal code,
      search, and pagination.
- [x] Application tests cover accepted and rejected values.

**Tests**: unit
**Gate**: Quick application

### T09: Implement postal-code validation strategy

**What**: Add extensible postal-code rules for Brazil, United States, and
fallback countries.
**Where**: `src/Phonebook.Application`
**Depends on**: T08
**Requirement**: PB-15

Done when:

- [x] Brazil rule validates configured Brazilian postal-code format.
- [x] United States rule validates configured United States postal-code format.
- [x] Other countries require non-empty postal code.
- [x] Tests cover each rule.

**Tests**: unit
**Gate**: Quick application

### T10: Implement EF Core model configuration

**What**: Create `PhonebookDbContext` and entity configurations.
**Where**: `src/Phonebook.Infrastructure`
**Depends on**: T06, T07
**Requirement**: PB-05, PB-12, PB-13, PB-17

Done when:

- [x] Email has a unique filtered index for non-null values.
- [x] Phone uniqueness by contact is enforced.
- [x] Contact cascades to phones, address, and photo.
- [x] Photo uses a separate table.
- [x] Integration tests verify constraints and cascade behavior.

**Tests**: integration
**Gate**: Integration

### T11: Add initial EF Core migration

**What**: Generate and review the initial PostgreSQL migration.
**Where**: `src/Phonebook.Infrastructure/Migrations`
**Depends on**: T10
**Requirement**: PB-17

Done when:

- [x] Migration creates contacts, phone numbers, addresses, and contact photos.
- [x] Migration includes expected indexes and cascade deletes.
- [x] Integration tests apply migrations successfully.

**Tests**: integration
**Gate**: Integration

### T12: Implement integration test database base

**What**: Create local PostgreSQL test database setup using
`PHONEBOOK_TEST_CONNECTION_STRING`.
**Where**: `tests/Phonebook.IntegrationTests`
**Depends on**: T11
**Requirement**: PB-17

Done when:

- [x] Test setup recreates the database.
- [x] Test setup applies migrations, not `EnsureCreated`.
- [x] Tests fail clearly when the connection string is missing or migrations
      fail.
- [x] Pending model changes cause the suite to fail.

**Tests**: integration
**Gate**: Integration

### T13: Implement application command handlers

**What**: Implement create, update, and delete handlers.
**Where**: `src/Phonebook.Application`
**Depends on**: T08, T10, T12
**Requirement**: PB-01, PB-05, PB-10, PB-11, PB-12

Done when:

- [x] Handlers validate inputs and business preconditions before persistence.
- [x] Handlers do not call query handlers or other command handlers.
- [x] Application and integration tests cover success and failure paths.

**Tests**: integration
**Gate**: Integration

### T14: Implement application query handlers

**What**: Implement get-for-edit and list query handlers.
**Where**: `src/Phonebook.Application`
**Depends on**: T08, T10, T12
**Requirement**: PB-06, PB-07, PB-08, PB-09

Done when:

- [x] Queries use no-tracking projections.
- [x] List query supports search, pagination, and display phone selection.
- [x] Get-for-edit returns all editable fields.
- [x] Integration tests cover projection, empty results, search, pagination,
      and phone priority.

**Tests**: integration
**Gate**: Integration

### T15: Implement REST endpoints

**What**: Add contact controller endpoints and API models.
**Where**: `src/Phonebook.Api`
**Depends on**: T13, T14
**Requirement**: PB-01, PB-06, PB-09, PB-10, PB-12, PB-21

Done when:

- [x] Endpoints map to the CQRS requests in the design.
- [x] Validation errors return HTTP 400.
- [x] Missing contacts return HTTP 404.
- [x] Create returns HTTP 201.
- [x] Integration tests cover every endpoint.

**Tests**: integration
**Gate**: Integration

### T16: Create Vite Vue WebUi

**What**: Scaffold Vue under `WebUi/` and configure build, routing, and API base
URL.
**Where**: `WebUi/`
**Depends on**: T15
**Requirement**: PB-18

Done when:

- [x] Vue app builds.
- [x] API access lives in feature API modules or shared API utilities.
- [x] Basic routing exists for list, create, and edit screens.

**Tests**: E2E
**Gate**: WebUi

### T17: Implement contact list UI

**What**: Build list screen with search, pagination, display phone, email, photo
fallback, edit action, and delete action.
**Where**: `WebUi/src/features/contacts`
**Depends on**: T16
**Requirement**: PB-06, PB-07, PB-08, PB-18

Done when:

- [x] Search and pagination sync to URL query string.
- [x] Empty state renders when no results exist.
- [x] Icon buttons have accessible names.
- [x] Playwright covers list, search, and pagination.

**Tests**: E2E
**Gate**: WebUi

### T18: Implement shared contact form UI

**What**: Build reusable create/edit form for names, email, phones, address, and
photo.
**Where**: `WebUi/src/features/contacts`
**Depends on**: T16
**Requirement**: PB-01, PB-03, PB-13, PB-14, PB-18

Done when:

- [x] Form supports dynamic phone add/remove up to 10.
- [x] Form requires at least one phone.
- [x] Form supports favorite phone selection.
- [x] Form validates photo size before submit.
- [x] Form shows inline errors and focuses first error on submit.

**Tests**: E2E
**Gate**: WebUi

### T19: Implement create contact UI flow

**What**: Connect shared form to create endpoint.
**Where**: `WebUi/src/features/contacts`
**Depends on**: T18
**Requirement**: PB-01, PB-18, PB-19

Done when:

- [x] User can create a valid contact.
- [x] Validation failures render inline.
- [x] Playwright covers create success and required-field failures.

**Tests**: E2E
**Gate**: WebUi

### T20: Implement edit contact UI flow

**What**: Load contact data for edit and connect shared form to update endpoint.
**Where**: `WebUi/src/features/contacts`
**Depends on**: T18
**Requirement**: PB-09, PB-10, PB-11, PB-18, PB-19

Done when:

- [x] Stored values populate the correct fields.
- [x] User can update contact data.
- [x] User can remove an existing photo.
- [x] Playwright covers edit and photo removal.

**Tests**: E2E
**Gate**: WebUi

### T21: Implement delete confirmation UI flow

**What**: Add confirmation modal and delete request handling.
**Where**: `WebUi/src/features/contacts`
**Depends on**: T17
**Requirement**: PB-12, PB-18, PB-19

Done when:

- [x] Delete does not run before confirmation.
- [x] Cancel leaves the contact visible.
- [x] Confirm removes the contact from the list.
- [x] Playwright covers cancel and confirm paths.

**Tests**: E2E
**Gate**: WebUi

### T22: Install and configure Playwright

**What**: Add Playwright configuration, browser install instructions, and E2E
test scripts.
**Where**: `WebUi/`
**Depends on**: T16
**Requirement**: PB-19

Done when:

- [x] Playwright is installed.
- [x] E2E command starts or targets the required local services.
- [x] Test output and reports are ignored by `.gitignore`.

**Tests**: E2E
**Gate**: WebUi

### T23: Add end-to-end regression coverage

**What**: Complete Playwright scenarios across create, list, edit, delete,
validation, search, pagination, and photo removal.
**Where**: `WebUi/e2e`
**Depends on**: T19, T20, T21, T22
**Requirement**: PB-19

Done when:

- [x] E2E tests cover all P1 UI workflows.
- [x] E2E tests cover the important P2 photo and address validations.
- [x] Playwright suite passes.

**Tests**: E2E
**Gate**: WebUi

### T24: Add developer documentation

**What**: Update README or docs with setup, PostgreSQL test configuration,
migrations, and test commands.
**Where**: `README.md`, `docs/`
**Depends on**: T23
**Requirement**: PB-17, PB-19

Done when:

- [x] Documentation explains `PHONEBOOK_TEST_CONNECTION_STRING`.
- [x] Documentation explains migration workflow.
- [x] Documentation lists backend, frontend, integration, and E2E commands.

**Tests**: build
**Gate**: Full

### T25: Run final verification

**What**: Run full build, test, WebUi, and E2E gates and complete verifier pass.
**Where**: Project root
**Depends on**: T24
**Requirement**: PB-01 through PB-21

Done when:

- [x] Full gate passes.
- [x] Spec acceptance criteria are mapped to passing tests.
- [x] Verifier writes `.specs/features/phonebook/validation.md`.

**Tests**: all
**Gate**: Full

## Diagram-definition cross-check

| Task | Depends on | Diagram shows | Status |
| --- | --- | --- | --- |
| T01 | None | Start of Phase 1 | Match |
| T02 | T01 | T01 -> T02 | Match |
| T03 | T01 | T01 -> T03 through Phase 1 order | Match |
| T04 | T03 | T03 -> T04 | Match |
| T05 | T01 | Phase 2 after Phase 1 | Match |
| T06 | T05 | T05 -> T06 | Match |
| T07 | T05 | T06 -> T07 by phase order, with T05 available | Match |
| T08 | T07 | T07 -> T08 | Match |
| T09 | T08 | T08 -> T09 | Match |
| T10 | T06, T07 | Phase 3 after Phase 2 | Match |
| T11 | T10 | T10 -> T11 | Match |
| T12 | T11 | T11 -> T12 | Match |
| T13 | T08, T10, T12 | T12 -> T13 with prior phase dependencies available | Match |
| T14 | T08, T10, T12 | T13 -> T14 by phase order, with dependencies available | Match |
| T15 | T13, T14 | Phase 4 after Phase 3 | Match |
| T16 | T15 | T15 -> T16 | Match |
| T17 | T16 | T16 -> T17 | Match |
| T18 | T16 | T17 -> T18 by phase order, with T16 available | Match |
| T19 | T18 | Phase 5 after Phase 4 | Match |
| T20 | T18 | T19 -> T20 by phase order, with T18 available | Match |
| T21 | T17 | T20 -> T21 by phase order, with T17 available | Match |
| T22 | T16 | T21 -> T22 by phase order, with T16 available | Match |
| T23 | T19, T20, T21, T22 | Phase 6 after Phase 5 | Match |
| T24 | T23 | T23 -> T24 | Match |
| T25 | T24 | T24 -> T25 | Match |

## Test co-location validation

| Task | Code layer created or modified | Matrix requires | Task says | Status |
| --- | --- | --- | --- | --- |
| T01 | Project configuration | build | build | OK |
| T02 | Project configuration | build | build | OK |
| T03 | Project configuration | build | build | OK |
| T04 | API composition | integration | integration | OK |
| T05 | Domain | unit | unit | OK |
| T06 | Domain | unit | unit | OK |
| T07 | Application | unit | unit | OK |
| T08 | Application | unit | unit | OK |
| T09 | Application | unit | unit | OK |
| T10 | Infrastructure | integration | integration | OK |
| T11 | Infrastructure | integration | integration | OK |
| T12 | Infrastructure and tests | integration | integration | OK |
| T13 | Application and persistence workflow | integration | integration | OK |
| T14 | Application and persistence workflow | integration | integration | OK |
| T15 | API | integration | integration | OK |
| T16 | WebUi | E2E | E2E | OK |
| T17 | WebUi | E2E | E2E | OK |
| T18 | WebUi | E2E | E2E | OK |
| T19 | WebUi | E2E | E2E | OK |
| T20 | WebUi | E2E | E2E | OK |
| T21 | WebUi | E2E | E2E | OK |
| T22 | WebUi | E2E | E2E | OK |
| T23 | WebUi | E2E | E2E | OK |
| T24 | Documentation | build | build | OK |
| T25 | All layers | all | all | OK |
