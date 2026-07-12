# Project state

## Decisions

| ID | Status | Decision | Rationale | Date |
| --- | --- | --- | --- | --- |
| AD-001 | active | Use Clean Architecture with separate Domain, Application, Infrastructure, Api, WebUi, and test projects. | The phonebook feature has domain rules, CQRS use cases, persistence, REST API, UI, and multiple test layers. | 2026-07-11 |
| AD-002 | active | Use MediatR for CQRS request dispatch and FluentValidation for input validation. | These libraries were selected by the user and define the application use-case pattern. | 2026-07-11 |
| AD-003 | active | Use PostgreSQL with EF Core migrations for application and integration-test database setup. | Integration tests must use a local PostgreSQL database, must not use Docker, and must fail when migrations cannot be applied or model changes are pending. | 2026-07-11 |
| AD-004 | active | Create the Vue frontend as a Vite app under `WebUi/`. | The user requires a Vue UI while keeping REST API access separate from backend business rules. | 2026-07-11 |

## Handoff

No implementation has started. Planning documents live under
`.specs/features/phonebook/` and `docs/`.
