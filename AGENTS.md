# AGENTS.md

## Goal

Produce clean, maintainable, production-ready code.

Never sacrifice readability for cleverness.

---

# General Rules

* Think before editing.
* Prefer the smallest correct change.
* Preserve existing architecture unless explicitly asked to refactor.
* Do not introduce unnecessary abstractions.
* Avoid breaking public APIs.
* If requirements are ambiguous, ask instead of guessing.
* Inspect the existing implementation before creating new patterns or abstractions.
* Reuse existing project conventions when they are consistent with this document.
* Do not create speculative infrastructure for hypothetical future requirements.

---

# Code Style

## C#

* Target .NET 10.
* Prefer async/await for asynchronous I/O operations.
* Use `ConfigureAwait(false)` in library code.
* Prefer LINQ only when readability improves.
* Prefer pattern matching when it makes intent clearer.
* Prefer collection expressions when appropriate.
* Avoid unnecessary allocations.
* Use meaningful variable names.
* Keep methods small and focused.
* Prefer explicit domain intent over generic CRUD terminology.
* Propagate `CancellationToken` through asynchronous application and infrastructure operations when supported.
* Do not use async methods when no asynchronous work is performed.
* Avoid unnecessary materialization of collections.
* Prefer immutable request and response models when practical.

## TypeScript

* Use strict typing.
* Never use `any` unless explicitly requested.
* Prefer interfaces for contracts.
* Prefer `readonly` whenever possible.
* Keep functions pure whenever practical.
* Avoid duplicated state.
* Prefer explicit domain types over primitive strings when a finite set of values exists.

## Vue

* Prefer Composition API.
* Keep business logic outside templates.
* Avoid duplicated reactive state.
* Prefer `computed` over `watch` when possible.
* Keep components focused on presentation and user interaction.
* Move reusable business or workflow logic into composables or feature services when appropriate.

## WebUi

* Before creating, modifying, reviewing, or refactoring frontend code, read and follow `docs/design/DESIGN.md`.
* Keep the frontend in the root `WebUi` project.
* Keep API access in feature `api` modules or shared API utilities.
* Keep pages focused on composition and workflow.
* Keep reusable UI in `shared/components`.
* Do not duplicate backend business rules in the UI.
* Client-side validation exists for user feedback and must not replace backend validation.

---

# Architecture

* Follow Clean Architecture principles.
* Follow SOLID.
* Follow DRY, KISS, YAGNI, and Clean Code principles.
* Follow CQRS for application use cases.
* Prefer composition over inheritance.
* Keep UI, application logic, domain logic, and data access separated.
* Do not mix concerns between architectural layers.
* Dependencies must point inward according to Clean Architecture boundaries.
* The Domain layer must not depend on Application, Infrastructure, API, or WebUi.
* The Application layer must not depend directly on Infrastructure implementations.
* Infrastructure must implement contracts defined by inner layers when appropriate.
* API and WebUi must not contain domain business rules.
* For frontend code, organize by feature and isolate HTTP and data access from components.

---

# CQRS

Application use cases MUST follow CQRS.

Before implementing or modifying an application use case, classify the operation as either a Command or a Query based on its intent.

## Commands

Commands represent intent to change application state.

Examples include operations that:

* create
* register
* update
* edit
* activate
* deactivate
* cancel
* approve
* reject
* process
* import
* transfer
* close
* reopen
* remove or delete, when the domain allows deletion

Rules:

* Commands MUST represent state-changing operations.
* Commands and Queries MUST have separate request and handler types.
* Do not use a Command for a read-only operation.
* A Command Handler may read data required to validate or execute the operation.
* Reads required by a Command are part of the Command workflow.
* Do not create or call a Query Handler solely to retrieve data required internally by a Command.
* Do not call another Command Handler from a Command Handler.
* Prefer direct dependency usage inside handlers instead of chaining handlers.
* Keep Commands focused on business intent rather than CRUD terminology when meaningful domain terminology exists.
* Commands may persist changes.
* Commands may return data required to represent the result of the operation.
* Do not return large read models from Commands when the caller can retrieve them through a dedicated Query.
* Keep transaction boundaries explicit and consistent with the business operation.
* Validate input and business preconditions before persisting changes whenever possible.

## Queries

Queries represent intent to retrieve data without changing application state.

Examples include operations that:

* get
* retrieve
* list
* search
* filter
* summarize
* report
* inspect
* look up

Rules:

* Queries MUST be read-only.
* Queries MUST NOT change application state.
* Queries MUST NOT call `SaveChangesAsync`.
* Queries MUST NOT cause observable side effects.
* Queries and Commands MUST have separate request and handler types.
* Do not use a Query for an operation that changes state.
* Do not call a Command Handler from a Query Handler.
* Prefer direct dependency usage inside handlers instead of chaining handlers.
* Keep Queries focused on the data required by the caller.
* Prefer read-optimized DTO projections.
* Avoid loading complete entities when only a projection is required.
* Prefer server-side filtering, ordering, projection, and pagination.
* Use no-tracking access patterns when entities do not need to be tracked.
* Avoid N+1 queries.
* Do not expose persistence entities directly as Query results.

## Mixed Operations

If a task appears to combine reads and writes:

* Determine the primary intent of the use case.
* Use a Command when the primary intent is changing application state.
* Reads required to validate or execute a Command belong to the Command workflow.
* Do not create a Query solely to read data internally for a Command.
* Keep independently useful read-only use cases as Queries.
* Do not split a single transactional business operation into artificial Command and Query chains.

---

# Skills

Project-specific implementation workflows are defined in `.agents/skills`.

Use the appropriate Skill when the task matches its purpose.

## Command Skill

For tasks involving the creation, implementation, structural modification, or review of a Command use case:

* Read and follow `.agents/skills/create-command/SKILL.md`.
* Apply the Skill together with the CQRS and Architecture rules defined in this document.
* Use the Skill for tasks involving Command requests, handlers, validators, persistence workflows, transaction boundaries, and Command tests.
* Do not use the Command Skill for read-only use cases.

## Query Skill

For tasks involving the creation, implementation, structural modification, or review of a Query use case:

* Read and follow `.agents/skills/create-query/SKILL.md`.
* Apply the Skill together with the CQRS and Architecture rules defined in this document.
* Use the Skill for tasks involving Query requests, handlers, DTO projections, filtering, pagination, read optimization, and Query tests.
* Do not use the Query Skill for state-changing use cases.

## Skill Usage Rules

* Identify the operation type before selecting a Skill.
* Use Skills only when their scope matches the task.
* Do not force a Skill onto unrelated work.
* Do not use multiple Skills when one clearly covers the task.
* When a task contains independent Command and Query use cases, apply the corresponding Skill to each use case separately.
* Skills supplement this `AGENTS.md`; they do not override it.
* If a Skill conflicts with this `AGENTS.md`, follow this `AGENTS.md`.
* If a referenced Skill is unavailable, follow the CQRS and Architecture rules in this document and preserve existing project conventions.
* Read only the Skill references needed for the current task.
* Do not load unrelated Skill documentation without a concrete need.

---

# Task-Specific Instructions

Before creating, modifying, reviewing, or refactoring application features, identify the type of work being performed.

## Backend Application Features

For application use cases:

1. Determine whether the operation is a Command or Query.
2. Apply the CQRS rules from this document.
3. Read and follow the corresponding Skill when applicable:

   * `.agents/skills/create-command/SKILL.md`
   * `.agents/skills/create-query/SKILL.md`
4. Inspect similar existing use cases in the same feature or module.
5. Preserve existing conventions when they do not conflict with this document or the applicable Skill.
6. Implement the smallest complete change.
7. Update or create tests for changed behavior.

## Frontend Design

For operations that create, modify, review, or refactor frontend code:

* Read and follow `docs/design/DESIGN.md`.
* Apply the design system to UI layout, colors, typography, spacing, shapes, and component styling.
* Keep design choices consistent with existing frontend conventions when they do not conflict with `docs/design/DESIGN.md`.
* Do not apply backend Command or Query Skills to frontend-only work.

## Instruction Priority

When working on the project, follow this priority:

1. Follow this `AGENTS.md`.
2. Follow the applicable task-specific instruction:

   * `.agents/skills/create-command/SKILL.md`
   * `.agents/skills/create-query/SKILL.md`
   * `docs/design/DESIGN.md`
3. Follow existing project conventions when they do not conflict with higher-priority instructions.
4. Prefer the smallest correct change.
5. If requirements are ambiguous, ask before making assumptions about business rules, architecture, or design intent.

---

# Performance

Before writing code, consider:

* unnecessary allocations
* repeated LINQ enumerations
* repeated database queries
* N+1 queries
* unnecessary collection materialization
* O(n²) algorithms
* unnecessary async operations
* loading more data than required
* client-side filtering that should happen in the database

Prefer simpler and faster implementations.

Do not sacrifice readability for premature optimization.

For Queries, pay particular attention to:

* projection before materialization
* server-side filtering
* pagination
* no-tracking reads
* avoiding unnecessary navigation loading

For Commands, pay particular attention to:

* minimizing database round trips
* avoiding duplicate existence checks when a single operation can safely enforce the invariant
* transaction boundaries
* concurrency behavior when relevant

---

# Error Handling

* Fail with meaningful errors.
* Never swallow exceptions.
* Validate input early.
* Do not use exceptions for normal business flow when the project has an established result or validation pattern.
* Preserve useful exception context when rethrowing.
* Do not expose infrastructure details or sensitive information to API consumers.
* Handle expected domain and validation failures using the project's established error-handling conventions.

---

# Git

Do not create commits unless explicitly asked.

Do not push.

Do not create branches.

## Commit Guidelines

When creating or suggesting Git commits, always follow the Conventional Commits specification:

https://www.conventionalcommits.org/en/v1.0.0/

Use the following format:

<type>[optional scope]: <description>

[optional body]

[optional footer(s)]

Common commit types:

* `feat`: introduces a new feature
* `fix`: fixes a bug
* `refactor`: restructures code without changing behavior
* `perf`: improves performance
* `test`: adds or updates tests
* `docs`: changes documentation
* `style`: changes formatting without affecting behavior
* `build`: changes the build system or dependencies
* `ci`: changes CI/CD configuration
* `chore`: maintenance work that does not modify application behavior

Commit rules:

* Use lowercase for the commit type.
* Keep the description concise and imperative.
* Use a scope when it helps identify the affected module or feature.
* Do not use vague descriptions such as `fix stuff`, `update code`, or `changes`.
* Analyze the actual staged changes before proposing or creating a commit.
* Ensure the commit message accurately describes the staged changes.
* If staged changes contain multiple unrelated concerns, recommend splitting them into separate commits.
* Do not include unrelated changes in the same commit.
* For breaking changes, use `!` after the type or scope, or include a `BREAKING CHANGE:` footer.

Examples:

feat(cashier): add payment form filtering

fix(payment): include missing payment methods in balance

refactor(menu): simplify disabled state synchronization

test(cashier): add payment form validation tests

ci(pipeline): update dotnet test configuration

When explicitly asked to commit:

1. Inspect `git status`.
2. Inspect the staged diff.
3. Verify that the staged changes belong to the same logical change.
4. Propose or create an accurate Conventional Commit message.
5. Never push unless explicitly asked.

---

# Testing

When changing behavior:

* Update existing tests affected by the change.
* Create new tests when behavior is added or when regression coverage is appropriate.
* Test observable behavior rather than implementation details.
* Keep tests deterministic.
* Avoid unnecessary mocking.
* Prefer clear Arrange, Act, and Assert structure.
* Cover relevant success and failure paths.
* For Commands, test state changes, validation failures, and important business rules.
* For Queries, test filtering, projection, empty results, and relevant edge cases.
* Do not weaken or delete tests only to make a change pass.

If tests cannot be executed, explain why.

---

# Refactoring

When refactoring:

* Preserve behavior.
* Avoid unrelated formatting changes.
* Avoid touching unrelated files.
* Keep the refactoring scope explicit.
* Do not combine broad architectural refactoring with a small feature or bug fix unless required.
* Run relevant tests after refactoring when possible.

---

# Responses

When proposing changes:

1. Explain the problem.
2. Explain the solution.
3. Mention relevant trade-offs.
4. Show the final code or summarize the exact files changed.

If there is a significantly better approach than requested, explain it before implementing.

Keep explanations proportional to the complexity of the task.

---

# Security

Never:

* hardcode secrets
* expose credentials
* log sensitive data
* disable security checks
* ignore SQL injection risks
* trust client-side validation as a security boundary
* expose internal exception details to external consumers

Always:

* validate untrusted input
* use parameterized database access
* respect authorization boundaries
* preserve existing authentication and authorization behavior unless explicitly asked to change it

---

# Communication

Be concise.

Avoid repeating information.

State assumptions explicitly.

When uncertain, say so.

Prefer facts over speculation.

When a task is completed, summarize:

* what changed
* relevant architectural decisions
* tests executed and their results
* anything that could not be verified
