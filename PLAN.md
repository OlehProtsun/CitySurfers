# PLAN.md

## CitySurfers Backend — Stage 1: Deployable Demo Foundation

**Repository:** `OlehProtsun/CitySurfers`  
**Baseline commit:** `047a852`  
**Stage:** 1 — Backend skeleton + persistent NoSQL demo foundation  
**Primary goal:** build the smallest backend that can be deployed and demonstrated reliably, while keeping expensive production features replaceable.

---

## Execution status — 2026-10-03

Implementation tasks 1–12 are complete. Stage 1 acceptance remains pending Atlas verification.

Verified:
- Clean source copy: restore/build passed (0 warnings/errors); 36 tests passed (17 unit, 19 integration), without Atlas.
- Release publish and Docker build passed.
- Local process and final Docker image, using real temporary MongoDB 8.0: liveness/readiness 200, valid login 200, invalid login 401.
- Persisted user data survives API restart; seeding preserves existing values/timestamps and creates no duplicates.
- Unique username index rejects duplicates; SeedOnStartup=false does not insert a demo account.
- Real database outage: liveness 200, readiness 503, login 500 with sanitized Problem Details.
- CORS allow/deny, development OpenAPI, validation 400 and sanitized 500 for JSON/HTML/plain-text Accept headers pass integration tests.
- Layer references inspected; build outputs, local credential files and .env are ignored and excluded from the image.
- Temporary smoke containers and network removed; Docker image uses non-root UID 1654.
- Verified image: `sha256:115ef1acb98e3ff2d75ab0fcfac644b4c7a15b23ea34135e2881caf7a15319d1`.

Pending:
- No MongoDb__ConnectionString for an Atlas cluster is available. Local MongoDB verification does not verify Atlas networking, credentials, TLS/SRV connectivity or deployed readiness.
- Repeat health/readiness and correct/incorrect login with the configured Atlas URI, locally and in the container.
- Stage 2 is not implemented.

## 1. Goal of This Stage

Create a clean, runnable and deployable ASP.NET Core backend foundation for CitySurfers.

This stage is **not** intended to build the final production platform.

The target is a presentation-ready backend that:

- follows the N-Layer architecture required by `AGENT.md`;
- has clear replaceable interfaces;
- uses a real persistent cloud database so demo data survives backend restarts;
- keeps authentication intentionally simple for the presentation;
- avoids expensive production infrastructure that currently adds little visible value;
- can be packaged as a Docker container and deployed;
- can later replace demo implementations without rewriting controllers or unrelated modules.

The guiding rule is:

> Use real persistence where persistence matters for the deployed demo. Use simple Demo implementations where the real subsystem would consume significant time without improving the presentation.

---

## 2. Required Context

Before implementing anything, read:

1. `AppContext.md`
2. `AGENT.md`
3. this `PLAN.md`

Interpret them as:

- `AppContext.md` = product source of truth;
- `AGENT.md` = engineering and architecture rules;
- `PLAN.md` = implementation order and current scope.

Do not invent permanent product rules for items still marked TBD.

Especially do not invent final algorithms for:

- Game Rating;
- points;
- ranking;
- overtakes;
- runner matching;
- route scoring;
- season resets;
- anti-cheat;
- privacy thresholds.

If the demo needs one of these concepts before the real rules are defined, create a deterministic `Demo*` implementation behind an interface.

---

## 3. Stage 1 Architecture Philosophy

Use this pattern throughout the MVP:

```text
Application contract
        ↓
Infrastructure implementation
        ↓
DI registration in composition root
```

For complex features whose real implementation is not needed yet:

```text
Interface
    ├── Demo implementation       <- now
    └── Production implementation <- later
```

Examples:

```text
IAuthService
    ├── DemoAuthService
    └── ProductionAuthService      // later
```

```text
IRankingProvider
    ├── DemoRankingProvider
    └── RealRankingProvider        // later
```

```text
IOvertakeTargetProvider
    ├── DemoOvertakeTargetProvider
    └── RealOvertakeTargetProvider // later
```

For persistence, Stage 1 is different: persistence must be real because the API will be deployed.

```text
IUserStore
    └── MongoUserStore             // Stage 1
```

Future example:

```text
IRunSessionStore
    └── MongoRunSessionStore        // when Running slice is implemented
```

Do not spread temporary behavior through controllers with conditions like:

```csharp
if (isDemo)
{
    // temporary behavior
}
```

Put temporary behavior in an explicitly named replaceable service.

---

## 4. Persistence Decision — MongoDB Atlas

Use **MongoDB Atlas** as the MVP/deployed demo database.

Use the official:

```text
MongoDB.Driver
```

NuGet package.

### Why MongoDB for this stage

The goal is not to prove a relational database architecture.

MongoDB is selected because it keeps the MVP simple:

- managed cloud persistence;
- no EF Core;
- no relational schema migrations;
- no SQL server to deploy alongside the API;
- official .NET driver;
- simple document storage;
- easy environment-variable connection string;
- suitable for demo users and future run documents;
- backend can be deployed independently of the database.

### Important

Do not build a generic MongoDB framework.

Do not create:

- a generic `IRepository<T>` for everything;
- a Unit of Work abstraction;
- custom ORM-like infrastructure;
- complex Mongo mapping layers;
- event sourcing;
- CQRS infrastructure;
- database transactions unless a real use case requires them.

Create small persistence abstractions around actual use cases.

---

## 5. What Is Real vs Demo in the Presentation MVP

| Concern | MVP approach | Later production replacement |
|---|---|---|
| API host | Real ASP.NET Core API | Keep/evolve |
| Database | Real MongoDB Atlas | Keep or replace only if product requirements change |
| User persistence | Real MongoDB collection | Evolve schema/repository |
| Run persistence | MongoDB when run feature is added | Evolve as needed |
| Authentication | `DemoAuthService` | Proper authentication system |
| Password handling | Demo-only credential comparison | Password hashing / external identity provider |
| Registration | Not needed initially | Real registration flow |
| Ranking | `DemoRankingProvider` when needed | Real ranking engine |
| Points | `DemoPointsCalculator` when needed | Real scoring algorithm |
| Overtakes | `DemoOvertakeTargetProvider` when needed | Real matching/overtake engine |
| Live runner map | static/aggregated demo provider when needed | privacy-safe real aggregation |
| Notifications | no-op/demo | push provider |
| AI Coach | deterministic demo response | real AI integration |
| Cache | none | Redis only if justified |
| Live transport | normal HTTP first | SignalR/WebSockets if required |
| Background jobs | none | add only for a concrete job |
| Deployment | Dockerized backend | CI/CD can be added later |
| Logging | built-in logging | richer telemetry later if justified |

The database is intentionally **not** a fake in-memory database because deployed demo data should survive restarts.

---

## 6. Explicit Stage 1 Scope

### In scope

- .NET solution;
- `Api`, `Application`, `Domain`, `Infrastructure` projects;
- unit tests;
- integration tests;
- correct project references;
- dependency injection;
- ASP.NET Core startup;
- Problem Details;
- centralized exception handling;
- built-in logging;
- health endpoints;
- development OpenAPI;
- MongoDB Atlas integration;
- MongoDB configuration through environment/configuration;
- `users` collection;
- `IUserStore`;
- `MongoUserStore`;
- idempotent demo-user seeding;
- presentation-only `DemoAuthService`;
- `POST /api/auth/login`;
- Dockerfile;
- `.dockerignore`;
- configuration-based CORS for deployed frontend/backend separation;
- build/test validation;
- deployment-readiness checklist.

### Out of scope

Do not implement during Stage 1:

- EF Core;
- SQL Server/PostgreSQL;
- relational migrations;
- ASP.NET Core Identity;
- JWT access tokens;
- refresh tokens;
- OAuth/social login;
- MFA;
- email verification;
- password reset;
- full user registration;
- roles/permissions system;
- Redis;
- SignalR;
- message brokers;
- microservices;
- Kubernetes;
- Terraform;
- complex CI/CD;
- real ranking engine;
- real points algorithm;
- real overtake matching;
- real-time runner tracking;
- anti-cheat;
- push notifications;
- real AI integration;
- background workers unless a current feature requires one.

---

## 7. Technical Baseline

Unless existing repository configuration explicitly requires something else, use:

- .NET 10;
- ASP.NET Core Web API;
- built-in Microsoft Dependency Injection;
- built-in configuration;
- built-in logging;
- built-in Problem Details / exception handling where practical;
- built-in health checks;
- built-in OpenAPI support where practical;
- official `MongoDB.Driver`;
- xUnit;
- `Microsoft.AspNetCore.Mvc.Testing` for API integration tests.

Avoid unnecessary packages.

Do not add by default:

- MediatR;
- AutoMapper;
- FluentValidation;
- Serilog;
- MassTransit;
- generic repository frameworks;
- mapping frameworks;
- architecture frameworks.

A package should be added only when it solves a concrete current problem.

---

## 8. Target Solution Structure

Create/maintain:

```text
CitySurfers/
│
├── CitySurfers.sln
├── Dockerfile
├── .dockerignore
│
├── src/
│   ├── CitySurfers.Api/
│   ├── CitySurfers.Application/
│   ├── CitySurfers.Domain/
│   └── CitySurfers.Infrastructure/
│
└── tests/
    ├── CitySurfers.UnitTests/
    └── CitySurfers.IntegrationTests/
```

This is a **modular monolith**.

Do not create microservices.

---

## 9. Layer Responsibilities

### `CitySurfers.Domain`

Contains core product concepts only.

For Stage 1 keep it minimal.

It may contain a small user/profile model only if it is useful as a real domain concept.

Rules:

- no ASP.NET Core references;
- no MongoDB references;
- no Infrastructure references;
- no Application references;
- no BSON attributes;
- no framework-specific persistence concerns;
- no speculative base entities/frameworks.

Do not design the entire CitySurfers domain before its use cases exist.

---

### `CitySurfers.Application`

Contains use-case contracts and abstractions needed by the application.

Stage 1 should contain contracts such as:

```text
Authentication/
    IAuthService
    LoginRequest
    LoginResult

Users/
    IUserStore
    UserRecord / UserData contract as appropriate
```

Exact structure can differ if a cleaner design is found.

Rules:

- reference Domain;
- never reference Infrastructure;
- never reference MongoDB.Driver;
- do not depend on ASP.NET Core HTTP types;
- do not return `IActionResult` or use `HttpContext`;
- use focused interfaces rather than one giant repository abstraction.

---

### `CitySurfers.Infrastructure`

Contains technical implementations.

Stage 1 should contain approximately:

```text
Infrastructure/
├── DependencyInjection.cs
├── Persistence/
│   └── MongoDb/
│       ├── MongoDbOptions.cs
│       ├── Documents/
│       │   └── UserDocument.cs
│       ├── MongoUserStore.cs
│       ├── MongoDbHealthCheck.cs
│       └── DemoDataSeeder.cs
└── Authentication/
    └── DemoAuthService.cs
```

This is conceptual, not a mandatory exact folder tree.

Rules:

- Mongo-specific document classes stay in Infrastructure;
- do not add BSON attributes to Domain entities;
- implement `IUserStore` here;
- implement `IAuthService` with `DemoAuthService` here;
- `DemoAuthService` may depend on `IUserStore`;
- Mongo connection details remain in Infrastructure/configuration;
- do not expose `IMongoCollection<T>` outside Infrastructure.

---

### `CitySurfers.Api`

Contains HTTP transport and the composition root.

Responsibilities:

- login endpoint/controller;
- health endpoints;
- OpenAPI;
- Problem Details;
- centralized exception mapping;
- CORS policy from configuration;
- DI composition;
- startup invocation of demo-data initialization if enabled.

Controllers/endpoints must remain thin.

No database queries or password comparison directly inside controllers.

---

## 10. Project Reference Rules

Required dependency direction:

```text
CitySurfers.Domain
        ↑
CitySurfers.Application
        ↑
CitySurfers.Infrastructure
        ↑
CitySurfers.Api
```

Concrete references:

```text
Application    -> Domain
Infrastructure -> Application
Infrastructure -> Domain        only when required
Api            -> Application
Api            -> Infrastructure
```

Forbidden:

```text
Domain         -> Application
Domain         -> Infrastructure
Domain         -> Api
Application    -> Infrastructure
Application    -> Api
Infrastructure -> Api
```

No circular references.

---

## 11. MongoDB Configuration

Use a strongly typed options class such as:

```text
MongoDbOptions
- ConnectionString
- DatabaseName
```

Configuration section:

```json
{
  "MongoDb": {
    "ConnectionString": "",
    "DatabaseName": "citysurfers"
  }
}
```

Never commit the real connection string.

For deployment, configuration must be overridable with environment variables:

```text
MongoDb__ConnectionString
MongoDb__DatabaseName
```

Recommended deployed database name:

```text
citysurfers
```

For tests use a test double/in-memory store rather than requiring a live Atlas database.

---

## 12. MongoDB Client Registration

Create one `MongoClient` and reuse it through DI.

Do not create a new Mongo client for every request.

Expected conceptual registration:

```text
MongoClient -> Singleton
IMongoDatabase -> Singleton
MongoUserStore -> Scoped or Singleton as appropriate
```

Use configuration validation at startup so missing database configuration fails clearly in deployed environments.

Do not log the full MongoDB connection string because it contains credentials.

---

## 13. Stage 1 Database Collections

Create/use only the collection actually needed now:

```text
users
```

Do **not** create empty collections for every future module.

Future collections such as:

```text
runs
runSessions
rankings
routes
```

should appear only when corresponding functionality is implemented.

---

## 14. User Document for the Demo

Keep the Stage 1 document intentionally small.

Conceptual shape:

```json
{
  "id": "demo-user-1",
  "username": "demo",
  "displayName": "Demo Runner",
  "demoPassword": "1234",
  "createdAtUtc": "..."
}
```

This is **presentation data**, not the final production authentication schema.

Use a field name such as `DemoPassword` / `demoPassword` so the temporary nature is obvious.

Rules:

- never put a real person's password in this collection;
- never reuse real credentials;
- do not pretend this is production password storage;
- later production authentication must replace this mechanism instead of quietly extending it.

Create a unique index on:

```text
username
```

This prevents accidental duplicate seeded demo accounts and keeps login behavior deterministic.

Do not add indexes that are not required yet.

---

## 15. Demo Data Seeder

Create an idempotent `DemoDataSeeder` (or equivalent).

Purpose:

- ensure required presentation users exist after first deployment;
- make a fresh database immediately usable;
- allow repeat deployment/startup without duplicate users.

The seeder should perform an upsert or existence check for a small fixed set of demo accounts.

Example accounts:

```text
demo / 1234
runner1 / test123
```

Exact credentials may differ, but they must be obviously demo credentials.

Use configuration:

```text
DemoData__SeedOnStartup=true
```

so seeding can later be disabled without deleting the seeder.

Do not create huge fake datasets during application startup.

If ranking/map screens later need richer demo data, seed that data through a dedicated concern only when required.

---

## 16. User Persistence Contract

Add a focused application-layer abstraction.

Conceptual example:

```csharp
public interface IUserStore
{
    Task<UserData?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);
}
```

Add methods only when a use case actually requires them.

Do not immediately add full CRUD just because a repository exists.

`MongoUserStore` implements this contract using the `users` collection.

---

## 17. Demo Authentication — Required

Authentication for the presentation is intentionally simple.

### Endpoint

```text
POST /api/auth/login
```

Request:

```json
{
  "username": "demo",
  "password": "1234"
}
```

### Flow

```text
HTTP request
    ↓
Auth endpoint
    ↓
IAuthService
    ↓
DemoAuthService
    ↓
IUserStore
    ↓
MongoUserStore
    ↓
MongoDB Atlas / users
```

`DemoAuthService`:

1. loads the user by username;
2. compares the submitted password with the demo credential;
3. returns basic user data when valid;
4. returns authentication failure when invalid.

### Success response

Example:

```json
{
  "id": "demo-user-1",
  "username": "demo",
  "displayName": "Demo Runner"
}
```

### Failure

```text
HTTP 401 Unauthorized
```

Use the same generic failure message for unknown username and wrong password.

### Required abstraction

```text
IAuthService
    ↑
DemoAuthService
```

### Explicitly do not implement now

- JWT;
- refresh tokens;
- cookies/session infrastructure;
- ASP.NET Core Identity;
- OAuth;
- MFA;
- email verification;
- password recovery;
- authorization roles/policies.

This login is a presentation mechanism, not a production security boundary.

---

## 18. Current User Handling for the Demo

Do not create a complex authentication/session subsystem in Stage 1.

After successful login, the frontend may keep the returned demo user ID locally for presentation purposes.

When later endpoints need a current user, prefer the smallest temporary mechanism that works for the demo, for example an explicit:

```text
X-Demo-User-Id
```

header or user ID in a clearly defined request contract.

If this mechanism is introduced, isolate access behind something like:

```text
ICurrentUserAccessor
    ↑
DemoCurrentUserAccessor
```

Do not spread header parsing through all controllers.

Do not implement this until another endpoint actually needs current-user identity.

Before production this must be replaced by real authenticated identity.

---

## 19. Health Checks for Deployment

Expose:

```text
GET /health
```

for process/liveness.

Expected:

```text
HTTP 200 OK
```

Also expose a readiness check:

```text
GET /health/ready
```

The readiness check should verify that MongoDB can respond, for example with a lightweight ping.

Purpose:

- `/health` answers: is the API process alive?
- `/health/ready` answers: can the API currently reach required infrastructure?

Keep health output simple.

Do not leak connection strings or credentials.

---

## 20. CORS for Demo Deployment

The frontend and backend may be deployed on different origins.

Add a small configuration-based CORS policy.

Do not permanently hardcode `AllowAnyOrigin()` as the deployment policy.

Support configuration such as:

```text
Cors__AllowedOrigins__0=https://citysurfers-demo.example
Cors__AllowedOrigins__1=http://localhost:5173
```

For local development, known localhost origins are acceptable.

Keep credentials disabled unless a later real auth mechanism needs cookies.

---

## 21. API Baseline

Required endpoints for Stage 1:

```text
GET  /health
GET  /health/ready
POST /api/auth/login
```

Required platform behavior:

- development OpenAPI;
- Problem Details-compatible failures;
- centralized exception handling;
- built-in logging;
- configuration-based CORS;
- no stack traces returned by default.

Do not add complex API versioning or other infrastructure without a current need.

---

## 22. Error Handling

Use a small centralized HTTP error strategy.

Requirements:

- unexpected exceptions are logged;
- stack traces are not returned by default;
- invalid login returns `401` rather than throwing;
- database connectivity failures produce an appropriate server error/log entry;
- readiness endpoint reports unhealthy when MongoDB is unavailable;
- do not create a large custom error framework just for Stage 1.

Leave room for future mapping of:

- validation errors;
- not found;
- conflicts;
- business rule violations;
- forbidden operations.

---

## 23. OpenAPI

Expose API metadata in development.

The following endpoints should be discoverable:

```text
GET  /health
GET  /health/ready
POST /api/auth/login
```

Do not spend significant time on elaborate API documentation tooling.

---

## 24. Docker Deployment Baseline

Add a production-oriented multi-stage `Dockerfile` for `CitySurfers.Api`.

Use official Microsoft .NET container images compatible with the selected .NET version.

Conceptual stages:

```text
restore/build -> publish -> ASP.NET runtime image
```

Requirements:

- build from repository root;
- publish only the API and required project dependencies;
- run the published API;
- use a standard container HTTP port such as `8080`;
- no database inside the API container;
- no secrets baked into the image;
- configuration comes from environment variables;
- add `.dockerignore` for `bin`, `obj`, `.git`, IDE data, etc.

Do not add Docker Compose unless it later solves a concrete local-development need.

Because MongoDB is hosted in Atlas, the deployed API container remains stateless apart from temporary process memory.

---

## 25. Required Deployment Configuration

The deployed API should need only a small set of environment variables.

Minimum:

```text
ASPNETCORE_ENVIRONMENT=Production
MongoDb__ConnectionString=<secret Atlas URI>
MongoDb__DatabaseName=citysurfers
DemoData__SeedOnStartup=true
Cors__AllowedOrigins__0=<deployed frontend origin>
```

Depending on hosting platform, the HTTP port may also be configured by platform settings.

Do not commit real values for secrets.

Keep a safe example file/documentation with placeholders if useful, for example:

```text
.env.example
```

Never create a committed `.env` with real credentials.

---

## 26. Deployment Target Strategy

Do not couple code to one hosting provider.

The backend should be deployable to any provider capable of running a Docker container or ASP.NET Core application.

The only external required service for Stage 1 is:

```text
MongoDB Atlas
```

Do not add provider-specific SDKs unless a selected hosting provider actually requires one.

Stage 1 is complete when the repository is technically deployable; selecting and configuring a specific hosting vendor can be the deployment task immediately after Stage 1.

---

## 27. Expensive Features That Should Remain Demo Implementations

### Ranking

If the frontend needs leaderboard data:

```text
IRankingProvider
    ↑
DemoRankingProvider
```

Return deterministic demo ranking rows.

If stable persistence helps the presentation, demo ranking rows may be seeded into MongoDB, but do not create the real ranking engine.

Prefer static provider data unless editing/persistence is actually useful for the demo.

---

### Points

If the UI requires points:

```text
IPointsCalculator
    ↑
DemoPointsCalculator
```

Use a small deterministic calculation.

Example principle only:

```text
points = distance-based fixed demo calculation
```

Do not claim the temporary formula is the final CitySurfers model.

---

### Overtakes

If live-run UI needs an overtake target:

```text
IOvertakeTargetProvider
    ↑
DemoOvertakeTargetProvider
```

Return stable values such as:

```json
{
  "runnerName": "Runner_42",
  "distanceToOvertakeMeters": 1100,
  "currentRank": 38,
  "targetRank": 37
}
```

Do not build real-time competitor matching yet.

---

### Activity Map

If the map screen needs runner/activity visualization:

```text
IActivityMapProvider
    ↑
DemoActivityMapProvider
```

Return safe, deterministic Kraków demo points/routes.

Do not pretend demo points are real live people.

Do not spend time building the full privacy aggregation pipeline yet.

---

### Notifications

If another use case expects notification dispatch:

```text
INotificationService
    ↑
NoOpNotificationService
```

until push notifications produce actual demo value.

---

### AI Coach

If AI Coach is needed for the presentation:

```text
IAiCoachService
    ↑
DemoAiCoachService
```

Return deterministic contextual messages.

Do not integrate a real AI provider during this stage unless explicitly requested.

---

## 28. Running Persistence Direction for Stage 2

Because MongoDB is already available, the Running vertical slice should use persistent storage rather than an in-memory run store.

Future pattern:

```text
IRunSessionStore
    ↑
MongoRunSessionStore
```

Recommended first run document should remain simple, for example:

```text
id
userId
startedAtUtc
finishedAtUtc
distanceMeters
durationSeconds
status
```

Only add fields that the frontend/use case actually needs.

Do not attempt to model every future GPS point, route, split, anti-cheat marker and score in advance.

If detailed GPS storage becomes necessary later, design it as a separate concern based on real product requirements.

---

## 29. Dependency Injection

Create clear registration entry points.

Expected shape:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

Infrastructure registration should include Stage 1 bindings conceptually equivalent to:

```text
IUserStore   -> MongoUserStore
IAuthService -> DemoAuthService
```

Register MongoDB client/database infrastructure once.

Do not manually instantiate repositories/clients inside controllers.

`Program.cs` is the composition root and should remain small.

---

## 30. Testing Strategy

Tests must not depend on the developer having access to Atlas.

Do not require network/database access for normal `dotnet test`.

### Unit tests

At minimum test `DemoAuthService` using a small fake/in-memory `IUserStore`:

- valid credentials return expected demo user;
- invalid password fails;
- unknown username fails.

### Integration tests

Use ASP.NET Core test hosting.

Override persistence/auth dependencies as needed so tests are self-contained.

Required API tests:

```text
GET /health -> 200
```

```text
POST /api/auth/login with valid test credentials -> 200
```

```text
POST /api/auth/login with invalid credentials -> 401
```

The Mongo readiness endpoint may be tested separately with a stubbed health dependency or excluded from network-free integration assertions if that keeps the test setup simpler.

Do not add Docker-based Testcontainers in Stage 1 unless repository-level Mongo integration testing becomes necessary.

The deployed demo itself will verify real Atlas connectivity through `/health/ready`.

---

## 31. Build Quality Rules

Use normal modern C# defaults:

- nullable reference types enabled;
- async database APIs;
- `CancellationToken` where appropriate;
- no static global mutable application state;
- no service locator;
- no secrets in source control;
- small focused classes;
- clear names;
- dependency inversion at replaceable boundaries;
- no direct Mongo dependency in Domain/Application.

Demo credentials are presentation data, not real secrets.

MongoDB Atlas credentials **are secrets** and must only come from secure deployment configuration/environment variables.

---

## 32. Repository Hygiene

If missing, add/update:

- `.gitignore`;
- `.dockerignore`;
- solution/project files;
- optional `.env.example` with placeholders only.

Do not commit:

- `bin/`;
- `obj/`;
- IDE temporary files;
- MongoDB connection strings;
- real passwords;
- API keys;
- private deployment configuration.

Do not rewrite unrelated repository content.

Do not modify `AppContext.md` or `AGENT.md` unless implementation is blocked by a direct conflict.

---

## 33. Future Module Direction

The backend will likely grow around concepts such as:

```text
Users
Authentication
Running
Competition
Seasons
Progress
Routes
Maps
Races
Notifications
Integrations
```

Do not create every module now.

Create a module only when implementing its first real use case.

Keep these concepts separate:

- authentication != user profile;
- run tracking != ranking;
- ranking != points;
- personal progress != Game Rating;
- maps != raw private location exposure;
- demo providers != production algorithms.

---

## 34. Implementation Order

Execute in this order.

### Task 1 — Inspect repository

- [x] Read `AppContext.md`.
- [x] Read `AGENT.md`.
- [x] Read `PLAN.md`.
- [x] Inspect existing code/configuration.
- [x] Preserve valid existing setup.

### Task 2 — Create solution/projects

- [x] Create/update `CitySurfers.sln`.
- [x] Create `src/CitySurfers.Domain`.
- [x] Create `src/CitySurfers.Application`.
- [x] Create `src/CitySurfers.Infrastructure`.
- [x] Create `src/CitySurfers.Api`.
- [x] Create `tests/CitySurfers.UnitTests`.
- [x] Create `tests/CitySurfers.IntegrationTests`.
- [x] Add all projects to the solution.

### Task 3 — Configure references

- [x] `Application -> Domain`.
- [x] `Infrastructure -> Application`.
- [x] `Infrastructure -> Domain` only if needed.
- [x] `Api -> Application`.
- [x] `Api -> Infrastructure`.
- [x] test project references as required.
- [x] verify no circular/forbidden references.

### Task 4 — Add base DI

- [x] Add `AddApplication(...)`.
- [x] Add `AddInfrastructure(...)`.
- [x] Wire them in API startup.

### Task 5 — API foundation

- [x] Configure controllers/endpoints.
- [x] Configure development OpenAPI.
- [x] Configure Problem Details.
- [x] Configure centralized exception handling.
- [x] Configure built-in logging.
- [x] Configure CORS from configuration.
- [x] Map `/health`.

### Task 6 — MongoDB integration

- [x] Add official `MongoDB.Driver` to Infrastructure.
- [x] Add `MongoDbOptions`.
- [x] Bind `MongoDb` configuration.
- [x] Validate required Mongo configuration.
- [x] Register one reusable `MongoClient`.
- [x] Register `IMongoDatabase` internally in Infrastructure.
- [x] Add `MongoDbHealthCheck`.
- [x] Map `/health/ready` with Mongo connectivity check.
- [x] Ensure secrets are never logged.

### Task 7 — User persistence

- [x] Add focused `IUserStore` contract.
- [x] Add Infrastructure `UserDocument`.
- [x] Add `MongoUserStore`.
- [x] Use `users` collection.
- [x] Add unique username index.
- [x] Do not create generic repository infrastructure.

### Task 8 — Demo data seeding

- [x] Add `DemoDataOptions` or equivalent.
- [x] Add idempotent `DemoDataSeeder`.
- [x] Seed at least one demo user.
- [x] Make startup seeding configurable.
- [x] Ensure restart does not create duplicates.

### Task 9 — Demo authentication

- [x] Add login input/output contracts.
- [x] Add `IAuthService`.
- [x] Add `DemoAuthService`.
- [x] Make it read the user through `IUserStore`.
- [x] Register `IAuthService -> DemoAuthService`.
- [x] Add `POST /api/auth/login`.
- [x] Valid credentials -> `200` + demo user.
- [x] Invalid credentials -> `401`.
- [x] Do not add JWT/Identity/tokens.

### Task 10 — Tests

- [x] Add `DemoAuthService` unit tests with fake `IUserStore`.
- [x] Add API health integration test.
- [x] Add successful login integration test.
- [x] Add failed login integration test.
- [x] Keep normal test run independent from Atlas/network.

### Task 11 — Docker/deployment readiness

- [x] Add multi-stage `Dockerfile`.
- [x] Add `.dockerignore`.
- [x] Configure container port.
- [x] Verify API gets Mongo/CORS settings from environment variables.
- [x] Add placeholder deployment configuration documentation if useful.
- [x] Do not bake secrets into image.

### Task 12 — Repository hygiene

- [x] Update `.gitignore` if necessary.
- [x] Verify no secrets/build output are committed.
- [x] Keep changes focused.

### Task 13 — Validate locally

Run:

```bash
dotnet restore
dotnet build
dotnet test
```

Then run the API with valid MongoDB configuration and verify:

```text
GET /health       -> 200
GET /health/ready -> 200 when Atlas is reachable
```

Verify login with both correct and incorrect demo credentials.

### Task 14 — Validate container

Build the Docker image:

```bash
docker build -t citysurfers-api .
```

Run it with environment variables for MongoDB/CORS and verify the same health/login endpoints.

Do not include real credentials in shell history/documentation committed to Git.

---

## 35. Stage 1 Acceptance Criteria

Stage 1 is complete when:

- [x] solution builds from a clean checkout;
- [x] `dotnet test` passes without requiring Atlas;
- [x] API starts locally;
- [x] `GET /health` returns `200`;
- [ ] `GET /health/ready` confirms MongoDB connectivity when configured; (local MongoDB verified; Atlas pending)
- [x] MongoDB Atlas connection is configuration-driven;
- [x] no database secret exists in source control;
- [x] `users` are persisted in MongoDB;
- [x] demo users are seeded idempotently;
- [x] username has a unique index;
- [x] valid demo login returns persisted demo user data;
- [x] invalid login returns `401`;
- [x] authentication logic is behind `IAuthService`;
- [x] temporary auth implementation is named `DemoAuthService`;
- [x] user persistence is behind `IUserStore`;
- [x] Mongo implementation stays in Infrastructure;
- [x] no JWT/Identity/refresh-token infrastructure exists;
- [x] no EF Core/SQL database infrastructure exists;
- [x] Domain has no Mongo/ASP.NET/Infrastructure dependencies;
- [x] Application has no Mongo/Infrastructure/API dependency;
- [x] controllers contain no database/business logic;
- [x] exceptions are handled centrally;
- [x] Docker image builds;
- [x] API can run from the container using environment variables;
- [x] CORS can be configured for the deployed frontend;
- [x] no final ranking/points/overtake rules were invented;
- [x] no unnecessary infrastructure packages were added.

---

## 36. Rules for the Next Presentation Features

For every new feature use this decision tree:

```text
Does this need persistence to make the deployed demo reliable?
        |
        +-- Yes -> use a focused MongoDB store.
        |
        +-- No  -> keep it stateless if simpler.

Does the feature require an expensive/undefined production algorithm?
        |
        +-- No  -> implement the simple real behavior.
        |
        +-- Yes -> does the full subsystem add visible demo value now?
                     |
                     +-- Yes -> implement only the minimum useful real part.
                     |
                     +-- No  -> implement a replaceable Demo* provider.
```

Examples follow.

### UI needs leaderboard

Implement:

```text
IRankingProvider
DemoRankingProvider
GET /api/ranking
```

Do not build the final ranking engine.

### UI needs overtake target

Implement:

```text
IOvertakeTargetProvider
DemoOvertakeTargetProvider
```

Do not build live matchmaking.

### UI needs run state/history

Implement:

```text
IRunSessionStore
MongoRunSessionStore
```

Persist runs because the demo is deployed and restart persistence is useful.

### UI needs map activity

Implement:

```text
IActivityMapProvider
DemoActivityMapProvider
```

Return deterministic safe Kraków data.

Do not build real privacy-sensitive live tracking until explicitly required.

---

## 37. Recommended Stage 2

Do not execute Stage 2 as part of this plan unless explicitly requested.

The next useful vertical slice should be:

```text
Demo User
    ↓
Start Run
    ↓
Persist Active Run
    ↓
Update Run Progress
    ↓
Finish Run
    ↓
Persist Run Result
    ↓
Post-Run Summary
```

Recommended implementations:

```text
IRunSessionStore
    -> MongoRunSessionStore

IRankingProvider
    -> DemoRankingProvider

IOvertakeTargetProvider
    -> DemoOvertakeTargetProvider

IPointsCalculator
    -> DemoPointsCalculator
```

This provides a convincing deployed demo:

- login persists across deployments/restarts;
- users are real database records;
- run history can persist;
- ranking/overtake/scoring can remain fast deterministic demo logic;
- each demo provider can later be replaced independently.

---

## 38. What Must Be Replaced Before Production

The following are intentionally **not production-ready**:

- `DemoAuthService`;
- plain-text demo credential comparison;
- `demoPassword` field;
- demo current-user identification mechanism if later introduced;
- hardcoded/seeded demo accounts as authentication strategy;
- static ranking data;
- static overtake targets;
- demo scoring;
- no-op notifications;
- static map activity;
- static AI Coach responses.

MongoDB Atlas itself does **not** need to be replaced solely because this is a demo. It can remain if later product requirements fit MongoDB.

Before production, database indexes, security, access rules, backup strategy, data model and scaling requirements must be reviewed based on real usage.

---

## 39. Explicit Time-Saving Rules for the AI Agent

The agent must prefer the smallest implementation that satisfies the current demo use case.

### Do this

- use MongoDB documents directly inside Infrastructure;
- create focused store interfaces;
- seed deterministic demo data;
- return simple DTOs;
- use normal HTTP requests;
- use simple deterministic demo calculations;
- keep configuration small;
- keep Docker deployment portable;
- implement only endpoints required by the presentation/frontend.

### Do not do this without a concrete requirement

- introduce JWT because "APIs usually use JWT";
- introduce EF Core because "repositories usually use EF";
- introduce Redis because "rankings need caching";
- introduce SignalR because "runners are live";
- introduce background workers because "the architecture may need jobs";
- create microservices because modules are separate;
- create CQRS/MediatR pipelines for trivial use cases;
- create complicated generic repositories;
- create a full domain model for future features;
- create production ranking/overtake logic while rules are TBD;
- create Kubernetes/Terraform/large CI pipelines for the demo;
- over-engineer authentication that will be replaced later.

When uncertain, choose the simpler replaceable implementation.

---

## 40. Completion Report Format for the AI Agent

After completing Stage 1, report concisely:

```text
Implemented:
- N-Layer .NET backend foundation
- MongoDB Atlas persistence integration
- users collection + MongoUserStore
- idempotent demo user seeding
- demo authentication behind IAuthService
- health/readiness endpoints
- OpenAPI / centralized error handling / CORS
- Docker deployment baseline
- unit/integration tests

Validation:
- dotnet build: passed
- dotnet test: passed
- GET /health: 200
- GET /health/ready: healthy with MongoDB
- valid demo login: 200
- invalid demo login: 401
- docker build: passed

Persistent components:
- MongoDB Atlas
- users collection

Temporary MVP components:
- DemoAuthService
- demo credentials

Deferred intentionally:
- JWT / Identity / OAuth
- production password security
- real ranking/scoring
- real overtakes/live matching
- SignalR
- Redis
- push notifications
- AI provider integration
- advanced infrastructure
```

Do not generate a new roadmap after finishing. Continue only from the next explicit plan/task.

---

## 41. Technical References

For implementation details use official documentation first:

- MongoDB .NET/C# Driver: `https://www.mongodb.com/docs/drivers/csharp/current/`
- MongoDB Atlas free deployment: `https://www.mongodb.com/docs/atlas/tutorial/deploy-free-tier-cluster/`
- ASP.NET Core/.NET documentation: `https://learn.microsoft.com/aspnet/core/`

Do not copy architecture from tutorials when it conflicts with `AGENT.md` or this plan.

---

# End of Stage 1 Plan
