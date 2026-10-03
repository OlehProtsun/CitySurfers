# PLAN.md — Stage 2: Running Session + Demo Competition Vertical Slice

## 0. Execution Directive

This file is the source of truth for Stage 2.

The implementing AI agent must:

1. Read `AGENT.md` first and follow its engineering rules.
2. Read this `PLAN.md`.
3. Do **not** perform a repository-wide analysis.
4. Do **not** create a new roadmap or alternative architecture.
5. Do **not** re-plan Stage 1.
6. Inspect only files directly required for the current task.
7. Start implementing the first unfinished task immediately.
8. Complete tasks in the order defined below unless a direct dependency requires a small reordering.
9. Keep the existing architecture and naming conventions where practical.
10. Validate every meaningful step with build/tests.
11. Do not introduce new infrastructure unless this plan explicitly requires it.
12. If the real code differs slightly from this plan, make the smallest safe adjustment and continue. Do not use this as a reason to analyze the entire repository.

---

# 1. Stage Status

Stage 1 is complete.

Already implemented and working:

- ASP.NET Core backend;
- N-Layer architecture;
- `CitySurfers.Api`;
- `CitySurfers.Application`;
- `CitySurfers.Domain`;
- `CitySurfers.Infrastructure`;
- Dependency Injection;
- centralized exception handling / Problem Details;
- OpenAPI;
- CORS;
- health checks;
- MongoDB;
- MongoDB Atlas connectivity;
- user persistence;
- demo user seeding;
- demo authentication;
- `POST /api/auth/login`;
- Docker support;
- unit/integration test projects.

Do not rebuild or redesign Stage 1.

---

# 2. Goal of Stage 2

Implement the first complete product vertical slice:

```text
Demo User
    ↓
Start Run
    ↓
Persist Active Run in MongoDB
    ↓
Update Run Progress
    ↓
Calculate Run Metrics
    ↓
Evaluate Demo Competition Targets
    ↓
Generate Overtake Events
    ↓
Update Demo Rank / Points
    ↓
Finish Run
    ↓
Persist Completed Run
    ↓
Return Post-Run Summary
```

At the end of Stage 2 the frontend must be able to:

1. login with the existing demo account;
2. start a run;
3. periodically send distance and duration;
4. display current pace;
5. display the runner currently ahead;
6. display distance remaining to overtake that runner;
7. receive an `OVERTAKE` event when the threshold is crossed;
8. update rank and points;
9. receive the next target;
10. finish the run;
11. display a persistent post-run summary.

---

# 3. Real vs Demo Components

## Real now

Implement as real application behavior:

- run lifecycle;
- run validation;
- distance tracking;
- duration tracking;
- average pace;
- MongoDB run persistence;
- active/completed state transition;
- persistent overtakes;
- persistent points earned during the run;
- API contracts;
- tests.

## Demo now

Keep deliberately simple and replaceable:

- starting rank;
- opponent selection;
- target distances;
- points per overtake;
- rank movement.

Temporary competition logic must be hidden behind one replaceable abstraction:

```text
IRunCompetitionProvider
        ↑
DemoRunCompetitionProvider
```

Do not hardcode opponents inside controllers, Mongo repositories, or DTO mapping.

---

# 4. Explicit Non-Goals

Do **not** implement in Stage 2:

- JWT;
- refresh tokens;
- ASP.NET Identity;
- OAuth;
- roles/permissions;
- Redis;
- SignalR;
- WebSockets;
- queues;
- background workers;
- microservices;
- MediatR/CQRS frameworks;
- event bus;
- real matchmaking;
- real leaderboard algorithm;
- real ranking algorithm;
- complex scoring formulas;
- anti-cheat;
- GPS route persistence;
- map matching;
- calories;
- heart rate;
- elevation;
- Strava/Garmin integration;
- push notifications;
- AI coach;
- achievements.

If something above is needed only to make the presentation convincing, prefer a deterministic demo implementation.

---

# 5. Demo Current User

Do not redesign authentication.

Do not add JWT.

Do not accept arbitrary `userId` from run request bodies.

Add:

```text
ICurrentUserAccessor
        ↑
DemoCurrentUserAccessor
```

`DemoCurrentUserAccessor` should resolve the existing seeded demo user server-side.

Recommended behavior:

- use configured/default username `demo`;
- load that user through the existing `IUserStore`;
- return its id.

The running module depends only on `ICurrentUserAccessor`.

Later this can be replaced by a real authenticated-user implementation without changing run use cases.

---

# 6. Domain Model

Create the minimum model required for Stage 2.

Recommended entity:

```text
RunSession
```

Required state:

```text
Id
UserId
Status
StartedAtUtc
UpdatedAtUtc
FinishedAtUtc?
DistanceMeters
DurationSeconds
AveragePaceSecondsPerKm?
StartingRank
CurrentRank
SeasonPointsEarned
Overtakes
```

Status:

```text
Active
Completed
```

Create a small overtake entity/value object:

```text
OpponentKey
OpponentDisplayName
CompletedAtUtc
DistanceThresholdMeters
PointsAwarded
RankBefore
RankAfter
```

Do not add fields for future features that Stage 2 does not use.

---

# 7. Run Lifecycle

## Start Run

A user can have only one active run.

If no active run exists:

```text
status = Active
distance = 0
duration = 0
starting rank = demo starting rank
current rank = starting rank
points = 0
overtakes = empty
```

If an active run already exists:

```text
409 Conflict
```

Do not silently create a second run.

## Update Progress

Only an active run can be updated.

Input:

```text
distanceMeters
durationSeconds
```

Validation:

- distance >= 0;
- duration >= 0;
- distance must never decrease;
- duration must never decrease;
- completed run cannot be updated.

Do not add anti-cheat or speed validation.

## Finish Run

Only an active run can be finished.

Input:

```text
distanceMeters
durationSeconds
```

Apply the same non-decreasing validation.

Then:

```text
apply final progress
evaluate newly crossed demo targets
status = Completed
finishedAtUtc = now
persist
return post-run summary
```

Finishing an already completed run must not duplicate points or overtakes.

Return:

```text
409 Conflict
```

---

# 8. Run Metrics

Implement real average pace calculation.

Formula:

```text
paceSecondsPerKm =
    durationSeconds / (distanceMeters / 1000)
```

Calculate only when:

```text
distanceMeters > 0
```

Otherwise:

```text
AveragePaceSecondsPerKm = null
```

Create a small pure component, e.g.:

```text
RunMetricsCalculator
```

It must not depend on HTTP, MongoDB, authentication, controllers, or demo opponents.

Add unit tests.

---

# 9. Demo Competition

Add:

```text
IRunCompetitionProvider
        ↑
DemoRunCompetitionProvider
```

Use deterministic demo data.

Starting rank:

```text
#41
```

Targets:

| Order | Opponent | Distance threshold | Reward |
|---:|---|---:|---:|
| 1 | Runner_92 | 1200 m | +16 |
| 2 | Marta | 2500 m | +14 |
| 3 | Runner_17 | 4000 m | +11 |
| 4 | Kamil_24 | 5500 m | +18 |

These are temporary demo values.

Do not implement a points formula.

The target itself contains its reward.

Do not create a fake leaderboard in this stage.

---

# 10. Overtake Evaluation

For every progress update:

```text
if currentDistance >= target.DistanceThreshold
and target was not already completed
then create an overtake
```

A single update may cross multiple targets.

Example:

```text
previous = 900 m
new = 2800 m
```

Expected:

```text
Runner_92 overtaken
Marta overtaken
```

Each target is awarded once only.

Every newly completed target:

1. persists one overtake;
2. decreases rank by one;
3. adds the target reward;
4. returns an `OVERTAKE` event.

Example:

```text
Start rank = 41

Runner_92:
41 -> 40
+16

Marta:
40 -> 39
+14
```

Final:

```text
CurrentRank = 39
SeasonPointsEarned = 30
```

Repeated progress above the same threshold must not award anything again.

---

# 11. Competition Snapshot

Start/progress responses must include:

```json
{
  "rank": 41,
  "seasonPointsEarned": 0,
  "currentTarget": {
    "opponent": "Runner_92",
    "distanceToOvertakeMeters": 1200,
    "potentialPoints": 16
  }
}
```

After progress:

```json
{
  "rank": 41,
  "seasonPointsEarned": 0,
  "currentTarget": {
    "opponent": "Runner_92",
    "distanceToOvertakeMeters": 190,
    "potentialPoints": 16
  }
}
```

After overtake:

```json
{
  "rank": 40,
  "seasonPointsEarned": 16,
  "currentTarget": {
    "opponent": "Marta",
    "distanceToOvertakeMeters": 1300,
    "potentialPoints": 14
  }
}
```

When all targets are completed:

```text
currentTarget = null
```

`distanceToOvertakeMeters` must never become negative.

---

# 12. Overtake Events

When progress crosses a target, return only events newly created by that request.

Example:

```json
{
  "events": [
    {
      "type": "OVERTAKE",
      "opponent": "Runner_92",
      "rankBefore": 41,
      "rankAfter": 40,
      "pointsAwarded": 16
    }
  ]
}
```

Historical overtakes remain stored on the run.

Do not use SignalR or WebSockets.

The frontend receives events through normal HTTP progress requests.

---

# 13. Persistence

Add:

```text
IRunSessionStore
        ↑
MongoRunSessionStore
```

Minimum operations:

```text
GetActiveByUserIdAsync
GetByIdAsync
AddAsync
SaveAsync
```

Exact method names may follow existing project conventions.

Do not build a generic repository framework.

---

# 14. MongoDB

Create collection:

```text
runs
```

Reuse existing MongoDB registrations.

Do not create a second `MongoClient`.

Recommended stored document:

```json
{
  "_id": "...",
  "userId": "demo-user-1",
  "status": "active",
  "startedAtUtc": "...",
  "updatedAtUtc": "...",
  "finishedAtUtc": null,
  "distanceMeters": 1100,
  "durationSeconds": 340,
  "averagePaceSecondsPerKm": 309.09,
  "startingRank": 41,
  "currentRank": 41,
  "seasonPointsEarned": 0,
  "overtakes": []
}
```

Persist all final values and overtakes after completion.

Add only indexes needed by Stage 2.

Recommended:

```text
userId + status
```

for active-run lookup.

Do not add migrations/versioning infrastructure.

---

# 15. Application Service

Create a focused application service/use case such as:

```text
RunSessionService
```

Responsibilities:

```text
StartAsync
GetActiveAsync
GetByIdAsync
UpdateProgressAsync
FinishAsync
```

It orchestrates:

```text
ICurrentUserAccessor
IRunSessionStore
IRunCompetitionProvider
RunMetricsCalculator
TimeProvider
```

Use built-in `TimeProvider` if it fits the existing project.

Do not put MongoDB or HTTP details inside the service.

Do not introduce command/query framework boilerplate.

---

# 16. API Endpoints

Use base route:

```text
/api/runs
```

## Start

```http
POST /api/runs
```

No user id in request body.

Success:

```text
201 Created
```

Example:

```json
{
  "id": "run-id",
  "status": "active",
  "startedAtUtc": "2026-10-03T16:00:00Z",
  "distanceMeters": 0,
  "durationSeconds": 0,
  "averagePaceSecondsPerKm": null,
  "competition": {
    "rank": 41,
    "seasonPointsEarned": 0,
    "currentTarget": {
      "opponent": "Runner_92",
      "distanceToOvertakeMeters": 1200,
      "potentialPoints": 16
    }
  }
}
```

If active run exists:

```text
409 Conflict
```

## Get Active Run

```http
GET /api/runs/active
```

Purpose:

- frontend reload/resume;
- proving persistence after restart.

Do not create a run from this endpoint.

## Get Run

```http
GET /api/runs/{runId}
```

Return it only if it belongs to the current demo user.

Never expose another user's run by arbitrary id.

## Update Progress

```http
PATCH /api/runs/{runId}/progress
```

Request:

```json
{
  "distanceMeters": 1010,
  "durationSeconds": 320
}
```

Example response:

```json
{
  "id": "run-id",
  "status": "active",
  "distanceMeters": 1010,
  "durationSeconds": 320,
  "averagePaceSecondsPerKm": 316.83,
  "competition": {
    "rank": 41,
    "seasonPointsEarned": 0,
    "currentTarget": {
      "opponent": "Runner_92",
      "distanceToOvertakeMeters": 190,
      "potentialPoints": 16
    }
  },
  "events": []
}
```

## Finish

```http
POST /api/runs/{runId}/finish
```

Request:

```json
{
  "distanceMeters": 6800,
  "durationSeconds": 2210
}
```

Success:

```text
200 OK
```

Return post-run summary.

---

# 17. Post-Run Summary

Return:

```text
runId
startedAtUtc
finishedAtUtc
distanceMeters
durationSeconds
averagePaceSecondsPerKm
overtakesCount
overtakes
rankBefore
rankAfter
seasonPointsEarned
nextTarget
```

Example:

```json
{
  "runId": "run-id",
  "distanceMeters": 6800,
  "durationSeconds": 2210,
  "averagePaceSecondsPerKm": 325,
  "overtakesCount": 4,
  "rankBefore": 41,
  "rankAfter": 37,
  "seasonPointsEarned": 59,
  "overtakes": [
    {
      "opponent": "Runner_92",
      "pointsAwarded": 16
    },
    {
      "opponent": "Marta",
      "pointsAwarded": 14
    }
  ],
  "nextTarget": null
}
```

Keep this response frontend-friendly for the presentation.

---

# 18. Error Mapping

Use existing centralized Problem Details handling.

Minimum behavior:

| Situation | HTTP |
|---|---:|
| Start while active run exists | 409 |
| Run not found | 404 |
| Run belongs to another user | 404 |
| Negative distance | 400 |
| Negative duration | 400 |
| Distance decreases | 400 |
| Duration decreases | 400 |
| Update completed run | 409 |
| Finish completed run | 409 |

Do not expose internal MongoDB errors.

---

# 19. Dependency Injection

Register only Stage 2 dependencies:

```text
ICurrentUserAccessor
    -> DemoCurrentUserAccessor

IRunSessionStore
    -> MongoRunSessionStore

IRunCompetitionProvider
    -> DemoRunCompetitionProvider

RunMetricsCalculator
RunSessionService
```

Reuse the existing DI style.

---

# 20. Tests

Stage 2 is not complete without tests.

## Unit — Metrics

- zero distance -> null pace;
- 1000 m / 300 s -> 300 sec/km;
- normal decimal-distance calculation.

## Unit — Lifecycle

- start creates active run;
- second start conflicts;
- progress updates run;
- distance cannot decrease;
- duration cannot decrease;
- completed run cannot update;
- finish completes run;
- completed run cannot finish twice.

## Unit — Competition

- before threshold -> no event;
- crossing first threshold -> one overtake;
- repeated update -> no duplicate;
- large jump -> multiple overtakes;
- rank changes once per overtake;
- points added once per overtake;
- next target is correct;
- distance to target never negative.

## API / Integration

At minimum:

- start endpoint contract;
- active-run conflict;
- invalid progress;
- successful progress;
- finish endpoint contract.

Automated tests must not depend on developer Atlas credentials.

---

# 21. Manual Demo Verification

After automated tests pass, verify against the configured demo MongoDB:

```text
1. Login as demo user.
2. POST /api/runs.
3. Verify a document appears in `runs`.
4. PATCH progress below 1200 m.
5. Verify no overtake.
6. PATCH progress above 1200 m.
7. Verify Runner_92 appears exactly once.
8. PATCH progress above 2500 m.
9. Verify Marta appears exactly once.
10. Restart API/container.
11. GET /api/runs/active.
12. Verify active run survived restart.
13. Finish the run.
14. Verify Mongo document becomes completed.
15. Restart API/container.
16. GET /api/runs/{id}.
17. Verify metrics/overtakes/points remain persisted.
```

---

# 22. Implementation Order

## Task 1 — Current User Boundary

- [x] Add `ICurrentUserAccessor`.
- [x] Add `DemoCurrentUserAccessor`.
- [x] Resolve the existing demo user.
- [x] Register DI.
- [x] Add focused tests.

## Task 2 — Running Domain

- [x] Add `RunSession`.
- [x] Add run status.
- [x] Add overtake model.
- [x] Add lifecycle/state validation.
- [x] Add unit tests.

## Task 3 — Metrics

- [x] Add `RunMetricsCalculator`.
- [x] Calculate average pace.
- [x] Add unit tests.

## Task 4 — Demo Competition

- [x] Add `IRunCompetitionProvider`.
- [x] Add `DemoRunCompetitionProvider`.
- [x] Add deterministic targets.
- [x] Add overtake evaluation.
- [x] Ensure idempotency.
- [x] Add unit tests.

## Task 5 — Mongo Persistence

- [x] Add `IRunSessionStore`.
- [x] Add Mongo run document mapping.
- [x] Add `MongoRunSessionStore`.
- [x] Add `runs` collection.
- [x] Add active-run index.
- [x] Reuse existing Mongo registrations.

## Task 6 — Application Service

- [x] Add `RunSessionService`.
- [x] Implement `StartAsync`.
- [x] Implement `GetActiveAsync`.
- [x] Implement `GetByIdAsync`.
- [x] Implement `UpdateProgressAsync`.
- [x] Implement `FinishAsync`.
- [x] Add service tests.

## Task 7 — API

- [x] Add request/response DTOs.
- [x] Add runs controller.
- [x] Add start endpoint.
- [x] Add get-active endpoint.
- [x] Add get-by-id endpoint.
- [x] Add progress endpoint.
- [x] Add finish endpoint.
- [x] Preserve Problem Details behavior.

## Task 8 — Validation

- [x] Build solution.
- [x] Run full test suite.
- [x] Fix regressions.
- [x] Verify OpenAPI.
- [x] Verify health endpoints.
- [x] Verify demo auth still works.
- [x] Verify Docker build.

## Task 9 — Real Mongo Demo Flow

- [x] Execute Section 21.
- [x] Verify restart persistence.
- [x] Verify no duplicated rewards.
- [x] Verify completed run loads after restart.

## Task 10 — Minimal Documentation

- [x] Update README with Stage 2 endpoints.
- [x] Add minimal curl examples.
- [x] Do not rewrite unrelated documentation.

---

# 23. Acceptance Criteria

Stage 2 is complete only when:

- [x] Stage 1 still works.
- [x] Demo login still works.
- [x] A run can be started.
- [x] Only one active run per user is allowed.
- [x] Active run persists in MongoDB.
- [x] Active run survives restart.
- [x] Progress can be updated.
- [x] Distance/duration cannot move backward.
- [x] Average pace is correct.
- [x] Demo target distance decreases with progress.
- [x] Crossing target creates `OVERTAKE`.
- [x] Overtake persists.
- [x] Rank changes exactly once per target.
- [x] Points are awarded exactly once per target.
- [x] One large progress jump can complete multiple targets.
- [x] Run can be finished.
- [x] Completed run persists.
- [x] Completed run survives restart.
- [x] Post-run summary contains competition data.
- [x] Re-finishing does not duplicate rewards.
- [x] Full automated test suite passes.
- [x] Docker build succeeds.

---

# 24. Stage 3 — Do Not Implement Yet

The likely next stage will be read-side data for the main application UI:

```text
Home / World
    ↓
Today's leaderboard
Monthly leaderboard
Runner profile / statistics
Recent runs
Kraków activity map demo data
```

Do not start Stage 3 during this plan.
