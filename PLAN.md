# PLAN.md — Stage 6: MVP Demo Release Candidate — Frontend Handoff + Deployment Readiness

## 0. Execution Directive

This file is the source of truth for Stage 6.

The implementing AI agent must:

1. Read `AGENT.md` first and follow it as the authoritative engineering and behavioral rule set.
2. Read this `PLAN.md`.
3. Do **not** perform a repository-wide analysis.
4. Do **not** create another roadmap, architecture report, or implementation plan.
5. Do **not** re-evaluate whether Stage 6 is the correct next stage.
6. Do **not** redesign Stages 1–5.
7. Inspect only files directly required by the current unfinished task and their immediate dependencies.
8. Start implementing the first unfinished task immediately.
9. Complete tasks in the order defined here unless a direct dependency requires a small local reordering.
10. Preserve the existing N-Layer architecture and all existing API contracts.
11. Reuse existing API, MongoDB, Docker, CORS, health-check, error-handling, and testing infrastructure.
12. Keep Stage 6 focused on release/demo readiness. Do not add new product mechanics.
13. Do not add frontend framework code to this backend repository.
14. Do not choose a cloud hosting vendor or frontend framework unless already configured by the repository.
15. Do not introduce provider-specific infrastructure or credentials.
16. Validate meaningful changes with focused tests while working.
17. Run the full automated suite before declaring the stage complete.
18. If a referenced symbol/path differs slightly, find the direct equivalent, make the smallest safe adaptation, and continue.
19. A small mismatch is **not** permission to scan the entire repository.
20. Do not start post-MVP features after completing this stage.

Required workflow:

```text
Read AGENT.md
→ Read PLAN.md
→ Find first unfinished task
→ Inspect only required files
→ Implement
→ Test
→ Mark task complete
→ Continue
```

Do not use:

```text
Analyze whole repository
→ produce architecture report
→ invent another roadmap
→ propose alternative Stage
→ wait for approval
```

---

# 1. Verified Starting Point

Stages 1–5 are complete.

The backend currently provides:

- ASP.NET Core / .NET 10 API;
- N-Layer modular architecture;
- MongoDB persistence;
- persistent demo user;
- demo authentication;
- server-side current-user resolution;
- run start / progress / finish lifecycle;
- deterministic run competition;
- overtake events and points;
- completed-run summaries;
- run history;
- personal progress;
- daily leaderboard;
- monthly leaderboard;
- monthly rival;
- primary next-goal selection;
- privacy-safe Kraków activity map;
- frontend-oriented `GET /api/home`;
- deterministic demo rank progression;
- opt-in demo-run reset;
- configurable CORS;
- centralized Problem Details;
- liveness and readiness health endpoints;
- Development-only OpenAPI;
- multi-stage non-root Docker image;
- automated unit/integration tests;
- real Mongo smoke tests.

Stage 5 established the deterministic showcase:

```text
0 points  → #41
16 points → #40
30 points → #39
41 points → #38
59 points → #37

59 monthly points
→ rival #36 at 64 points
→ points gap 5
→ points to pass 6
```

Do not change this behavior.

---

# 2. Review of Stage 5 — Preserve It

Stage 5 correctly implemented:

```http
GET /api/home
```

with:

```text
today rank / points
nullable active run
nullable primary next goal
```

It also correctly:

- reused `LeaderboardService`;
- reused `NextGoalService`;
- reused `IRunSessionStore`;
- aligned Today and Month demo competition ladders;
- preserved real current-user scoring;
- preserved run rewards and overtake thresholds;
- added `DemoData__ResetRunsOnStartup`;
- kept reset disabled by default;
- isolated reset to the demo user's runs;
- added CORS integration tests;
- expanded the API manual request file;
- added real Mongo Stage 5 smoke coverage;
- preserved the existing `users` and `runs` collection model.

Do not rewrite Stage 5.

---

# 3. Why Stage 6 Exists

The backend feature set is sufficient for the hackathon Core MVP.

The next problem is no longer:

```text
What backend product feature should we build?
```

The next problem is:

```text
Can a frontend developer connect to this API,
can the team run the complete demo reliably,
and can the same container be deployed and verified safely?
```

Stage 6 turns the backend into an MVP demo release candidate.

It must provide four things:

```text
1. Frozen frontend integration contract
2. Reproducible local release-mode demo
3. Production-mode / remote deployment verification
4. Automated release validation
```

Stage 6 must not introduce another gameplay module.

---

# 4. Product Scope Decision

The `AppContext.md` Core MVP already requires:

- user account;
- starting/recording a run;
- distance/pace/duration;
- daily ranking;
- season points;
- monthly leaderboard;
- live/simulated overtakes;
- post-run summary;
- personal progress;
- privacy-safe Kraków activity map.

Those backend capabilities already exist.

Rivals are also implemented even though they are a secondary feature.

Therefore Stage 6 must **not** implement:

- routes;
- King of Route;
- AI Coach;
- races;
- matchmaking;
- achievements;
- real social accounts;
- notifications;
- advanced anti-cheat.

Those can wait until after the MVP demo.

---

# 5. Stage Goal

At the end of Stage 6:

1. the API contract used by the MVP frontend is explicitly documented and treated as frozen;
2. the frontend team has one clear integration/handoff document;
3. API base URL is clearly treated as external frontend configuration;
4. the expected client call sequence is documented;
5. live-run client behavior is documented;
6. normal API error/recovery behavior is documented;
7. the backend can be launched locally in a production-like demo setup with one command;
8. that local demo setup includes MongoDB without requiring Atlas;
9. the local demo setup starts in deterministic fresh-demo mode by default;
10. the normal standalone Docker image remains unchanged in purpose;
11. the production-mode API is automatically smoke-tested;
12. a remote deployed API can be checked by a reusable smoke script;
13. the remote smoke script does not require MongoDB credentials;
14. a full destructive demo flow is opt-in, not the default remote check;
15. a CI workflow builds and tests the backend automatically;
16. the CI workflow does not require MongoDB or cloud secrets;
17. no secrets are committed;
18. Development OpenAPI behavior remains unchanged;
19. Production still does not expose OpenAPI;
20. all Stage 1–5 behavior remains green;
21. after Stage 6, backend feature work should stop for the hackathon and frontend integration/deployment should proceed.

---

# 6. Stage 6 Architectural Principle

Stage 6 is a release/integration stage.

Avoid changing:

```text
Domain
Application business rules
run scoring
leaderboard algorithms
rival algorithms
next-goal priority
map semantics
Mongo document schema
```

Most Stage 6 work should live in:

```text
docs/
tests/
scripts or tests/
GitHub workflow configuration
Docker Compose demo configuration
README
```

Small API/infrastructure corrections are allowed only when a Stage 6 verification test exposes a real integration or deployment blocker.

Do not manufacture code changes merely to make Stage 6 look larger.

---

# 7. Frontend Contract Freeze

Create:

```text
docs/MVP_FRONTEND_HANDOFF.md
```

This document is the authoritative handoff for the MVP frontend.

It must describe only the currently implemented backend.

Do not document unimplemented product features as if they exist.

The Stage 6 frontend contract is frozen around these endpoints:

```http
POST /api/auth/login

GET  /api/home

POST /api/runs
GET  /api/runs/active
GET  /api/runs/{runId}
PATCH /api/runs/{runId}/progress
POST /api/runs/{runId}/finish
GET  /api/runs/history

GET /api/progress

GET /api/leaderboards/today
GET /api/leaderboards/month

GET /api/rivals/current
GET /api/goals/next

GET /api/map/activity

GET /health
GET /health/ready
```

Do not rename these routes.

Do not change successful payload shapes unless an existing contract is objectively broken.

---

# 8. Frontend Screen → API Mapping

The frontend handoff document must map UI responsibilities to API calls.

## Login / Demo Entry

Use:

```http
POST /api/auth/login
```

Important MVP limitation:

```text
login validates the demo credentials,
but does not issue a token or session.
```

Other endpoints resolve the demo user server-side.

The frontend must not send or invent a user id.

## Home / Main Screen

Use:

```http
GET /api/home
```

It provides:

```text
today.rank
today.points
activeRun?
nextGoal?
```

The frontend should prefer Home for initial screen bootstrap.

Do not call three separate endpoints merely to rebuild the same initial state.

## Activity Map

Use:

```http
GET /api/map/activity?period=live
GET /api/map/activity?period=today
GET /api/map/activity?period=month
```

Document that returned coordinates are aggregate demo activity zones, not individual live runner GPS positions.

## Rankings

Use:

```http
GET /api/leaderboards/today
GET /api/leaderboards/month
GET /api/rivals/current
```

## Personal Progress

Use:

```http
GET /api/progress
GET /api/runs/history
```

---

# 9. Live Run Frontend Contract

Document the complete client run lifecycle.

## Start

```http
POST /api/runs
```

Expected:

```text
201 when created
409 if an active run already exists
```

If `409` occurs, the client should refresh Home or `GET /api/runs/active` instead of creating another run.

## Progress

```http
PATCH /api/runs/{runId}/progress
Content-Type: application/json
```

Example conceptual body:

```json
{
  "distanceMeters": 1300,
  "durationSeconds": 390
}
```

The frontend must send:

```text
non-negative
non-decreasing
distance and duration
```

The backend remains authoritative for:

```text
overtake creation
points
rank change
next run target
```

The frontend must not calculate or persist its own authoritative overtake state.

## Finish

```http
POST /api/runs/{runId}/finish
```

The frontend should render the returned post-run summary, then refresh:

```http
GET /api/home
```

to obtain the next rival goal.

---

# 10. Demo Progress Driver Boundary

The current backend does not ingest real GPS tracks.

For the hackathon UI, run progress may come from:

```text
a frontend demo simulator
or
a future real sensor/GPS source
```

Both must use the same API:

```http
PATCH /api/runs/{runId}/progress
```

Do not add a backend timer that automatically invents run distance.

Do not add a public endpoint such as:

```text
/api/demo/advance
/api/demo/simulate
```

The backend contract must remain compatible with a future real activity source.

The demo simulator belongs in the frontend/integration layer.

---

# 11. Client Refresh Strategy

Document a minimal client strategy.

Recommended MVP behavior:

```text
App entry / screen resume
→ GET /api/home

Start run
→ POST /api/runs

During run
→ frontend submits progress
→ render competition state from progress response

After finish
→ POST finish
→ render summary
→ GET /api/home
```

Do not require aggressive polling.

Do not add WebSockets or SignalR for this MVP.

Dedicated screens may lazy-load their own data.

---

# 12. Client Error/Recovery Contract

Document frontend behavior for common statuses.

## 400

Use for malformed input / validation failure.

Frontend behavior:

```text
show a friendly validation error
do not retry automatically with identical payload
```

## 401

Demo login failed.

Frontend behavior:

```text
show generic invalid-credentials state
```

## 404

Examples:

```text
run not found
GET /api/runs/active while idle
```

For active-run discovery, the frontend should normally prefer `/api/home`, where idle is represented as `activeRun: null`.

## 409

Examples:

```text
active run already exists
completed run cannot be modified
concurrent conflicting write
```

Frontend behavior should refresh authoritative state before retrying.

## 500 / 503

Treat as temporary backend/service failure.

Frontend should offer retry rather than inventing local authoritative game state.

---

# 13. API Base URL Contract

The frontend base URL must be external configuration.

Examples:

```text
local:
http://localhost:5092

docker local:
http://localhost:8080

deployed:
https://<deployed-api-host>
```

Do not hard-code the production URL into backend source code.

Do not invent a hosting vendor URL.

The handoff doc should instruct the frontend to use a single environment/config value such as:

```text
API_BASE_URL
```

The exact frontend environment-variable mechanism depends on the frontend framework and is outside this repository.

---

# 14. Local MVP Demo Compose

Add:

```text
docker-compose.demo.yml
```

Purpose:

```text
one-command local fallback demo
```

It should run:

```text
MongoDB
+
CitySurfers API
```

without requiring a separately installed/local Atlas connection.

The API service must:

- build from the existing repository `Dockerfile`;
- run in `Production`;
- connect to the Compose Mongo service;
- use a dedicated demo database;
- seed demo data;
- enable `ResetRunsOnStartup=true` for deterministic startup;
- expose the API on localhost port `8080`;
- configure a documented local frontend origin where needed;
- depend on Mongo health/readiness appropriately.

The Mongo service should:

- use an official stable MongoDB image;
- expose no credentials in source control;
- be isolated to the demo compose network;
- use an ephemeral or clearly demo-only data lifecycle;
- include a health check.

For the hackathon fallback, repeatability is more important than preserving demo history.

Do not modify the normal production Dockerfile to embed MongoDB.

---

# 15. Demo Compose Commands

Document:

```bash
docker compose -f docker-compose.demo.yml up --build
```

and:

```bash
docker compose -f docker-compose.demo.yml down -v
```

Expected URLs:

```text
API:
http://localhost:8080

Liveness:
http://localhost:8080/health

Readiness:
http://localhost:8080/health/ready
```

The compose setup must be suitable as an offline/local fallback if the remote demo environment is unavailable.

---

# 16. Compose Security Rules

Do not commit:

- MongoDB cloud credentials;
- API secrets;
- `.env`;
- frontend secrets.

Local Compose may use non-sensitive local-only Mongo connection values.

If authentication is configured for the local Mongo container, use demo-only local credentials and document that they are not production credentials.

Do not make the Compose file the production deployment architecture.

It is a local MVP demo fallback.

---

# 17. Production-Mode Release Smoke

Add:

```text
tests/Stage6.ReleaseSmoke.ps1
```

This is a local release-candidate validation.

It must:

1. build the actual repository Dockerfile;
2. run MongoDB in an isolated temporary environment or reuse a clearly isolated test Mongo;
3. run the API with:

```text
ASPNETCORE_ENVIRONMENT=Production
```

4. enable deterministic demo reset;
5. configure a test frontend CORS origin;
6. verify liveness;
7. verify readiness;
8. verify that Production does **not** expose `/openapi/v1.json`;
9. verify login;
10. verify clean Home state;
11. run the complete four-overtake demo flow;
12. verify the `#41 → #37` transition;
13. verify the 59-point finish;
14. verify the 6-point rival goal;
15. verify history/progress;
16. verify allowed CORS origin;
17. verify a disallowed CORS origin;
18. verify preflight;
19. verify no unexpected Mongo collections;
20. clean up every resource created by the script even after failure.

Prefer reuse of existing Stage 5 smoke helpers/patterns rather than duplicating large amounts of PowerShell.

Do not weaken the existing Stage 5 smoke.

---

# 18. Remote Deployment Smoke

Add:

```text
tests/Stage6.RemoteSmoke.ps1
```

Purpose:

```text
verify an already deployed API using only its public base URL
```

Required parameter:

```text
-BaseUrl
```

Optional:

```text
-FrontendOrigin
-FullDemo
```

## Default Safe Remote Check

Without `-FullDemo`, verify only non-destructive/read-oriented behavior:

```text
GET /health
GET /health/ready
POST /api/auth/login
GET /api/home
GET /api/leaderboards/today
GET /api/leaderboards/month
GET /api/map/activity?period=today
GET /api/progress
GET /api/runs/history
```

If `-FrontendOrigin` is supplied:

- verify CORS allow-origin behavior;
- verify preflight.

The remote smoke must not require:

- MongoDB connection string;
- database credentials;
- Docker access to the deployment host;
- cloud-provider CLI.

## Full Demo Mode

When explicitly passed:

```text
-FullDemo
```

the script may execute the run mutation flow.

Before doing so, it must clearly print that this modifies the demo user's run state.

Do not make destructive/mutating behavior the default.

---

# 19. Remote Smoke Behavior

The remote smoke script must:

- normalize trailing slash in base URL;
- use HTTPS or HTTP as supplied;
- fail with non-zero exit code on contract mismatch;
- print a concise step/result summary;
- avoid printing sensitive headers or secrets;
- handle expected HTTP status codes explicitly;
- set reasonable request timeouts;
- never assume direct Mongo access.

Do not hard-code a deployed URL.

---

# 20. Frontend Contract Integration Tests

Add or extend API integration tests to protect the MVP frontend contract.

Create a focused test class such as:

```text
MvpFrontendContractApiTests
```

It should exercise the main integration sequence using existing in-memory/replacement infrastructure.

Minimum checks:

## Bootstrap

```text
login
→ home
```

Verify important JSON fields exist with expected names.

## Run

```text
start
→ progress
→ overtake
→ finish
→ home
```

Verify:

- run id remains stable;
- distance/duration fields remain stable;
- competition/goal fields use existing names;
- finish response remains usable as post-run summary;
- Home after finish returns rival goal.

## Dedicated screens

Verify successful contracts remain available for:

```text
today leaderboard
month leaderboard
progress
history
activity map
rival
next goal
```

Do not duplicate every service unit test.

This test protects the frontend-facing sequence and JSON contract.

---

# 21. Contract Freeze Rule

After Stage 6, successful MVP API contracts are frozen for the hackathon.

Future fixes may:

- add optional fields;
- fix genuine defects;
- add new endpoints.

They should not casually:

- rename existing JSON properties;
- remove fields;
- change route paths;
- change normal success status codes;
- reinterpret the meaning of existing fields.

Document this in the frontend handoff.

This is a release discipline rule, not formal semantic API versioning.

Do not add `/v1` routing in this stage.

---

# 22. CI Workflow

Add:

```text
.github/workflows/backend-ci.yml
```

The workflow should run for:

```text
push
pull_request
```

on the relevant backend branches, including `main`.

Minimum CI steps:

```text
checkout
setup .NET 10
restore
build Release
test Release
```

CI must:

- use no MongoDB cloud secrets;
- rely on existing unit/integration replacement infrastructure;
- fail on build/test failures;
- avoid publishing artifacts/secrets by default.

A Docker build validation step is desirable if reliable in GitHub-hosted runners.

If Docker registry/base-image network instability would make CI unnecessarily flaky, keep the mandatory CI gate to restore/build/test and document Docker release validation separately through Stage 6 smoke.

Do not create a cloud deployment pipeline yet.

---

# 23. CI Scope

Do not add:

- automatic cloud deployment;
- production credentials;
- MongoDB Atlas credentials;
- GHCR publishing unless already required by the repository;
- environment approval workflows;
- release tagging automation.

This is a hackathon MVP CI safety net, not a full enterprise pipeline.

---

# 24. Production Configuration Runbook

Add a deployment section to:

```text
docs/MVP_FRONTEND_HANDOFF.md
```

or a focused:

```text
docs/MVP_DEPLOYMENT.md
```

If the handoff document becomes too large, use `MVP_DEPLOYMENT.md`.

Document required deployment environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production

MongoDb__ConnectionString=<secret>
MongoDb__DatabaseName=<database>

DemoData__SeedOnStartup=true
DemoData__ResetRunsOnStartup=<true for deterministic hackathon restart OR false for persistence>

Cors__AllowedOrigins__0=<frontend-origin>

ASPNETCORE_HTTP_PORTS=8080
```

Clarify:

- Mongo connection string is a secret;
- database name is configurable;
- reset flag intentionally deletes demo-user run history on startup;
- CORS origin must be the actual browser frontend origin;
- native mobile clients are not governed by browser CORS in the same way;
- provider TLS/HTTPS termination may happen outside the ASP.NET container;
- `/health/ready` should be used for readiness where the hosting platform supports it.

Do not invent provider-specific environment variable names.

---

# 25. Deployment Provider Boundary

Do not choose:

- Render;
- Railway;
- Fly.io;
- Azure;
- AWS;
- GCP;
- DigitalOcean;
- another vendor.

No provider has been selected by the project context.

Stage 6 prepares a standard Dockerized release candidate that can be deployed to any platform supporting:

```text
Docker container
environment variables
outbound MongoDB connectivity
HTTPS/public routing
```

Actual provider setup requires provider credentials and is outside source-code execution unless explicitly supplied later.

---

# 26. MongoDB Deployment Notes

Document that the deployed API requires:

```text
MongoDB reachable from hosting provider
```

For Atlas-like deployments:

- network access must permit the hosting environment;
- DB user must have required read/write/index permissions;
- connection string stays outside source control.

Do not add Atlas SDKs.

Do not modify persistence architecture.

Do not hard-code cloud Mongo addresses.

---

# 27. OpenAPI Handoff

Development OpenAPI currently exists at:

```text
/openapi/v1.json
```

and Production intentionally does not expose it.

Preserve this.

For frontend handoff, document how a developer can obtain the schema locally:

```bash
dotnet run --project src/CitySurfers.Api
```

then:

```text
http://localhost:5092/openapi/v1.json
```

Do not expose Production OpenAPI merely to simplify frontend development.

If a committed contract snapshot is added, it must be generated from the real API and clearly documented as a snapshot.

A committed snapshot is optional; do not add snapshot machinery if it creates brittle or complicated maintenance.

---

# 28. README Stage 6 Update

Update README from:

```text
Stage 5 backend
```

to a Stage 6 / MVP demo release candidate description.

Add concise sections for:

- frontend handoff doc;
- local demo Compose;
- release smoke;
- remote smoke;
- CI;
- deployment environment variables;
- contract freeze;
- next step: frontend integration.

Keep existing technical Stage 1–5 details that are still useful.

Do not rewrite the README into marketing copy.

---

# 29. No New Product Features

Do not implement in Stage 6:

- routes;
- route ranking;
- King of Route;
- AI Coach;
- Game Rating;
- real seasons;
- real rival persistence;
- races;
- matchmaking;
- friends;
- notifications;
- achievements;
- levels;
- GPS track persistence;
- exact individual map location;
- external map APIs;
- Strava/Garmin imports;
- real multi-user accounts;
- JWT;
- OAuth;
- refresh tokens;
- SignalR;
- WebSockets;
- Redis;
- background jobs;
- message queues;
- microservices;
- new database technology;
- new Mongo collections.

Stage 6 is a release stage.

---

# 30. Authentication MVP Boundary

Do not replace demo authentication in this stage.

Current demo behavior is deliberate:

```text
POST /api/auth/login
→ demo validation
→ no token/session
→ server-side demo-user resolution for subsequent calls
```

This is not production authentication.

The frontend handoff must explicitly state this limitation.

Do not pretend the deployed hackathon demo is production-ready for real users.

Real authentication belongs after the hackathon MVP or in a separately approved stage.

---

# 31. Security Guardrails

Preserve:

- sanitized errors;
- no Mongo credentials in logs;
- no secrets in repository;
- CORS allowlist behavior;
- no wildcard Production CORS;
- non-root container;
- Development-only OpenAPI.

Review newly added scripts/docs for accidental secrets.

Do not log:

- Mongo connection strings;
- passwords;
- full environment dumps.

Do not add demo-reset HTTP endpoints.

---

# 32. Observability Scope

Existing console logging and health endpoints are sufficient for the MVP.

Do not add:

- Serilog stack;
- OpenTelemetry;
- distributed tracing backend;
- metrics server;
- external logging SaaS.

If Stage 6 tests expose a specific missing diagnostic, make the smallest targeted change.

Do not build a new observability platform.

---

# 33. Test Strategy

Stage 6 should add tests only for new release/integration guarantees.

Required validation categories:

```text
existing unit/integration suite
frontend contract integration flow
local production-mode release smoke
remote smoke script logic where practical
CI syntax/behavior through actual workflow structure
Docker build
```

Do not duplicate all 161 existing tests.

---

# 34. Regression Requirements

Run the full automated suite.

Verify no regressions in:

- health;
- readiness;
- login;
- Home;
- run creation;
- active run;
- progress;
- finish;
- run concurrency;
- overtakes;
- post-run summary;
- history;
- progress aggregates;
- today leaderboard;
- monthly leaderboard;
- rival;
- next goal;
- map;
- CORS;
- reset configuration;
- OpenAPI Development behavior;
- Production no-OpenAPI behavior.

Do not weaken existing tests.

---

# 35. Stage 6 Demo Acceptance Flow

A team member should be able to perform:

```text
docker compose -f docker-compose.demo.yml up --build
```

Then from a frontend or HTTP client:

```text
POST /api/auth/login
GET /api/home

POST /api/runs
PATCH progress → first overtake
PATCH progress → second overtake
PATCH progress → third overtake
PATCH progress → fourth overtake
POST finish

GET /api/home
GET /api/leaderboards/today
GET /api/leaderboards/month
GET /api/progress
GET /api/runs/history
GET /api/map/activity?period=today
```

Expected narrative remains:

```text
#41
→ #40
→ #39
→ #38
→ #37
→ +59 points
→ next rival #36
→ 6 points to pass
```

---

# 36. Implementation Tasks

## Task 1 — Baseline

- [x] Read `AGENT.md`.
- [x] Read this `PLAN.md`.
- [x] Do not perform repository-wide analysis.
- [x] Run the existing full automated test suite.
- [x] Confirm Stage 5 baseline is green.

## Task 2 — Frontend Handoff Document

- [x] Add `docs/MVP_FRONTEND_HANDOFF.md`.
- [x] Document API base URL configuration.
- [x] Document screen-to-endpoint mapping.
- [x] Document login limitation.
- [x] Document Home bootstrap.
- [x] Document run lifecycle.
- [x] Document progress payload rules.
- [x] Document error/recovery behavior.
- [x] Document activity-map privacy semantics.
- [x] Document contract freeze rule.
- [x] Do not describe unimplemented features as available.

## Task 3 — Local Demo Compose

- [x] Add `docker-compose.demo.yml`.
- [x] Run API from existing Dockerfile.
- [x] Run MongoDB as a separate service.
- [x] Use a dedicated demo database.
- [x] Run API in Production mode.
- [x] Enable seed + reset for deterministic startup.
- [x] Add Mongo health check.
- [x] Add API dependency/readiness behavior.
- [x] Expose API on `localhost:8080`.
- [x] Keep production Dockerfile single-service.

## Task 4 — Frontend Contract Integration Test

- [x] Add focused MVP frontend-flow API integration coverage.
- [x] Verify login → Home.
- [x] Verify start → progress → overtake.
- [x] Verify finish → Home fallback.
- [x] Verify dedicated screen endpoints.
- [x] Verify important JSON field names.
- [x] Avoid duplicating service unit tests.

## Task 5 — Production-Mode Release Smoke

- [x] Add `tests/Stage6.ReleaseSmoke.ps1`.
- [x] Build actual Dockerfile.
- [x] Run API as Production.
- [x] Use isolated Mongo test state.
- [x] Verify `/health`.
- [x] Verify `/health/ready`.
- [x] Verify Production OpenAPI is unavailable.
- [x] Verify clean Home.
- [x] Verify full four-overtake flow.
- [x] Verify 59 points and #37.
- [x] Verify 6-point rival.
- [x] Verify CORS.
- [x] Verify history/progress.
- [x] Verify collections unchanged.
- [x] Guarantee cleanup on failure.

## Task 6 — Remote Smoke

- [x] Add `tests/Stage6.RemoteSmoke.ps1`.
- [x] Require `-BaseUrl`.
- [x] Add optional `-FrontendOrigin`.
- [x] Add optional `-FullDemo`.
- [x] Keep default mode non-destructive.
- [x] Verify health/readiness.
- [x] Verify login/Home/read endpoints.
- [x] Verify optional CORS.
- [x] Fail loudly on contract mismatch.
- [x] Never require DB credentials.
- [x] Never hard-code deployment URL.

## Task 7 — CI

- [x] Add `.github/workflows/backend-ci.yml`.
- [x] Run on push.
- [x] Run on pull requests.
- [x] Setup .NET 10.
- [x] Restore.
- [x] Build Release.
- [x] Test Release.
- [x] Use no cloud/Mongo secrets.
- [x] Keep cloud deployment out of this workflow.

## Task 8 — Deployment Runbook

- [x] Add deployment requirements to handoff or focused deployment doc.
- [x] Document required environment variables.
- [x] Document Mongo connectivity requirements.
- [x] Document CORS frontend origin.
- [x] Document reset-on-start behavior.
- [x] Document readiness endpoint.
- [x] Document TLS termination expectation.
- [x] Keep provider-neutral.

## Task 9 — OpenAPI / Contract Verification

- [x] Verify Development exposes OpenAPI.
- [x] Verify Production does not.
- [x] Verify Stage 6 did not alter existing route paths.
- [x] Verify successful response property names required by frontend remain stable.
- [x] Do not introduce API version routing.

## Task 10 — Full Regression

- [x] Build solution.
- [x] Run all automated tests.
- [x] Verify Stages 1–5.
- [x] Verify Stage 6 tests.
- [x] Build Docker image.
- [x] Run Stage 5 Mongo smoke if practical/relevant.
- [x] Run Stage 6 Release smoke.
- [x] Run local demo Compose flow.

## Task 11 — Documentation

- [x] Update README to Stage 6 / MVP demo release candidate.
- [x] Link frontend handoff.
- [x] Document Compose demo start/stop.
- [x] Document release smoke.
- [x] Document remote smoke.
- [x] Document CI.
- [x] Document deployment boundary.
- [x] State that backend Core MVP feature work is frozen for the hackathon.

---

# 37. Acceptance Criteria

Stage 6 is complete only when:

- [x] Stage 1 behavior still works.
- [x] Stage 2 behavior still works.
- [x] Stage 3 behavior still works.
- [x] Stage 4 behavior still works.
- [x] Stage 5 behavior still works.
- [x] Full automated suite passes.
- [x] No existing MVP route is removed or renamed.
- [x] No successful MVP JSON contract is broken.
- [x] `docs/MVP_FRONTEND_HANDOFF.md` exists.
- [x] Handoff accurately maps frontend screens to real endpoints.
- [x] Handoff documents demo-auth limitation.
- [x] Handoff documents monotonic progress requirements.
- [x] Handoff documents common error recovery.
- [x] Handoff documents API base URL configuration.
- [x] Handoff documents contract-freeze expectations.
- [x] `docker-compose.demo.yml` exists.
- [x] Demo Compose runs API + Mongo.
- [x] API runs in Production mode in Compose.
- [x] Demo Compose produces deterministic clean state.
- [x] Demo Compose preserves the existing Dockerfile architecture.
- [x] Frontend contract integration flow is covered automatically.
- [x] `tests/Stage6.ReleaseSmoke.ps1` exists.
- [x] Release smoke verifies Production OpenAPI is not exposed.
- [x] Release smoke verifies full deterministic gameplay flow.
- [x] Release smoke verifies CORS.
- [x] Release smoke cleans up resources.
- [x] `tests/Stage6.RemoteSmoke.ps1` exists.
- [x] Remote smoke accepts arbitrary `-BaseUrl`.
- [x] Remote smoke default mode is non-destructive.
- [x] Full remote demo flow is opt-in.
- [x] Remote smoke requires no Mongo credentials.
- [x] `.github/workflows/backend-ci.yml` exists.
- [x] CI restores/builds/tests .NET 10 solution.
- [x] CI requires no production secrets.
- [x] README documents Stage 6 release usage.
- [x] Deployment runbook remains provider-neutral.
- [x] No cloud credentials are committed.
- [x] No new MongoDB collections are introduced.
- [x] No new product module is introduced.
- [x] Real user scoring logic is unchanged.
- [x] Run rewards are unchanged.
- [x] Rival logic is unchanged.
- [x] NextGoal priority is unchanged.
- [x] Activity-map privacy behavior is unchanged.
- [x] Docker image still runs as non-root.
- [x] Development OpenAPI still works.
- [x] Production OpenAPI remains disabled.
- [x] Local fallback demo can be launched with one command.
- [x] Backend is ready for direct frontend integration.

---

# 38. Definition of Stage 6 Success

Stage 6 succeeds when the backend can be handed to a frontend developer with no repository archaeology required.

The frontend developer should need only:

```text
1. API_BASE_URL
2. docs/MVP_FRONTEND_HANDOFF.md
3. the documented API sequence
```

The backend team should be able to verify a remote deployment with:

```powershell
pwsh -NoProfile -File tests/Stage6.RemoteSmoke.ps1 `
  -BaseUrl https://your-api-host
```

And a hackathon fallback should be launchable locally with:

```bash
docker compose -f docker-compose.demo.yml up --build
```

At this point:

```text
backend MVP feature development stops
→ frontend connects to API
→ deploy container
→ run RemoteSmoke
→ polish demo
```

---

# 39. What Comes After Stage 6

Do not implement this section during Stage 6.

The next work is no longer another backend feature stage.

It should be:

```text
Frontend implementation/integration
+
real hosting-provider deployment
+
deployed end-to-end verification
+
hackathon presentation polish
```

If frontend integration discovers a concrete backend blocker, fix that blocker with the smallest backward-compatible change.

Do not resume speculative backend feature development before the MVP demo.

## Stage 6 execution evidence — 2026-10-03

- Baseline: `dotnet test` — 161 passed (96 unit, 65 integration).
- Final: `dotnet build CitySurfers.sln -c Release --no-restore` — zero warnings/errors; `dotnet test CitySurfers.sln -c Release --no-build --no-restore` — 162 passed (96 unit, 66 integration).
- Focused `MvpFrontendContractApiTests` passed; existing Development schema/path tests and Production OpenAPI absence passed. No API/Application/Domain/Infrastructure source was changed.
- Actual Dockerfile build and `Stage6.ReleaseSmoke.ps1` passed: isolated Production API/Mongo, health/readiness, OpenAPI 404, allow/deny/preflight CORS, four overtakes, 59 points/rank 37, six-point rival, history/progress, only runs/users, non-root runtime.
- Release cleanup passed on success and intentionally occupied-port failure, including a container partially created by failed docker run. All temporary containers/networks/volumes were removed.
- Demo Compose config/build/start passed using a separate validation project; remote FullDemo completed against localhost:8080. API restart restored rank 41/zero points/idle. Validation Compose resources were removed with down -v.
- Remote smoke default preserved Home exactly; trailing slashes, configured CORS and opt-in FullDemo passed. Dirty-state FullDemo and disallowed-origin checks returned non-zero. No DB credentials were used.
- Existing Stage5.MongoSmoke.ps1 passed against the Stage 6 image and its own temporary database; persistence/reset/isolation/index checks remained green.
- PowerShell files parsed without errors. Workflow YAML parsed by the installed Compose YAML parser (expected rejection by Compose's service schema); GitHub workflow triggers, .NET 10 setup and restore/build/test steps reviewed. The equivalent Release build/test gate passed locally; hosted CI execution requires pushing the workflow.
- Handoff fields were checked against controllers/contracts and the integration flow; README/document relative links checked. Credential-file ignore coverage preserves the existing local credential file after normalizing the docs directory casing; no credential content was read or committed.
- Deviation: after repeated registry EOF failures resolving floating .NET 10 tags, the two existing Dockerfile base references were pinned to the official .NET 10 digests already used by Stage 5 local validation. The real Dockerfile then built successfully; multi-stage single-service/non-root architecture remains intact.
- External boundary: no provider, credentials or public deployed API URL was supplied. Remote smoke was validated on local HTTP containers; actual hosted connectivity, TLS and browser frontend integration are not claimed. These are deployment/integration follow-ups already outside Stage 6 source-code execution.