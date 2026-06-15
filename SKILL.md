# Backend Engineering Skill

## Purpose

Working rules, architecture constraints, execution workflow, and reporting standards for this repository.

Goal: consistency, maintainability, and safe implementation across all tasks.

---

# Core Principles

## Inspect Before Implement

Never assume. Before touching code:

- Read the files relevant to the task.
- Identify existing patterns in those files.
- Check what dependencies and configurations are already in place.

Concrete inspection steps:
1. `view` the project structure to orient.
2. `grep` or search for existing implementations of the same concept (e.g., before adding a new repository, find an existing one and read it).
3. Read the relevant `appsettings.json` and DI registration files.

If an existing pattern covers the task, follow it. Do not invent a parallel pattern.

If no pattern exists yet, say so in the planning step and propose one before implementing.

If the existing pattern is clearly wrong (security risk, architectural violation, broken in production), flag it explicitly rather than silently perpetuating it. Propose a correction as a separate task.

If a file cannot be found:
- Do not assume it does not exist.
- Search first.
- Only conclude absence after inspection.

---

## Minimize Scope

Only modify files directly related to the task. Avoid:

- Unrelated refactoring.
- Renaming without a stated reason.
- Moving code between layers unnecessarily.
- Architecture changes unless explicitly requested.

---

## Preserve Existing Behavior

Do not change observable behavior unless explicitly requested.

For refactoring tasks:
- Existing API contracts must remain unchanged.
- Existing response structures must remain unchanged.
- Existing routes must remain unchanged.
- Existing business behavior must remain unchanged.

If behavior changes are unavoidable, explain them first.

---

## Explain Before Acting

Before writing code:

1. State what you found.
2. State what is missing or incorrect.
3. State what you will change and why.

Then implement. Then validate.

---

## Architecture Consistency

Follow the existing architecture. Prefer consistency with the repository over personal preference.

When the existing architecture has a problem, name it and propose a fix — but as a separate task, not a side effect of the current one.

---

## Confirmation Rules

Large changes require confirmation before proceeding. Examples:

- More than 10 files modified.
- New package installation.
- Authentication changes.
- Database schema changes.
- Cross-layer changes.

---

# Project Context

ASP.NET Core Web API following Clean Architecture.

Layers: API, Application, Domain, Infrastructure, Shared.

Multiple developers work simultaneously. Changes must be safe for team collaboration — keep scope tight, avoid large cross-cutting diffs.

## Merge Rules

When multiple codebases exist:
- Current root project is the source of truth.
- External projects are reference material only.
- Never overwrite root blindly.

Prefer in order:
1. Reuse.
2. Adapt.
3. Extend.

Only replace existing code if the replacement is clearly superior and low-risk.

---

# Clean Architecture

## Dependency Direction

```
API          → Application
Infrastructure → Application
Infrastructure → Domain
Application  → Domain
Domain       → (nothing)
```

Domain must not depend on any other layer. No circular dependencies.

If you need to check for circular dependencies: `dotnet build` will surface them. Read the dependency graph from project `.csproj` references.

---

## Layer Responsibilities

### API
- Controllers, Middleware, Request/Response models
- Dependency injection registration
- Authentication and authorization configuration

No business logic. Controllers call Application use cases and return responses.

### Application
- Use cases and application services
- DTOs and interface definitions
- Validation (FluentValidation or equivalent)
- Business workflows

No infrastructure implementation (no EF Core, no HTTP clients). Defines interfaces that Infrastructure implements.

### Domain
- Entities, Value Objects, Enums
- Domain rules and invariants
- No framework dependencies

When two entities have the same name:
- Root version wins.
- Only add missing properties if safe.
- Never remove properties automatically.
- Never rename properties automatically.

### Infrastructure
- EF Core, DbContext, entity configurations
- Repository implementations
- External service clients (email, storage, etc.)
- Authentication providers (JWT generation, token validation logic)

Implements interfaces defined by Application.

### Shared
- Constants and enums used across layers
- Generic result types (e.g., `Result<T>`, `PagedList<T>`)
- Extension methods and utility helpers
- Cross-cutting concerns (e.g., `ClaimsPrincipalExtensions`)

Keep Shared lean. Domain-specific logic does not belong here.

---

# Database and EF Core

Before any database-related changes, inspect:

- `DbContext` class and entity configurations (`IEntityTypeConfiguration<T>`)
- Connection string location and format (`appsettings.json`, environment variables, secrets)
- Existing migrations under `Infrastructure/Migrations/`
- Any SQL scripts in the repository

Determine the strategy in use:

| Strategy | Signal |
|---|---|
| Code First + EF Migrations | `/Migrations` folder exists with `__EFMigrationsHistory` entries |
| Database First | Scaffolded entity classes, no migrations folder |
| Manual SQL | `.sql` files in the repo, no EF migrations |

Do not assume. State what you found before proceeding.

**Adding a migration:**
```bash
dotnet ef migrations add <MigrationName> --project Infrastructure --startup-project API
dotnet ef database update --project Infrastructure --startup-project API
```

Verify EF tooling is available: `dotnet ef --version`. If missing: `dotnet tool install --global dotnet-ef`.

After applying, inspect the generated migration file for unintended changes (dropped columns, renamed tables) before committing.

---

# Error Handling

Follow whatever error handling pattern already exists in the project. Look for:

- A global exception middleware or `IExceptionHandler` (ASP.NET Core 8+)
- A `Result<T>` or `Error` type in Application or Shared
- ProblemDetails responses

If a `Result<T>` pattern is in use: return `Result` from use cases; the controller maps it to HTTP responses. Never throw exceptions for expected business failures (not found, validation error, conflict).

If no pattern exists, propose one before implementing. Do not mix exception-based and result-based flows.

Common HTTP mappings to establish consistently:

| Failure type | HTTP status |
|---|---|
| Not found | 404 |
| Validation error | 400 |
| Conflict / duplicate | 409 |
| Unauthorized | 401 |
| Forbidden | 403 |
| Unexpected error | 500 |

---

# Logging

Before adding any logging, check what provider is already configured (look in `Program.cs` and `appsettings.json`).

Common setups:
- `Microsoft.Extensions.Logging` (built-in, minimal)
- Serilog (check for `UseSerilog()` in `Program.cs`)
- NLog (check for `UseNLog()`)

Follow the existing provider. Do not introduce a second logging library.

Log levels to follow:

| Level | When |
|---|---|
| `Information` | Business events (order created, user logged in) |
| `Warning` | Recoverable problems (retry, fallback used) |
| `Error` | Failures that need attention (exception caught, operation failed) |
| `Debug` | Developer diagnostics — not in production |

Never log passwords, tokens, PII, or secrets. Use structured logging (`_logger.LogInformation("Order {OrderId} created", order.Id)`) not string interpolation.

---

# Testing

Before adding tests, inspect the test projects:

- What test framework is in use? (xUnit, NUnit, MSTest)
- What mocking library? (Moq, NSubstitute)
- Are there integration tests using `WebApplicationFactory`?
- Is there a shared test fixture or base class?

Follow the existing structure. Tests live in a project that mirrors the layer they test (e.g., `Application.Tests`, `Infrastructure.Tests`).

**Unit tests:** Test use cases and domain logic in isolation. Mock all dependencies via the interfaces Application defines.

**Integration tests:** Test HTTP endpoints end-to-end using `WebApplicationFactory<Program>` with a real or in-memory database. Check that DI wires up, middleware runs, and responses match expected contracts.

When a task adds behavior, add at least one test that covers the happy path and one that covers the primary failure case.

---

# Configuration

Configuration sources, in order of precedence (ASP.NET Core default):

1. Environment variables
2. `appsettings.{Environment}.json`
3. `appsettings.json`

Rules:
- Never hardcode secrets, credentials, or connection strings in code.
- Use `IOptions<T>` or `IOptionsSnapshot<T>` to bind configuration sections to typed classes.
- Keep `appsettings.Development.json` out of version control if it contains real credentials. Use `dotnet user-secrets` locally.

---

# Dependency Injection

All services registered through DI. Before adding a registration:

1. Search for existing registrations (usually in extension methods per layer, e.g., `AddApplicationServices()`, `AddInfrastructureServices()`).
2. Follow the existing registration method — do not add registrations directly in `Program.cs` if the project uses extension methods.
3. Choose lifetime deliberately: `Scoped` for most services (per request), `Singleton` for stateless/shared, `Transient` only when needed.

Avoid duplicate registrations. If a service is already registered, do not re-register it.

---

# Package Rules

Before adding a NuGet package:

1. Search whether an existing package already solves the problem.
2. Prefer built-in .NET libraries.
3. Avoid introducing dependencies unnecessarily.

---

# Version Control

Changes should be safe for a multi-developer team:

- Keep commits scoped to a single logical change.
- Do not bundle refactoring with feature work in the same commit.
- Migration files must be committed with the code that requires them.
- If you add a new package (`dotnet add package`), the `.csproj` change must be included.

**Git Safety:**
Never run the following unless explicitly requested:
- `git reset --hard`
- `git clean -fd`
- `git checkout .`
- `git restore .`

---

# Task Execution Workflow

## Step 1 — Analysis

Read:
- Relevant projects and folders
- Existing implementation of the same concept
- Configuration files
- Any related tests

State findings clearly before moving on.

---

## Step 2 — Planning

Describe:
- What currently exists
- What is missing or incorrect
- What you will change, file by file
- Any risks or unknowns

Get confirmation if the plan involves broad changes or introduces a new pattern.

---

## Step 3 — Implementation

Make the smallest safe change. Follow:
- Existing coding style and naming conventions
- Existing architecture patterns
- Existing error handling and logging approach

---

## Step 4 — Validation

Match validation effort to the task type:

**Code change:**
```bash
dotnet build
# Resolve all warnings, not just errors
```

**New endpoint:**
```bash
dotnet run --project API
# Hit the endpoint via Swagger UI or curl
# Verify response shape, status code, and error cases
```

**Database change:**
```bash
dotnet ef database update --project Infrastructure --startup-project API
# Inspect the schema in the database to confirm the migration applied correctly
```

**New service / DI registration:**
- Start the application and confirm it starts without exceptions
- Run any existing integration tests: `dotnet test`

A task is not complete until:
- `dotnet build` succeeds.
- No compile errors remain.
- Startup succeeds if affected.

Warnings:
- Do not introduce new warnings.
- Existing warnings may be reported but do not need to be fixed unless related to the task.
- If warnings cannot be fixed safely, mention them in the final report.

---

## Step 5 — Report

Always provide a final report in the format below.

---

# Final Report Format

## Findings
- What was already implemented
- Architecture observations
- Any problems noticed (flagged, not fixed in this task)

## Changes Made
- Files modified (with brief reason)
- Files added
- Configuration or package changes
- Migration changes

## Validation
- Build result
- Test result
- Runtime / startup result
- Database result (if applicable)

## Risks
- Known gaps or missing information
- Potential issues introduced
- Anything that needs follow-up

## Next Steps
- Recommended follow-on tasks

---

# Task-Specific Guidance

## Adding a New Endpoint

1. Find an existing endpoint that does something similar in shape (e.g., a GET by ID if you're adding one).
2. Follow the same controller method, use case, DTO, and repository pattern.
3. Add request validation (check how validation is applied — attribute-based, FluentValidation pipeline behavior, or manual).
4. Map errors to correct HTTP status codes.
5. Add at least one integration test.

---

## Adding a New Feature (Use Case)

1. Define the use case interface and implementation in Application.
2. Define any new DTOs in Application — do not reuse domain entities as DTOs.
3. Define any new repository methods in the Application interface; implement in Infrastructure.
4. Register the use case in DI.
5. Add a controller action in API that calls the use case.
6. Write unit tests for the use case; write an integration test for the endpoint.

---

## Bug Fix

1. Reproduce the bug with a failing test before touching any code.
2. Make the minimal change that makes the test pass.
3. Do not refactor unrelated code in the same change.

---

## Setup SQL Server Connection

1. Locate the connection string in `appsettings.Development.json` or user secrets.
2. Verify SQL Server is running and the database exists.
3. Run `dotnet ef database update` to apply any pending migrations.
4. Start the application and confirm it connects without errors.

Success criteria: application starts, no `SqlException` on startup, a test query (e.g., hit a simple GET endpoint) returns data.

---

## Setup Migrations

1. Check for an existing `Migrations/` folder.
2. If migrations exist: run `dotnet ef database update` to apply pending ones.
3. If no migrations exist: explain this before creating an initial migration. Confirm Code First is the intended strategy. Then:
   ```bash
   dotnet ef migrations add InitialCreate --project Infrastructure --startup-project API
   dotnet ef database update --project Infrastructure --startup-project API
   ```
4. Inspect the generated migration for correctness before applying.

---

## Refactoring

- Refactoring must not change observable behavior.
- Cover the code with tests before refactoring, if tests are absent.
- Keep refactoring commits separate from feature commits.
- Document the motivation in the planning step — "cleans up code" is not sufficient.

Do not:
- Rename classes without reason.
- Rename methods without reason.
- Reorganize folders.
- Introduce new architectural patterns.
- Replace working code for stylistic reasons.

---

# Adapting Communication Style

Default response structure: Findings → Plan → Implementation → Validation. Use concise technical language. Avoid unnecessary explanations.

Match explanation depth to the person you are working with:

- If they are learning: explain what each term means when first introduced. Explain the *why* behind each change, not just the *what*. Show the commands used and what to verify.
- If they are experienced: lead with findings and diffs. Skip definitions. Use precise technical language directly.

Do not assume expertise level — read it from the conversation. Adjust as needed.

## C# Coding Conventions

Unless the repository explicitly uses a different convention:

- Follow standard Microsoft C#/.NET coding conventions.
- Use PascalCase for classes, methods, properties, enums, and public members.
- Use camelCase for parameters and local variables.
- Prefix interfaces with `I`.
- Prefix private fields with `_`.
- Async methods must end with `Async`.

## File Organization

- Use one public type per file.
- File name must match the primary type name.
- Do not create generic files such as `Models.cs`, `Entities.cs`, `Helpers.cs`, or `Utils.cs` that contain multiple unrelated types.
- Place types in folders that reflect their responsibility and layer.
---

# Token Efficiency

Token cost in coding agents doesn't just come from the prompt you type. Each turn, the model receives everything: system prompt, chat history, read file content, output of tool calls, and memory files. In a 50-turn session with a 10 KB SKILL.md file, this file alone consumes approximately 500 KB of input. The cost accumulates exponentially, not linearly.

## System Prompt / SKILL.md Hygiene

The system prompt is the most expensive artifact: it's resend on **every** turn, forever.

- Keep the total size of SKILL.md under **5,000 tokens (~200–300 lines)**. If the document becomes too large, consider separating less frequently used sections into separate files and loading them on demand.

- Delete sections irrelevant to the current project. A backend API project doesn't need frontend or mobile sections.

- Don't use SKILL.md as a scratchpad or temporary notes. Anything added here will cost tokens on every remaining turn of the session.

- When the document is too large, split it into **path-scoped rules** — only load when the agent reaches the file that matches that pattern:

- Cursor: `.cursor/rules/*.mdc` with `globs` frontmatter

- Claude Code: scoped rules files

- Cline: `.clinerules/<topic>.md` with conditional activation

## Prompt Cache Stability

Anthropic and most providers cache the prefix of the request. The cache is broken when the prefix changes — all overhead is restarted.

Rules to keep the cache stable:

- Do not modify SKILL.md, `.clinerules`, or the system prompt while the session is running.

- Do not install or remove MCP servers during an active session.

- Do not change the model mid-session.

- **Configure before starting, change as little as possible afterward.**

## Session Scoping — The One Task One Session Rule

Token cost increases with session length. Long sessions not only cost more — model quality decreases as the context expands, instructions are buried under accumulated noise.

Rules:

- **One session = one logical task.** Do not bundle unrelated changes.

- Start a new session for each task without direct continuity with the previous task.

- Use compact/summarize at natural breakpoints before context explodes:

- **Cline**: `/smol` (summarize in place), `/newtask` (handoff to a new session with a summary)

- **Claude Code**: `/compact`

- **Cursor**: Start a new chat, brief with a summary instead of copying the entire history.

- When the session is long and the model starts drifting (forgetting instructions, repeating questions), **start fresh**. Brief the new session only with what is truly needed.

## Inspect Selectively — Selective File Reading

Each file read into the context window will be resend for the remainder of the session.

Rules:
- **Grep first, read later.** Use `grep`/`rg` to pinpoint the exact location, then only read that specific section.

- Do not read files for exploratory purposes. Only read when the task actually requires that content.

- For large files (>200 lines), read a specific range instead of the entire file.

- Use `.claudeignore` / ignore patterns in `.clinerules` to exclude them from the agent's discovery file:

```
# .claudeignore (Claude Code) or equivalent
**/bin/
**/obj/
**/node_modules/
**/Migrations/ # Read only when necessary — migration files are very long
**/*.Designer.cs

**/wwwroot/lib/
```

Example applied to the current workflow:

```bash
# Instead of reading the entire DbContext (500+ lines):
grep -n "DbSet\|OnModelCreating" src/Infrastructure/Data/AppDbContext.cs

# Then read only the necessary range:

# view AppDbContext.cs lines 1–40 (DbSet declarations)
# view AppDbContext.cs lines 210–240 (target entity config)
```

## Command Output Discipline

The output of tool calls (dotnet build, test runner, migrations) goes directly into the context window.

Rules:
- Use **targeted output**. Filter noise before outputting to the context.

- Run tests only on the project related to the task, not the entire solution.

```bash
# Instead of:
dotnet build

# Use:
dotnet build 2>&1 | grep -E " error | warning " | head -50

# Instead of:
dotnet test

# Use:
dotnet test Application.Tests/ --logger "console;verbosity=minimal"
```

With migrations, always inspect the generated file immediately after creation — if there are unintended changes, delete and recreate it before applying, otherwise.

## Task Decomposition — Breaking Down Large Tasks

The most impactful token-saving habit: **breaking down large tasks into smaller, focused sessions.**

- Tasks involving 10+ files or multiple layers should be divided into subtasks, each in its own session.

- Each session should start with a clean context, containing only the essential artifacts.

- The output of the previous session (a summary of what was done) should be a brief for the next session — not the entire chat history.

A good breakdown for this project:

```
Session 1: Analysis + Planning (no coding)

Session 2: Implement Domain + Application layer
Session 3: Implement Infrastructure layer
Session 4: Implement API layer + validation
Session 5: Tests
```

## Memory Bank (Cline)

For a large project with a Cline, use **Memory Bank** (`.clinerules/memory-bank.md`) instead of pasting project context into each prompt.

The Memory Bank stores: project brief, system patterns, active context, and progress. The agent reads the beginning of each task instead of you having to explain it again.

- Update after key milestones, not continuously.

- Keep the total Memory Bank below **2,000 tokens**. Anything not in the Memory Bank will not cost tokens.

- Let Auto Compact handle context routines; manual updates are only for critical architectural decisions.

## Summary Table — Most Effective Leverage

| Leverage | Impact | Action |

|---|---|---|

| SKILL.md / system prompt size | **Very high** | Trim down to essentials; path-scope what is possible |

| Session length | **Very high** | One task per session; compact before context expands |

| File reads | **High** | Grep first; do not read speculative files |

| Task decomposition | **High** | Break down tasks; brief via summary, not history |

| Command output | **Medium** | Filter output; use `verbosity=minimal` |

| Prompt cache stability | **Medium** | Configure beforehand, do not change between sessions | | Model Selection | **Medium** | Lighter model for simple tasks |