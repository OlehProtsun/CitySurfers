# PLAN.md — Stage 4: MVP Motivation Layer — Rival + Next Goal

## 0. Execution Directive

This file is the source of truth for Stage 4.

The implementing AI agent must:

1. Read `AGENT.md` first and follow it as the engineering and behavioral rule set.
2. Read this `PLAN.md`.
3. Do **not** perform a repository-wide analysis.
4. Do **not** create a new roadmap, architecture proposal, or alternative Stage 4.
5. Do **not** re-plan or re-implement Stage 1, Stage 2, or Stage 3.
6. Do **not** scan the entire repository before starting.
7. Inspect only files directly required by the current task and their immediate dependencies.
8. Start implementing the first unfinished task immediately.
9. Complete tasks in the order defined in this plan unless a direct dependency requires a small local reordering.
10. Preserve the existing N-Layer architecture and current project conventions.
11. Reuse the existing running, leaderboard, current-user, MongoDB, error-handling, DI, and testing infrastructure.
12. Do not introduce new infrastructure unless explicitly required by this plan.
13. Validate meaningful changes with build/tests as work progresses.
14. If the real code differs slightly from this plan, make the smallest safe correction and continue.
15. A small mismatch is **not** permission to perform repository-wide exploration.
16. Keep the implementation MVP-focused and replaceable.
17. Do not silently invent permanent product rules for areas marked TBD in `AppContext.md`.

---

# 1. Verified Starting Point

Stages 1–3 are complete.

The backend currently contains:

- ASP.NET Core API;
- .NET 10;
- N-Layer / modular monolith structure;
- MongoDB persistence;
- persistent demo user;
- demo authentication;
- `ICurrentUserAccessor`;
- persistent `RunSession`;
- run start / progress / finish flow;
- deterministic demo overtake targets;
- persistent run-local overtake events;
- persistent run-local points;
- current-user run history;
- personal progress aggregates;
- daily leaderboard;
- monthly leaderboard;
- deterministic demo leaderboard population;
- privacy-safe Kraków activity map;
- Kraków calendar-period resolution;
- Docker support;
- automated unit and integration tests;
- real Mongo smoke validation.

Existing important components include:

```text
RunSession
RunSessionService
IRunSessionStore
IRunCompetitionProvider
DemoRunCompetitionProvider

IRunHistoryReader
RunHistoryService

LeaderboardService
ILeaderboardProvider
DemoLeaderboardProvider

ProgressService

ICurrentUserAccessor
```

Do not replace these components.

Stage 4 must build on top of them.

---

# 2. Why Stage 4 Exists

The backend already exposes most of the raw data required by the Core MVP.

However, the frontend still has to interpret multiple endpoints to answer the most important product question:

```text
What should the runner try to achieve next?
```

The core product concept is not merely:

```text
show statistics
```

It is:

```text
show a short, achievable competitive objective
```

The product should be able to tell the user things such as:

```text
320 m to overtake Marta
```

or:

```text
14 points to pass your monthly rival
```

Stage 4 introduces the minimal motivation layer required to provide that information.

This stage must connect:

```text
Active run
    +
Run competition target
    +
Monthly leaderboard position
    +
Nearby demo competitor
        ↓
Primary next goal
```

---

# 3. Stage Goal

At the end of Stage 4 the frontend must be able to obtain:

1. the current user's monthly rival;
2. the rival's rank and points;
3. the current user's rank and points;
4. the current point gap;
5. the number of points required to move ahead of that rival;
6. one primary next goal;
7. an active-run overtake goal while a relevant run target exists;
8. a monthly rival goal when there is no active run target;
9. a valid zero-state when no goal exists;
10. all of the above without introducing real social networking or multi-user persistence.

The intended behavior is:

```text
NO ACTIVE RUN
    ↓
monthly leaderboard
    ↓
competitor directly above user
    ↓
RIVAL GOAL
"14 points to move ahead of Marta"


ACTIVE RUN
    ↓
IRunCompetitionProvider
    ↓
current overtake target
    ↓
RUN OVERTAKE GOAL
"620 m to overtake Runner_92"


ACTIVE RUN, ALL RUN TARGETS COMPLETED
    ↓
fallback
    ↓
MONTHLY RIVAL GOAL
```

Only one primary goal should be returned.

---

# 4. Product Rule: One Primary Goal

Do not return a list of competing recommendations in Stage 4.

The application needs one primary motivational objective.

Temporary Stage 4 priority:

```text
1. Active-run overtake target
2. Monthly rival
3. No goal
```

This directly supports the UX principle that during a run one objective should normally remain visually dominant.

Do not add goal scoring, AI ranking, weighted goal selection, or recommendation engines.

---

# 5. Temporary MVP Rival Rule

`AppContext.md` intentionally leaves final rival-selection rules TBD.

Stage 4 therefore uses an explicitly temporary and replaceable MVP rule.

Definition:

```text
The current monthly rival is the competitor
immediately above the current user
in the current monthly leaderboard.
```

Example:

```text
#17 Marta      1451 pts
#18 You        1438 pts
#19 Piotr      1410 pts
```

Current rival:

```text
Marta
```

Do not select:

- friends;
- previous race opponents;
- geographically nearby runners;
- users with similar pace;
- users based on Game Rating;
- random runners;
- manually persisted rivals.

Those mechanisms belong to later stages.

---

# 6. Rival Point Semantics

Return two different concepts.

## PointsGap

Informational difference:

```text
PointsGap = max(0, RivalPoints - CurrentUserPoints)
```

Example:

```text
Rival = 1451
User  = 1438

PointsGap = 13
```

## PointsToPass

Points required to unambiguously exceed the rival:

```text
PointsToPass =
    max(1, RivalPoints - CurrentUserPoints + 1)
```

Example:

```text
Rival = 1451
User  = 1438

PointsToPass = 14
```

This avoids depending on internal leaderboard tie-breaking rules.

Do not change existing leaderboard ordering or ranking semantics in this stage.

---

# 7. Rival Zero State

If the current user is already rank `#1` in the monthly leaderboard:

```text
Rival = null
```

This is not an error.

Return HTTP `200`.

Do not return `404`.

Do not manufacture a stronger fake competitor just to ensure a rival always exists.

---

# 8. Real vs Demo Boundary

## Real now

Continue deriving from actual persisted current-user run data:

- current user's monthly points;
- active-run points where existing leaderboard rules include them;
- current user's monthly leaderboard rank;
- current run;
- current run distance;
- completed overtakes;
- current run competition snapshot.

## Demo now

Continue keeping replaceable:

- other leaderboard competitors;
- other users' scores;
- rival identity;
- live run opponents;
- run target thresholds;
- run target rewards.

Do not persist demo competitors as MongoDB users.

Do not create fake run documents for demo competitors.

---

# 9. Architecture

Add two focused Application modules:

```text
CitySurfers.Application
│
├── Rivals
│   ├── RivalModels.cs
│   └── RivalService.cs
│
└── Goals
    ├── NextGoalModels.cs
    └── NextGoalService.cs
```

Add thin API controllers:

```text
CitySurfers.Api
└── Controllers
    ├── RivalsController.cs
    └── GoalsController.cs
```

Do not add another Infrastructure implementation unless a concrete dependency requires one.

The Stage 4 logic can use existing providers.

---

# 10. RivalService

Implement:

```text
RivalService
```

Responsibility:

```text
Determine the current monthly rival
using existing leaderboard data.
```

Preferred dependency:

```text
LeaderboardService
```

Do not duplicate:

- period calculations;
- run history aggregation;
- active-run point inclusion;
- current-user point calculation;
- leaderboard ranking.

Those already belong to `LeaderboardService`.

Conceptual flow:

```text
RivalService
    ↓
LeaderboardService.GetAsync(Month)
    ↓
CurrentUser.Rank
    ↓
find leaderboard row with Rank = CurrentUser.Rank - 1
    ↓
calculate gap
    ↓
RivalResponse
```

The row should normally be available through `AroundMe`.

If implementation details require a small safe fallback to `Top`, it is acceptable.

Do not make another Mongo query merely to find the rival.

---

# 11. Rival Application Models

Create a clear model similar to:

```text
RivalSnapshot
    DisplayName
    Rank
    Points
    PointsGap
    PointsToPass
```

Response should also contain current-user leaderboard information and period information.

Recommended conceptual shape:

```text
RivalResponse
    Period
    PeriodStartUtc
    PeriodEndUtc
    CurrentUser
        Rank
        Points
    Rival?
        DisplayName
        Rank
        Points
        PointsGap
        PointsToPass
```

Use existing leaderboard models where appropriate instead of creating unnecessary duplicate types.

Do not expose internal demo competitor keys unless the frontend actually requires them.

---

# 12. Rival API

Add:

```http
GET /api/rivals/current
```

Stage 4 rival period:

```text
month
```

No query parameter is required.

Example response:

```json
{
  "period": "month",
  "periodStartUtc": "2026-09-30T22:00:00Z",
  "periodEndUtc": "2026-10-31T23:00:00Z",
  "currentUser": {
    "rank": 18,
    "points": 1438
  },
  "rival": {
    "displayName": "Marta_17",
    "rank": 17,
    "points": 1451,
    "pointsGap": 13,
    "pointsToPass": 14
  }
}
```

Top-rank zero-state example:

```json
{
  "period": "month",
  "periodStartUtc": "...",
  "periodEndUtc": "...",
  "currentUser": {
    "rank": 1,
    "points": 4000
  },
  "rival": null
}
```

The controller must remain thin.

No rival-selection logic belongs in the controller.

---

# 13. NextGoalService

Implement:

```text
NextGoalService
```

Responsibility:

```text
Return one primary actionable competitive goal.
```

Required dependencies may include:

```text
ICurrentUserAccessor
IRunSessionStore
IRunCompetitionProvider
RivalService
```

Do not use exceptions to represent the normal condition:

```text
there is no active run
```

Read the active run directly through the existing store.

---

# 14. Next Goal Selection Algorithm

Implement exactly this Stage 4 priority.

## Rule 1 — Active Run Overtake

If an active run exists:

1. obtain its competition snapshot using existing `IRunCompetitionProvider`;
2. inspect `CurrentTarget`;
3. if `CurrentTarget != null`, return a run-overtake goal.

Conceptual response:

```text
Type = run_overtake
Source = active_run
TargetDisplayName = CurrentTarget.Opponent
RemainingDistanceMeters = CurrentTarget.DistanceToOvertakeMeters
PotentialPoints = CurrentTarget.PotentialPoints
CurrentRank = CompetitionSnapshot.Rank
TargetRank = CompetitionSnapshot.Rank - 1
RunId = active run id
```

Do not recalculate overtake thresholds inside `NextGoalService`.

`IRunCompetitionProvider` remains the source of truth.

## Rule 2 — Monthly Rival

If:

```text
there is no active run
```

or:

```text
an active run exists but CurrentTarget is null
```

then resolve the current monthly rival.

If a rival exists:

```text
Type = rival_points
Source = monthly_leaderboard
TargetDisplayName = Rival.DisplayName
RemainingPoints = Rival.PointsToPass
CurrentRank = CurrentUser.Rank
TargetRank = Rival.Rank
```

## Rule 3 — No Goal

If neither source provides a goal:

```text
Goal = null
```

Return HTTP `200`.

---

# 15. Next Goal Model

Use semantic data.

Do not put localized UI sentences inside Application.

Recommended conceptual model:

```text
NextGoal
    Type
    Source
    TargetDisplayName
    RunId?
    RemainingDistanceMeters?
    RemainingPoints?
    PotentialPoints?
    CurrentRank?
    TargetRank?
```

Wrapper:

```text
NextGoalResponse
    Goal?
```

Allowed Stage 4 types:

```text
run_overtake
rival_points
```

Allowed sources:

```text
active_run
monthly_leaderboard
```

Do not add an enum serialization framework unless already used by the project.

Follow existing project serialization conventions.

---

# 16. Next Goal API

Add:

```http
GET /api/goals/next
```

Example during an active run:

```json
{
  "goal": {
    "type": "run_overtake",
    "source": "active_run",
    "targetDisplayName": "Marta",
    "runId": "abc123",
    "remainingDistanceMeters": 620,
    "remainingPoints": null,
    "potentialPoints": 14,
    "currentRank": 40,
    "targetRank": 39
  }
}
```

Example without an active run:

```json
{
  "goal": {
    "type": "rival_points",
    "source": "monthly_leaderboard",
    "targetDisplayName": "Marta_17",
    "runId": null,
    "remainingDistanceMeters": null,
    "remainingPoints": 14,
    "potentialPoints": null,
    "currentRank": 18,
    "targetRank": 17
  }
}
```

Example zero-state:

```json
{
  "goal": null
}
```

---

# 17. Relationship to Existing Run Competition

Do not replace:

```text
IRunCompetitionProvider
DemoRunCompetitionProvider
CompetitionSnapshot
TargetSnapshot
```

The existing run competition remains responsible for:

- deterministic run targets;
- threshold evaluation;
- overtake creation;
- potential reward;
- current run target.

Stage 4 only consumes its snapshot.

Do not move Stage 2 competition logic into the new Goals module.

---

# 18. Relationship to Existing Leaderboard

Do not replace:

```text
LeaderboardService
ILeaderboardProvider
DemoLeaderboardProvider
```

The leaderboard remains responsible for:

- period calculation;
- current-user score;
- completed run point aggregation;
- active-run point inclusion;
- demo city standings;
- rank calculation.

The rival module derives meaning from those standings.

Do not duplicate leaderboard calculations.

---

# 19. Relationship to RunSession

Do not add rival state to `RunSession`.

`RunSession` must continue to represent:

```text
one running-session lifecycle
```

It must not become responsible for:

- monthly rival identity;
- global leaderboard relationships;
- next-goal orchestration;
- season-wide motivation.

No `CurrentRival` field belongs in the run document.

---

# 20. Persistence

Stage 4 must introduce:

```text
ZERO new MongoDB collections
```

Do not create:

```text
rivals
goals
motivation
leaderboard
seasons
competitors
```

collections.

Rivals and goals are derived views for this MVP.

No migration is required.

No background job is required.

No caching layer is required.

---

# 21. Dependency Injection

Register new Application services through the existing Application DI module.

Expected additions:

```text
RivalService
NextGoalService
```

Use service lifetimes consistent with their dependencies and current conventions.

Do not introduce a service locator.

Do not manually instantiate application services inside controllers.

---

# 22. Error Handling

Normal states must not become errors.

These are valid `200` states:

```text
no active run
no rival
no next goal
active run with no remaining run target
```

Existing unexpected-error handling remains unchanged.

Do not leak:

- MongoDB details;
- stack traces;
- internal provider details.

---

# 23. API Contract Stability

Do not break existing Stage 1–3 endpoints.

Existing contracts must continue to work.

Especially preserve:

```text
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

GET /api/map/activity
```

Do not require frontend changes to existing endpoints solely for Stage 4.

---

# 24. Post-Run Summary

Do not perform a large rewrite of `PostRunSummary`.

The frontend can request:

```http
GET /api/goals/next
```

after finishing a run.

It can request:

```http
GET /api/rivals/current
```

for the current rival state.

This avoids coupling run lifecycle logic with season-wide motivation logic.

A tiny non-breaking contract improvement is allowed only if directly required by existing tests or unavoidable implementation details.

Otherwise leave the run contract unchanged.

---

# 25. Testing Strategy

Stage 4 must include focused unit tests and API integration tests.

Do not test implementation details.

Test observable behavior.

---

# 26. RivalService Unit Tests

Minimum cases:

### Case 1 — Direct Competitor Above

Given:

```text
#17 competitor
#18 current user
#19 competitor
```

Verify:

```text
rival rank = 17
```

### Case 2 — Gap

Verify:

```text
PointsGap = RivalPoints - CurrentUserPoints
```

when rival has more points.

### Case 3 — Points To Pass

Verify:

```text
PointsToPass = RivalPoints - CurrentUserPoints + 1
```

with minimum `1`.

### Case 4 — Current User Rank 1

Verify:

```text
Rival = null
```

### Case 5 — Current User Values Are Preserved

Verify response exposes the leaderboard's existing real:

```text
rank
points
period boundaries
```

Do not duplicate mocked run aggregation inside these tests if `LeaderboardService` can be represented through the smallest appropriate test seam.

---

# 27. NextGoalService Unit Tests

Minimum cases:

### Case 1 — Active Run Has Target

Verify:

```text
run_overtake
```

is returned.

### Case 2 — Active Run Goal Has Priority

Even if a monthly rival exists, verify the active run target wins.

### Case 3 — Remaining Distance Comes From Competition Snapshot

Do not independently recalculate target threshold in `NextGoalService`.

### Case 4 — No Active Run

Verify monthly rival goal is returned.

### Case 5 — Active Run Has No Remaining Target

Verify fallback to monthly rival.

### Case 6 — No Active Target and No Rival

Verify:

```text
Goal = null
```

### Case 7 — Rank Transition

For run target:

```text
TargetRank = CurrentRank - 1
```

### Case 8 — Rival Goal Uses PointsToPass

Verify the goal uses:

```text
Rival.PointsToPass
```

rather than recomputing a different value.

---

# 28. API Tests

Add API tests for:

```http
GET /api/rivals/current
GET /api/goals/next
```

Verify:

- status `200`;
- JSON contract;
- rival is monthly;
- current user data is present;
- active-run goal wins during a run;
- remaining distance decreases when run progress increases;
- after crossing a run target, the next run target is returned;
- after all demo run targets are crossed, goal falls back to monthly rival;
- no-goal response is valid;
- existing error middleware still behaves correctly.

Do not require real MongoDB for normal integration tests.

Continue using the project's replacement test stores/providers.

---

# 29. Regression Tests

Run the entire existing test suite.

Verify no regression in:

- authentication;
- health;
- Mongo configuration;
- run creation;
- progress updates;
- finish;
- concurrency behavior;
- overtake uniqueness;
- post-run summary;
- run history;
- progress aggregation;
- daily leaderboard;
- monthly leaderboard;
- activity map;
- period boundaries.

Do not weaken existing tests to make Stage 4 pass.

---

# 30. Real Mongo Stage 4 Smoke Flow

Add:

```text
tests/Stage4.MongoSmoke.ps1
```

Reuse the existing Stage 2 / Stage 3 smoke-test approach.

Do not create a new testing framework.

Suggested flow:

```text
1. Start API against local MongoDB.
2. Verify readiness.
3. Login as demo user.
4. GET /api/rivals/current.
5. Capture current rival/current-user monthly state.
6. GET /api/goals/next with no active run.
7. Verify rival_points when a rival exists.
8. Start a run.
9. GET /api/goals/next.
10. Verify run_overtake.
11. Update run progress without crossing first threshold.
12. GET /api/goals/next.
13. Verify remaining distance decreased.
14. Cross first target.
15. GET /api/goals/next.
16. Verify the next run target is returned.
17. Finish the run.
18. GET /api/goals/next.
19. Verify active-run target is no longer returned.
20. GET /api/rivals/current.
21. Verify current-user points/rank reflect persisted run data.
22. Restart API.
23. GET /api/rivals/current again.
24. Verify derived rival state is based on persisted run points after restart.
```

Do not assert a fragile hard-coded rival if accumulated local test data can change the user's monthly score.

Compare relationships and deltas where practical.

---

# 31. OpenAPI

Verify the generated OpenAPI contains:

```text
GET /api/rivals/current
GET /api/goals/next
```

and correct response models.

Do not add Swagger-specific dependencies if the current OpenAPI setup does not require them.

---

# 32. Documentation

Update README minimally.

Add:

```text
GET /api/rivals/current
GET /api/goals/next
```

Document:

- rival is derived from monthly leaderboard;
- other competitors remain deterministic demo data;
- current-user points remain derived from persisted runs;
- rival is not persisted;
- next goal prefers active-run overtake;
- monthly rival is the fallback;
- no new Mongo collections are introduced.

Include minimal curl examples.

Do not rewrite unrelated README sections.

---

# 33. Explicit Non-Goals

Do not implement during Stage 4:

- real authentication;
- JWT;
- ASP.NET Identity;
- real multi-user social accounts;
- friends;
- friend requests;
- direct messaging;
- persisted rival relationships;
- user-selected rivals;
- rival history;
- rival notifications;
- Game Rating;
- Game Rating algorithm;
- ELO/MMR;
- 1v1 races;
- matchmaking;
- WebSockets;
- SignalR;
- background workers;
- push notifications;
- route catalog;
- route rankings;
- King of Route;
- achievements;
- levels;
- AI Coach;
- GPS storage;
- real live location;
- public individual coordinates;
- geospatial MongoDB queries;
- Strava;
- Garmin;
- external map integration;
- Redis;
- a new database;
- a new Mongo client;
- complex recommendation algorithms;
- ML-based goal selection;
- production season reset jobs;
- historical season persistence.

---

# 34. Architectural Guardrails

Keep responsibilities separate:

```text
RunSession
    = one-run lifecycle and persisted run state

IRunCompetitionProvider
    = active-run targets and overtakes

LeaderboardService
    = calculate user's period score and standings

ILeaderboardProvider
    = replaceable city competition population

RivalService
    = derive current monthly rival from leaderboard

NextGoalService
    = choose one primary goal from existing systems
```

Do not merge these into one large:

```text
CompetitionService
GameService
DashboardService
HomeService
```

Do not place:

- Mongo-specific code in Application;
- demo data in Domain;
- rival-selection calculations in controllers;
- next-goal priority logic in controllers;
- leaderboard calculations in `RivalService`;
- overtake threshold calculations in `NextGoalService`.

---

# 35. Implementation Tasks

## Task 1 — Baseline Verification

- [x] Read `AGENT.md`.
- [x] Read this `PLAN.md`.
- [x] Inspect only existing leaderboard/run competition files needed for implementation.
- [x] Run the existing automated test suite before meaningful changes.
- [x] Confirm baseline is green.

## Task 2 — Rival Models

- [x] Add focused rival models.
- [x] Include current-user monthly rank and points.
- [x] Include optional rival.
- [x] Include `PointsGap`.
- [x] Include `PointsToPass`.
- [x] Include monthly period boundaries.
- [x] Do not add persistence models.

## Task 3 — RivalService

- [x] Add `RivalService`.
- [x] Reuse `LeaderboardService`.
- [x] Use monthly leaderboard.
- [x] Select rank directly above current user.
- [x] Support rank-1 zero state.
- [x] Add focused unit tests.

## Task 4 — Rival API

- [x] Add `GET /api/rivals/current`.
- [x] Keep controller thin.
- [x] Return `200` with nullable rival.
- [x] Add API integration tests.

## Task 5 — Next Goal Models

- [x] Add goal models.
- [x] Support `run_overtake`.
- [x] Support `rival_points`.
- [x] Support nullable goal.
- [x] Keep UI/localized text out of Application.

## Task 6 — NextGoalService

- [x] Resolve current user.
- [x] Read active run through existing store.
- [x] Reuse `IRunCompetitionProvider`.
- [x] Give active run target highest priority.
- [x] Fall back to `RivalService`.
- [x] Return null when neither source has a goal.
- [x] Add focused unit tests.

## Task 7 — Next Goal API

- [x] Add `GET /api/goals/next`.
- [x] Keep controller thin.
- [x] Add API tests.
- [x] Verify active-run priority.
- [x] Verify rival fallback.
- [x] Verify zero-state.

## Task 8 — Dependency Injection

- [x] Register `RivalService`.
- [x] Register `NextGoalService`.
- [x] Preserve current lifetimes and conventions.
- [x] Do not introduce service-location patterns.

## Task 9 — Regression Validation

- [x] Build solution.
- [x] Run full automated test suite.
- [x] Verify Stage 1.
- [x] Verify Stage 2.
- [x] Verify Stage 3.
- [x] Verify Stage 4.
- [x] Verify OpenAPI.
- [x] Verify Docker build.

## Task 10 — Real Mongo Demo Flow

- [x] Add/execute `Stage4.MongoSmoke.ps1`.
- [x] Verify idle rival goal.
- [x] Verify active-run goal.
- [x] Verify remaining distance changes.
- [x] Verify target changes after overtake.
- [x] Verify post-run fallback.
- [x] Verify persisted points affect rival state.
- [x] Verify API restart behavior.

## Task 11 — Documentation

- [x] Update README with Stage 4.
- [x] Document new endpoints.
- [x] Add minimal curl examples.
- [x] Document temporary rival selection rule.
- [x] Document real-vs-demo boundary.
- [x] Do not rewrite unrelated documentation.

---

# 36. Acceptance Criteria

Stage 4 is complete only when:

- [x] Stage 1 still works.
- [x] Stage 2 still works.
- [x] Stage 3 still works.
- [x] Existing tests remain green.
- [x] `GET /api/rivals/current` exists.
- [x] Rival uses the current monthly leaderboard.
- [x] Rival is the competitor immediately above the current user.
- [x] Rank #1 produces `rival: null`.
- [x] Rival response contains current-user rank and points.
- [x] Rival response contains rival rank and points.
- [x] `PointsGap` is correct.
- [x] `PointsToPass` is correct.
- [x] `GET /api/goals/next` exists.
- [x] Active run overtake target has first priority.
- [x] Active run target uses the existing competition provider.
- [x] Remaining run distance comes from existing competition state.
- [x] Monthly rival is used when no active target exists.
- [x] Goal can validly be null.
- [x] No duplicate leaderboard scoring logic was introduced.
- [x] No duplicate overtake threshold logic was introduced.
- [x] No new MongoDB collection was introduced.
- [x] No persisted fake competitors were introduced.
- [x] No persisted rival relationship was introduced.
- [x] Controllers remain thin.
- [x] Full automated test suite passes.
- [x] Docker build succeeds.
- [x] OpenAPI contains Stage 4 endpoints.
- [x] Real Mongo Stage 4 flow succeeds.
- [x] Derived rival state survives API restart because user run data persists.
- [x] README reflects the implemented behavior.

---

## Stage 4 validation evidence — 2026-10-03

- Baseline: 76 unit + 50 API tests passed before changes.
- Final solution build: zero warnings/errors; 88 unit + 57 API tests passed (145 total).
- Focused tests verify rival selection/gap/minimums/rank-one, snapshot priority, fallbacks,
  null contracts, target transitions, sanitized failures and OpenAPI response schemas.
- Docker image `citysurfers-api:stage4` built successfully. Registry tag resolution returned EOF;
  validation used an ignored `.local/Stage4.validation.Dockerfile` with only the two base-image
  references pinned to cached digests from the previous successful Stage 3 build. Repository
  `Dockerfile` is unchanged. Image digest: `sha256:87332ab48a20fbcfa342740d4cb4c3692785cffd63fdfdb836170ad0e5e711c2`.
- Stage 4 real local Mongo smoke passed, including readiness/login, idle and active goals,
  decreasing distance, next target, exhausted-target/post-run fallback, Mongo score delta,
  no new collections and derived rival state after API restart.
- Existing Stage 2 and Stage 3 real Mongo smoke scripts passed against the Stage 4 image,
  including persisted state, concurrency/reward uniqueness and read-side restart behavior.
- README updated; no new persistence or changes to Stage 1–3 business logic/contracts.

---

# 37. Expected Demo Narrative After Stage 4

The backend should support the following demo.

Before running:

```text
GET /api/goals/next

Your next goal:
14 points to pass Marta_17.
```

User starts running:

```text
POST /api/runs
```

Then:

```text
GET /api/goals/next

Runner_92
1200 m to overtake
+16 potential points
#41 → #40
```

After progress:

```text
Runner_92
350 m to overtake
```

After the overtake:

```text
Marta
next run target
```

After finishing:

```text
GET /api/goals/next

Monthly rival:
Marta_17
Only a small number of points remain.
```

This closes the MVP motivation loop:

```text
see target
→ run
→ overtake
→ earn points
→ affect leaderboard
→ get next target
→ want to run again
```

---

# 38. Stage 5 — Do Not Implement Yet

Do not start Stage 5 during this plan.

After Stage 4, the next stage should be selected separately.

Likely candidates:

```text
A. Deployment hardening + hackathon production environment
B. Route catalog + route competition demo
C. Profile + richer progression
D. AI Coach demo layer
```

For hackathon readiness, deployment hardening will likely become the highest-priority next step after the core motivational loop is complete.
