# PLAN.md — Stage 5: Frontend Integration Readiness + Deterministic MVP Demo

## 0. Execution Directive

This file is the source of truth for Stage 5.

The implementing AI agent must:

1. Read `AGENT.md` first and follow it as the authoritative engineering and behavioral rule set.
2. Read this `PLAN.md`.
3. Do **not** perform a repository-wide analysis.
4. Do **not** create another roadmap or implementation plan.
5. Do **not** re-evaluate whether this is the correct next stage.
6. Do **not** redesign Stage 1–4.
7. Inspect only files directly required by the current unfinished task and their immediate dependencies.
8. Start implementing the first unfinished task immediately.
9. Complete tasks in the order defined here unless a direct dependency requires a small local reordering.
10. Preserve the existing N-Layer architecture and current project conventions.
11. Reuse existing services, providers, stores, error handling, MongoDB setup, DI, test infrastructure, and Docker setup.
12. Do not introduce new infrastructure unless this plan explicitly requires it.
13. Keep all MVP/demo-only behavior isolated and replaceable.
14. Validate meaningful changes with focused tests while working.
15. Run the full regression suite before declaring the stage complete.
16. A small mismatch between this plan and the real code is **not** permission to scan the whole repository.
17. If a referenced symbol has a slightly different name, find the direct equivalent, make the smallest safe adaptation, and continue.
18. Do not start frontend implementation or Stage 6.

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

Do not use this workflow:

```text
Analyze repository
→ create architecture report
→ invent roadmap
→ redesign modules
→ ask for approval
```

---

# 1. Verified Starting Point

Stages 1–4 are complete.

The backend currently provides:

- ASP.NET Core / .NET 10 API;
- N-Layer modular architecture;
- MongoDB persistence;
- persistent demo user;
- demo authentication;
- server-side current-user resolution;
- run start / progress / finish lifecycle;
- deterministic demo run competition;
- overtake events;
- persistent run points;
- post-run summary;
- run history;
- personal progress;
- daily leaderboard;
- monthly leaderboard;
- privacy-safe Kraków activity map;
- monthly rival;
- primary next-goal selection;
- Docker image;
- liveness and Mongo readiness health checks;
- centralized Problem Details handling;
- configurable CORS;
- automated unit/integration tests;
- real Mongo smoke tests.

Important existing modules/components:

```text
ICurrentUserAccessor
IUserStore

RunSession
RunSessionService
IRunSessionStore
IRunCompetitionProvider
DemoRunCompetitionProvider

RunHistoryService
IRunHistoryReader
ProgressService

LeaderboardService
ILeaderboardProvider
DemoLeaderboardProvider

RivalService
NextGoalService

IActivityMapProvider
DemoKrakowActivityMapProvider
```

Do not replace these components.

---

# 2. Stage 4 Review — Preserve Existing Behavior

Stage 4 correctly added:

```http
GET /api/rivals/current
GET /api/goals/next
```

It correctly:

- derives the monthly rival from the existing monthly leaderboard;
- selects the competitor directly above the current user;
- returns `rival: null` for rank #1;
- calculates `pointsGap` and `pointsToPass`;
- prefers an active-run overtake target;
- falls back to the monthly rival;
- returns a valid null-goal state;
- reuses `IRunCompetitionProvider`;
- reuses `LeaderboardService`;
- introduces no rival persistence or new Mongo collection;
- keeps controllers thin;
- keeps semantic data instead of localized UI text.

Do not rewrite Stage 4 logic.

---

# 3. Why Stage 5 Exists

The Core MVP backend mechanics already exist.

The next backend work must make the system:

```text
easy for frontend to consume
+
repeatable for hackathon demos
+
coherent across run / leaderboard / rival modules
+
safe to configure for browser integration
```

There are three important gaps.

## Gap A — Home Requires Multiple Requests

The frontend currently needs several calls to build the initial Home / Live Run state:

```text
login
active run
today leaderboard
next goal
```

Stage 5 adds one focused frontend home/bootstrap read model.

## Gap B — Fresh Monthly Rival Is Not Reachable Enough

Current demo monthly competitor scores are much larger than the run reward ladder.

A fresh user can require roughly 201 points to pass the monthly rival, while one complete deterministic demo run awards only:

```text
16 + 14 + 11 + 18 = 59 points
```

That conflicts with the product principle of short, reachable next objectives.

Stage 5 fixes only the **replaceable demo competitor population**. It must not change real current-user scoring.

## Gap C — Demo State Accumulates

Persisted runs correctly survive restarts, but repeated hackathon presentations can move the demo user to different ranks or eventually to a null-rival state.

Stage 5 adds an explicit opt-in startup reset for the demo user's runs.

It must be disabled by default and must never be exposed through a public HTTP reset endpoint.

---

# 4. Stage Goal

At the end of Stage 5:

1. the frontend can load its initial core state from `GET /api/home` after demo login;
2. Home returns `200` even when there is no active run;
3. Home contains today's current-user rank and points;
4. Home contains a nullable compact active-run summary;
5. Home contains the existing primary next goal;
6. Home reuses existing leaderboard and goal logic;
7. the demo monthly leaderboard uses the same MVP point ladder as the daily demo leaderboard;
8. a clean demo starts around rank `#41` with a reachable rival;
9. one complete four-overtake demo run produces coherent `#41 → #37` progression;
10. after that run, the next monthly rival is only a small number of points away;
11. demo run history can be intentionally reset on startup through configuration;
12. reset is disabled by default;
13. reset affects only the demo user's runs;
14. CORS behavior is explicitly tested;
15. API examples cover the complete frontend demo flow;
16. OpenAPI contains the Home endpoint;
17. Docker + real Mongo smoke proves the deterministic showcase;
18. no frontend code is added yet.

---

# 5. Intended Deterministic Demo Narrative

After an intentional demo reset, the backend should support this story.

## Fresh state

```text
Today:
#41
0 pts

Next goal:
~9 points to pass #40
```

## Start run

```text
Runner_92
1200 m to overtake
+16 potential points
#41 → #40
```

## Overtake progression

Existing run reward totals are:

```text
0
16
30
41
59
```

The clean demo leaderboard should align approximately as:

```text
0  → #41
16 → #40
30 → #39
41 → #38
59 → #37
```

## After finish

Expected showcase state:

```text
Current monthly points: 59
Current rank: #37
Next rival rank: #36
Next rival points: 64
Points gap: 5
Points to pass: 6
```

These numbers are deterministic demo presentation data, not permanent product scoring rules.

---

# 6. Demo Leaderboard Alignment

Modify only the replaceable demo leaderboard population.

Do not change:

- real current-user point aggregation;
- run reward calculation;
- overtake rewards;
- target distance thresholds;
- `LeaderboardService` aggregation;
- period boundaries;
- rank sorting;
- rival selection;
- `pointsToPass` calculation.

## Stage 5 Demo Rule

For MVP/demo purposes:

```text
LeaderboardPeriod.Today competitor points
=
existing daily demo point ladder

LeaderboardPeriod.Month competitor points
=
the same demo point ladder
```

The existing ladder around the user is approximately:

```text
8
24
36
50
64
...
```

This is intentionally compatible with cumulative run rewards:

```text
0
16
30
41
59
```

Result:

```text
#41 → #40 → #39 → #38 → #37
```

Do not modify `ILeaderboardProvider`.

Do not create persisted leaderboard rows.

Do not create a new scoring engine.

---

# 7. Cross-Module Demo Consistency Tests

Add focused tests proving clean-state coherence.

Minimum expected ranks:

```text
0 points  → #41
16 points → #40
30 points → #39
41 points → #38
59 points → #37
```

Verify this for the demo monthly leaderboard and, where already covered, the daily leaderboard.

At 59 monthly points verify:

```text
current rank = #37
rival rank = #36
rival points = 64
pointsGap = 5
pointsToPass = 6
```

If deterministic tie-breaking requires a tiny adjustment, preserve the one-position-per-overtake narrative around the clean demo state.

Do not change the run competition model to force tests to pass.

---

# 8. Home Read Model

Add:

```text
CitySurfers.Application
└── Home
    ├── HomeModels.cs
    └── HomeService.cs
```

Add:

```text
CitySurfers.Api
└── Controllers
    └── HomeController.cs
```

This is a frontend bootstrap/read model, not a new business domain.

Do not move logic out of existing modules into Home.

---

# 9. Home API

Add:

```http
GET /api/home
```

The endpoint returns `200` for normal application state, including when there is no active run.

Recommended conceptual model:

```text
HomeResponse
    Today
        Rank
        Points
    ActiveRun?
        Id
        StartedAtUtc
        DistanceMeters
        DurationSeconds
        AveragePaceSecondsPerKm
    NextGoal?
```

Reuse existing types where appropriate.

In particular:

- reuse the existing current-user leaderboard rank/points model for `Today` if clean;
- reuse the existing Stage 4 `NextGoal` model;
- do not create another next-goal DTO with duplicate semantics.

## Example — idle

```json
{
  "today": {
    "rank": 41,
    "points": 0
  },
  "activeRun": null,
  "nextGoal": {
    "type": "rival_points",
    "source": "monthly_leaderboard",
    "targetDisplayName": "<deterministic competitor>",
    "runId": null,
    "remainingDistanceMeters": null,
    "remainingPoints": 9,
    "potentialPoints": null,
    "currentRank": 41,
    "targetRank": 40
  }
}
```

## Example — active run

```json
{
  "today": {
    "rank": 41,
    "points": 0
  },
  "activeRun": {
    "id": "run-id",
    "startedAtUtc": "2026-10-03T18:00:00Z",
    "distanceMeters": 600,
    "durationSeconds": 180,
    "averagePaceSecondsPerKm": 300
  },
  "nextGoal": {
    "type": "run_overtake",
    "source": "active_run",
    "targetDisplayName": "Runner_92",
    "runId": "run-id",
    "remainingDistanceMeters": 600,
    "remainingPoints": null,
    "potentialPoints": 16,
    "currentRank": 41,
    "targetRank": 40
  }
}
```

Do not hard-code competitor names in Home logic.

---

# 10. HomeService Responsibilities

Implement `HomeService` as orchestration only.

Preferred dependencies:

```text
ICurrentUserAccessor
IRunSessionStore
LeaderboardService
NextGoalService
```

Responsibilities:

1. resolve current user id;
2. obtain today's leaderboard from `LeaderboardService`;
3. read the optional active run from `IRunSessionStore`;
4. obtain the existing primary goal from `NextGoalService`;
5. compose the Home response.

Do not duplicate:

- leaderboard calculations;
- current-user point aggregation;
- rival selection;
- goal priority;
- competition target calculation;
- pace calculation;
- Mongo-specific code.

A second active-run read caused by calling `NextGoalService` is acceptable for this MVP.

Do not add caching or a complex shared query context solely to avoid one small duplicate read.

---

# 11. Home Normal States

`GET /api/home` must treat these as normal:

```text
no active run
no rival
no next goal
```

Return nullable fields and HTTP `200`.

Do not change the existing behavior of:

```http
GET /api/runs/active
```

Stage 5 adds a frontend-friendly bootstrap endpoint without breaking Stage 1–4 contracts.

---

# 12. Home Must Stay Small

Do not include in Home:

- full run history;
- full progress analytics;
- top 10 leaderboard;
- full monthly leaderboard;
- activity-map zones;
- routes;
- achievements;
- AI Coach text;
- races;
- future social data.

Dedicated screens continue using dedicated endpoints:

```text
/api/leaderboards/*
/api/progress
/api/map/activity
/api/runs/history
/api/rivals/current
```

---

# 13. Deterministic Demo Reset

Extend `DemoDataOptions` with:

```text
ResetRunsOnStartup
```

Default:

```text
false
```

Environment key:

```text
DemoData__ResetRunsOnStartup
```

Add to `.env.example`:

```text
DemoData__ResetRunsOnStartup=false
```

This is an explicit hackathon/demo reset switch.

---

# 14. Reset Safety Rules

When reset is `false`, current persistence behavior remains unchanged.

When reset is `true`, startup initialization must:

1. ensure the demo user exists;
2. resolve the actual persisted demo-user id;
3. delete only runs belonging to that demo user;
4. preserve the demo user document;
5. preserve indexes;
6. preserve non-demo users;
7. preserve runs belonging to other users;
8. continue normal startup.

Do not:

- drop the database;
- drop `runs`;
- drop `users`;
- remove indexes;
- delete every run;
- create a public reset endpoint;
- expose database credentials.

Mongo deletion logic stays in Infrastructure/startup initialization.

Do not place it in Application or controllers.

---

# 15. Reset Configuration Validation

Invalid configuration:

```text
ResetRunsOnStartup = true
SeedOnStartup = false
```

must fail startup through strongly typed options validation with a sanitized clear message.

Do not silently ignore this invalid combination.

The reset option is disabled by default.

It may be intentionally enabled for a deployed hackathon demo, but only through operator configuration.

There must be no HTTP reset surface.

---

# 16. CORS Frontend Readiness

Keep the existing configurable CORS implementation.

Do not use `AllowAnyOrigin` in Production.

Do not enable credentials.

Add integration tests for:

## Allowed origin

Configured origin:

```text
https://frontend.example
```

must receive the expected:

```http
Access-Control-Allow-Origin
```

header.

## Disallowed origin

A different origin must not receive an allow-origin header.

## Preflight

Verify an `OPTIONS` preflight for a mutation such as:

```http
PATCH /api/runs/{runId}/progress
```

with the configured origin, method, and content-type header.

Do not hard-code a real deployment domain into source code.

---

# 17. API Contract Stability

Do not remove or rename existing Stage 1–4 endpoint paths:

```http
POST /api/auth/login

POST /api/runs
GET /api/runs/active
GET /api/runs/{runId}
PATCH /api/runs/{runId}/progress
POST /api/runs/{runId}/finish
GET /api/runs/history

GET /api/progress

GET /api/leaderboards/today
GET /api/leaderboards/month

GET /api/rivals/current
GET /api/goals/next

GET /api/map/activity
```

Add only:

```http
GET /api/home
```

Do not rename old JSON properties.

Do not wrap all old responses in new envelopes.

Do not add API versioning or GraphQL.

---

# 18. Authentication Boundary

Do not implement real authentication in Stage 5.

Current demo behavior remains:

```text
POST /api/auth/login
→ validate fictional demo credentials
→ return demo user data
→ no JWT/session
```

Other endpoints continue to resolve the demo user server-side.

Do not add:

- JWT;
- refresh tokens;
- ASP.NET Identity;
- OAuth;
- authorization policies;
- client-provided user id.

Real authentication is a later replacement.

---

# 19. Error Handling

Preserve the current centralized Problem Details behavior.

Do not rewrite the exception handler unless a concrete Stage 5 test exposes a frontend contract problem.

Home normal states return `200`.

Unexpected errors remain sanitized.

Do not expose:

- Mongo driver messages;
- credentials;
- stack traces;
- internal provider details.

---

# 20. OpenAPI

Verify OpenAPI includes:

```http
GET /api/home
```

with a correct `200` response schema.

Verify existing Stage 1–4 paths remain present.

Development-only OpenAPI remains acceptable.

Do not add Swagger UI dependencies solely for Stage 5.

---

# 21. CitySurfers.Api.http

Expand:

```text
src/CitySurfers.Api/CitySurfers.Api.http
```

into a complete manual demo/frontend flow.

Include at minimum:

```text
health
readiness
login
home
start run
active run
progress update
finish run
home after finish
run history
progress
today leaderboard
monthly leaderboard
current rival
next goal
activity map
```

Use variables for host and run id where practical.

Do not hard-code deployment secrets.

---

# 22. Home Tests

Minimum cases:

### Case 1 — Fresh State

Verify:

```text
Today rank/points present
ActiveRun = null
NextGoal present when rival exists
```

### Case 2 — Active Run

Verify active-run summary contains:

```text
id
startedAtUtc
distance
duration
pace
```

### Case 3 — Active Run Goal

Verify Home returns existing `run_overtake` goal while a target exists.

### Case 4 — Finished / Idle

Verify Home returns `ActiveRun = null` and Stage 4 fallback goal.

### Case 5 — Null Goal

If no active target and no rival:

```text
NextGoal = null
```

and Home remains `200`.

Test behavior, not internal call counts.

---

# 23. Demo Leaderboard Tests

Add/update deterministic tests for:

```text
0  → #41
16 → #40
30 → #39
41 → #38
59 → #37
```

Verify the relevant progression for Month after Stage 5 alignment.

Also verify:

```text
59 monthly points
→ rival #36
→ rival points 64
→ pointsGap 5
→ pointsToPass 6
```

Do not change real current-user point aggregation tests.

---

# 24. Demo Reset Tests

Add the smallest practical automated coverage for:

- default reset value is `false`;
- reset=true + seed=false fails options validation;
- reset-disabled configuration preserves normal startup behavior.

Real deletion semantics must be verified in the Stage 5 Mongo smoke flow.

If existing test infrastructure supports Mongo-backed initializer testing without introducing a new framework, also test targeted deletion there.

Do not build a new Mongo test framework solely for this feature.

---

# 25. CORS Tests

Add API integration tests for:

```text
allowed origin
disallowed origin
preflight
```

Use configuration overrides and existing replacement stores/providers.

Do not require real MongoDB for CORS integration tests.

---

# 26. Regression Validation

Run the complete existing test suite.

Verify no regression in:

- login;
- Mongo configuration;
- seeding;
- health/readiness;
- run start;
- active run;
- progress;
- finish;
- concurrency;
- overtake uniqueness;
- post-run summary;
- history;
- personal progress;
- daily leaderboard;
- monthly leaderboard;
- map;
- rival;
- next goal;
- error middleware;
- OpenAPI.

Where old tests assert previous demo monthly competitor totals, update only those expectations to the new Stage 5 demo dataset.

Do not weaken tests for real user scoring.

---

# 27. Stage 5 Real Mongo / Docker Smoke

Add:

```text
tests/Stage5.MongoSmoke.ps1
```

Reuse the Stage 2–4 smoke approach.

Prefer a dedicated temporary database when practical, for example:

```text
citysurfers_stage5_smoke
```

Do not destroy unrelated developer data.

Suggested flow:

```text
1. Build Stage 5 Docker image.
2. Start API with local MongoDB and:
   SeedOnStartup=true
   ResetRunsOnStartup=true
   configured test CORS origin.
3. Verify /health.
4. Verify /health/ready.
5. Login demo user.
6. GET /api/home.
7. Verify:
   today rank = 41
   today points = 0
   activeRun = null
   reachable rival goal exists.
8. Verify allowed-origin CORS.
9. Verify mutation preflight.
10. POST /api/runs.
11. GET /api/home.
12. Verify:
    activeRun exists
    primary goal = run_overtake
    ranks = 41 → 40.
13. Cross first target.
14. Verify:
    points = 16
    today rank = 40
    next run target exists.
15. Cross all four targets.
16. Finish run.
17. Verify:
    points earned = 59
    run rank = 41 → 37.
18. GET /api/leaderboards/today.
19. Verify rank = 37.
20. GET /api/leaderboards/month.
21. Verify clean showcase rank = 37.
22. GET /api/rivals/current.
23. Verify:
    rival rank = 36
    rival points = 64
    pointsGap = 5
    pointsToPass = 6.
24. GET /api/home.
25. Verify:
    activeRun = null
    nextGoal = rival_points
    remainingPoints = 6.
26. Verify history contains completed run.
27. Verify progress reflects completed run.
28. Restart with ResetRunsOnStartup=false.
29. Verify run persists.
30. Restart with ResetRunsOnStartup=true.
31. Verify:
    demo history is empty
    demo user still exists
    clean Home state is restored.
32. Verify no new Mongo collections.
33. Stop/remove test container.
34. Clean temporary database if created.
```

The script must fail on contract mismatches.

Do not rely on manual inspection.

---

# 28. Docker Validation

Build the existing repository Dockerfile.

Preserve:

- multi-stage build;
- non-root runtime user;
- port `8080`;
- no embedded secrets.

Do not add MongoDB into the API image.

Do not build hosting-provider-specific orchestration in Stage 5.

---

# 29. Documentation

Update README from Stage 4 to Stage 5.

Document:

- `GET /api/home`;
- Home response purpose;
- frontend call sequence;
- aligned deterministic demo leaderboard;
- `DemoData__ResetRunsOnStartup`;
- default `false`;
- warning that enabling it deletes only demo-user runs on API startup;
- CORS configuration;
- Stage 5 smoke command;
- deterministic demo narrative.

Recommended frontend call sequence:

```text
POST /api/auth/login
GET  /api/home

# dedicated screens / lazy loading
GET /api/map/activity
GET /api/leaderboards/today
GET /api/leaderboards/month
GET /api/progress
GET /api/runs/history
```

During run:

```text
POST  /api/runs
PATCH /api/runs/{runId}/progress
POST  /api/runs/{runId}/finish
GET   /api/home
```

Do not rewrite unrelated README sections.

---

# 30. Explicit Non-Goals

Do not implement during Stage 5:

- frontend/mobile code;
- React / React Native / Flutter integration;
- frontend project scaffolding;
- real production deployment;
- hosting-provider-specific setup;
- CI/CD;
- JWT;
- ASP.NET Identity;
- OAuth;
- real multi-user authentication;
- roles/permissions;
- friends;
- persisted rivals;
- notifications;
- SignalR;
- WebSockets;
- routes;
- route rankings;
- King of Route;
- GPS ingestion;
- live individual coordinates;
- external map services;
- Strava/Garmin;
- AI Coach;
- Game Rating;
- races/matchmaking;
- achievements/levels;
- anti-cheat engine;
- Redis;
- caching;
- queues/background workers;
- new database technology;
- new Mongo collections;
- API versioning;
- GraphQL;
- microservices.

---

# 31. Architectural Guardrails

Keep responsibilities separate:

```text
RunSession
    = one-run lifecycle

IRunCompetitionProvider
    = active-run deterministic competition

LeaderboardService
    = real current-user score aggregation + standings orchestration

ILeaderboardProvider
    = replaceable demo competitor population

RivalService
    = derive monthly rival

NextGoalService
    = choose one primary motivational goal

HomeService
    = compose minimal frontend home state

DemoDataSeeder / demo initialization
    = initialize/reset demo persistence when explicitly configured
```

Do not create god services such as:

```text
GameService
AppService
MegaDashboardService
```

Do not put:

- Mongo code in Application;
- reset logic in controllers;
- leaderboard math in Home;
- goal priority logic in Home;
- scoring logic in Home;
- frontend sentences in Application.

---

# 32. Implementation Tasks

## Task 1 — Baseline

- [x] Read `AGENT.md`.
- [x] Read this `PLAN.md`.
- [x] Do not perform repository-wide analysis.
- [x] Run the current full automated suite.
- [x] Confirm Stage 4 baseline is green.

## Task 2 — Align Demo Monthly Leaderboard

- [x] Update only demo competitor population.
- [x] Preserve real current-user point aggregation.
- [x] Make Month use the aligned MVP point ladder.
- [x] Preserve sorting semantics.
- [x] Verify `0 → #41`.
- [x] Verify `16 → #40`.
- [x] Verify `30 → #39`.
- [x] Verify `41 → #38`.
- [x] Verify `59 → #37`.

## Task 3 — Verify Rival Showcase State

- [x] Verify 59 monthly points → #37.
- [x] Verify rival → #36.
- [x] Verify rival points → 64.
- [x] Verify gap → 5.
- [x] Verify points-to-pass → 6.
- [x] Preserve Stage 4 rival algorithm.

## Task 4 — Home Models

- [x] Add focused Home models.
- [x] Reuse existing current-user rank model where appropriate.
- [x] Reuse existing `NextGoal`.
- [x] Add nullable compact active-run summary.

## Task 5 — HomeService

- [x] Add `HomeService`.
- [x] Reuse current-user accessor.
- [x] Reuse run store.
- [x] Reuse leaderboard service.
- [x] Reuse next-goal service.
- [x] Treat no active run as normal.
- [x] Add focused tests.

## Task 6 — Home API

- [x] Add `GET /api/home`.
- [x] Keep controller thin.
- [x] Return `200` when idle.
- [x] Add API integration tests.
- [x] Verify JSON contract.

## Task 7 — Demo Reset

- [x] Add `ResetRunsOnStartup`.
- [x] Default to false.
- [x] Add options validation.
- [x] Reject reset=true + seed=false.
- [x] Delete only demo-user runs.
- [x] Preserve users/indexes/unrelated data.
- [x] Add `.env.example` entry.

## Task 8 — CORS Tests

- [x] Test allowed origin.
- [x] Test disallowed origin.
- [x] Test preflight.
- [x] Preserve no-credentials policy.
- [x] Do not add wildcard Production CORS.

## Task 9 — API Manual Contract File

- [x] Expand `CitySurfers.Api.http`.
- [x] Cover full MVP flow.
- [x] Add Home request.

## Task 10 — OpenAPI + Regression

- [x] Build solution.
- [x] Run full test suite.
- [x] Verify Stages 1–4 remain green.
- [x] Verify Stage 5.
- [x] Verify OpenAPI contains Home.
- [x] Verify existing paths remain intact.

## Task 11 — Real Mongo / Docker Smoke

- [x] Build Stage 5 image.
- [x] Add `Stage5.MongoSmoke.ps1`.
- [x] Verify clean reset state.
- [x] Verify Home idle state.
- [x] Verify CORS/preflight.
- [x] Verify run Home state.
- [x] Verify `41→40→39→38→37` progression.
- [x] Verify 59-point finish.
- [x] Verify 6-point rival goal.
- [x] Verify persistence with reset=false.
- [x] Verify reset with reset=true.
- [x] Verify no new collections.

## Task 12 — Documentation

- [x] Update README to Stage 5.
- [x] Document Home.
- [x] Document frontend call sequence.
- [x] Document demo leaderboard alignment.
- [x] Document reset option/warning.
- [x] Document CORS.
- [x] Document smoke flow.

---

# 33. Acceptance Criteria

Stage 5 is complete only when:

- [x] Stage 1 behavior still works.
- [x] Stage 2 behavior still works.
- [x] Stage 3 behavior still works.
- [x] Stage 4 behavior still works.
- [x] Full automated suite passes.
- [x] Existing endpoint paths are preserved.
- [x] `GET /api/home` exists.
- [x] Home returns `200` while idle.
- [x] Home returns today's real current-user rank/points.
- [x] Home returns nullable active run.
- [x] Home returns the existing primary next goal.
- [x] Home does not duplicate leaderboard logic.
- [x] Home does not duplicate next-goal logic.
- [x] Home remains a small bootstrap endpoint.
- [x] Demo Month uses the aligned point ladder.
- [x] `0 → #41`.
- [x] `16 → #40`.
- [x] `30 → #39`.
- [x] `41 → #38`.
- [x] `59 → #37`.
- [x] At 59 monthly points rival is #36 at 64 points.
- [x] `pointsGap = 5`.
- [x] `pointsToPass = 6`.
- [x] Real user scoring is unchanged.
- [x] Overtake rewards are unchanged.
- [x] Target distances are unchanged.
- [x] `DemoData__ResetRunsOnStartup` exists.
- [x] Reset defaults false.
- [x] Invalid reset/seed combination is rejected.
- [x] Reset deletes only demo-user runs.
- [x] Reset preserves demo user and unrelated data.
- [x] No public reset endpoint exists.
- [x] Allowed-origin CORS is tested.
- [x] Disallowed-origin CORS is tested.
- [x] Preflight is tested.
- [x] No Production wildcard origin is added.
- [x] `CitySurfers.Api.http` covers the demo flow.
- [x] OpenAPI includes Home.
- [x] Docker image builds.
- [x] Real Mongo Stage 5 smoke passes.
- [x] Smoke proves deterministic fresh state.
- [x] Smoke proves full one-run loop.
- [x] Smoke proves persistence when reset is off.
- [x] Smoke proves intentional reset when enabled.
- [x] No new Mongo collections are introduced.
- [x] README documents frontend-ready behavior.

---

# 34. Definition of Stage 5 Success

After Stage 5, backend feature development for the hackathon MVP should stop.

The frontend should be able to implement the core demo with:

```text
login
→ home
→ start run
→ progress
→ overtake feedback
→ finish
→ home / next rival
→ ranking / map / progress screens
```

The backend should support a repeatable presentation:

```text
fresh #41
→ overtake targets
→ #37
→ ~6 points to next rival
```

without changing real user scoring logic.

---

# 35. Stage 6 — Do Not Implement Yet

Do not start Stage 6 during this plan.

After Stage 5, the next work should focus on:

```text
Frontend integration
+
actual hosting/deployment
+
end-to-end deployed smoke
+
hackathon demo polish
```

Do not add more backend product modules before frontend integration unless a concrete frontend blocker is discovered.

# Stage 5 Completion Evidence

- Solution build passed with zero warnings and errors.
- Full automated regression suite passed: 96 unit tests and 65 integration tests (161 total).
- Home tests cover idle, active metrics/goal, finished monthly fallback, null goal and compact JSON.
- Demo leaderboard tests verify both periods at 0/16/30/41/59 points; the rival integration test
  verifies rank #37, rival #36 at 64 points, gap 5 and points-to-pass 6 after a persisted demo run.
- CORS integration tests passed for allowed/disallowed origins and mutation preflight without credentials.
- OpenAPI validation passed for Home's 200 schema, its models, and all existing Stage 1–4 paths.
- Docker image citysurfers-api:stage5 built successfully; runtime user 1654 and port 8080 verified.
  Registry tag lookup returned EOF. As in Stage 4, an ignored .local/Stage5.validation.Dockerfile
  substituted only the cached .NET 10 SDK/runtime digests; the repository Dockerfile remains unchanged.
- Stage5.MongoSmoke.ps1 passed against real local MongoDB: clean Home, all four daily/monthly rank
  transitions, 59-point finish, 6-point rival, history/progress, CORS and OpenAPI, persistence with
  reset=false and deletion with reset=true. A pre-existing demo id was resolved; unrelated users,
  active/completed runs, user documents and indexes survived. Collections remained runs/users.
  The smoke-created database and API container were removed; developer data was not modified.
- README and the manual HTTP contract file document the complete frontend/demo flow.
- No Stage 6 work was implemented.

Deviation: Docker validation pinned cached base-image digests in an ignored validation copy after
registry tag lookup failed with EOF. No application or architectural deviations.
