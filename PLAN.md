# PLAN.md — Stage 3: MVP Read Side — Leaderboards, Run History, Personal Progress, Kraków Activity Map

## 0. Execution Directive

This file is the source of truth for Stage 3.

The implementing AI agent must:

1. Read `AGENT.md` first and follow all engineering rules from it.
2. Read this `PLAN.md`.
3. Do **not** perform a repository-wide analysis.
4. Do **not** create a new roadmap, architecture proposal, or alternative Stage 3.
5. Do **not** re-plan or re-implement Stage 1 or Stage 2.
6. Inspect only files directly required for the current task.
7. Start implementing the first unfinished task immediately.
8. Complete tasks in the order defined below unless a direct dependency requires a small reordering.
9. Preserve the existing N-Layer architecture and current conventions.
10. Reuse existing MongoDB, current-user, running, error-handling, test, and DI infrastructure.
11. Validate meaningful changes with build/tests before moving on.
12. Do not introduce new infrastructure unless explicitly required by this plan.
13. If the real code differs slightly from the plan, make the smallest safe correction and continue. Do not use minor differences as a reason to scan the entire repository.

---

# 1. Stage Status

Stage 1 and Stage 2 are complete.

Existing working backend capabilities include:

- ASP.NET Core API;
- N-Layer architecture;
- MongoDB / MongoDB Atlas;
- persistent demo user;
- demo authentication;
- `ICurrentUserAccessor`;
- persistent `RunSession`;
- `POST /api/runs`;
- `GET /api/runs/active`;
- `GET /api/runs/{runId}`;
- `PATCH /api/runs/{runId}/progress`;
- `POST /api/runs/{runId}/finish`;
- real distance / duration / pace handling;
- deterministic demo overtakes;
- run-local demo rank movement;
- run-local demo season points;
- optimistic concurrency for run updates;
- persistent completed runs and overtakes;
- post-run summary;
- health/readiness endpoints;
- Docker;
- automated tests;
- real Mongo smoke validation.

Do not redesign these components.

---

# 2. Why Stage 3 Exists

The backend can now execute the core running session, but the frontend still lacks the read-side data required for the main application screens.

According to `AppContext.md`, the remaining Core MVP areas include:

- daily ranking;
- basic monthly leaderboard;
- personal progress;
- recent activity/history;
- Kraków activity map with privacy-safe aggregated data.

Stage 3 implements those read-side capabilities.

The objective is to make the deployed backend useful for the main demo screens without implementing expensive production ranking, geolocation, or social systems.

---

# 3. Stage Goal

Implement this read-side flow:

```text
MongoDB completed/active runs
            ↓
       User run history
            ↓
   Real user aggregates
     ↙        ↓        ↘
Today score  Month score  Personal progress
     ↓           ↓
Demo leaderboard opponents
     ↓           ↓
Today leaderboard / Monthly leaderboard

Demo privacy-safe Kraków activity provider
            ↓
Aggregated activity map endpoint
```

At the end of Stage 3, the frontend must be able to display:

1. recent completed runs;
2. current user's real daily points derived from run data;
3. current user's real monthly points derived from run data;
4. demo daily leaderboard;
5. demo monthly leaderboard;
6. current user's position inside those demo leaderboards;
7. personal running statistics calculated from persistent runs;
8. current-week/current-month progress;
9. simple month-to-month progress comparison;
10. aggregated Kraków running activity zones;
11. no individual runner GPS coordinates.

---

# 4. Important Stage 2 Interpretation

`RunSession.SeasonPointsEarned` is currently points earned **inside one run**.

For Stage 3:

```text
DailyPoints =
    sum of points from the current user's runs belonging to today
    + current active run points when applicable

MonthlySeasonPoints =
    sum of points from the current user's runs belonging to the current month
    + current active run points when applicable
```

Do not mutate old completed runs to store global totals.

Do not add season totals directly to the user document.

For this MVP stage, leaderboard totals are read-side aggregates computed from existing run data.

---

# 5. Temporary Period Rule

The application is Kraków-first.

Daily and monthly periods should use Kraków local time.

Add a small testable period/time component, for example:

```text
KrakowPeriodResolver
```

Responsibilities:

```text
GetTodayUtcRange(now)
GetCurrentWeekUtcRange(now)
GetCurrentMonthUtcRange(now)
GetPreviousMonthUtcRange(now)
```

Use:

```text
Europe/Warsaw
```

as the intended timezone.

If cross-platform runtime support requires a fallback timezone id, implement the smallest safe fallback.

Do not add a third-party time library.

Temporary MVP rule for assigning a run to a period:

```text
Use RunSession.StartedAtUtc
```

A run belongs to the local Kraków day/month in which it started.

---

# 6. Real vs Demo Components

## Real now

Implement from actual Mongo run data:

- completed run history;
- recent runs;
- run totals;
- total distance;
- total duration;
- weighted average pace;
- total overtakes;
- actual points earned by current user;
- current-day points;
- current-month points;
- current-week activity;
- previous-month activity;
- longest completed run;
- fastest average-pace completed run;
- active-run points included in current period totals where applicable.

## Demo now

Keep replaceable:

- other leaderboard users;
- other users' point totals;
- city-wide leaderboard population;
- Kraków activity-zone counts;
- map popularity values;
- map average pace values;
- live runner counts.

Use replaceable providers.

---

# 7. Explicit Non-Goals

Do **not** implement during Stage 3:

- JWT / real authentication;
- ASP.NET Identity;
- friends;
- rivals;
- messaging;
- 1v1 races;
- matchmaking;
- real multi-user ranking persistence;
- global ranking recalculation jobs;
- Redis;
- background workers;
- SignalR;
- WebSockets;
- notifications;
- route ownership;
- King of Route;
- achievements;
- AI Coach;
- GPS track storage;
- public individual runner coordinates;
- geospatial Mongo queries;
- external maps APIs;
- Google Maps integration;
- Mapbox integration;
- Strava/Garmin imports;
- real anti-cheat;
- production season-reset jobs;
- historical season persistence;
- a new database;
- a new Mongo client.

---

# 8. Read-Side Run Abstraction

Do not overload the write-oriented `IRunSessionStore` with many reporting queries.

Add a purpose-specific read abstraction in Application:

```text
IRunHistoryReader
        ↑
MongoRunHistoryReader
```

Recommended read model:

```text
RunHistoryItem
```

Minimum fields:

```text
Id
StartedAtUtc
FinishedAtUtc
DistanceMeters
DurationSeconds
AveragePaceSecondsPerKm
OvertakesCount
PointsEarned
```

Recommended operations:

```text
GetCompletedAsync(userId, fromUtc?, toUtc?, limit?, cancellationToken)
```

It is acceptable to aggregate in Application for MVP.

Do not build a generic reporting repository.

---

# 9. Mongo Run History Reader

Implement `MongoRunHistoryReader` using the existing `runs` collection.

Requirements:

- query only the supplied user's runs;
- completed runs only;
- newest first for recent history;
- optional UTC period filtering;
- limit server-side where practical;
- reuse existing `IMongoDatabase`;
- do not instantiate another `MongoClient`.

Add only an index that materially helps these queries.

Recommended:

```text
userId + status + startedAtUtc
```

---

# 10. Run History API

Add:

```http
GET /api/runs/history
```

Query:

```text
limit
```

Default:

```text
10
```

Allowed:

```text
1..50
```

Return completed runs newest first.

Example:

```json
{
  "items": [
    {
      "id": "run-id",
      "startedAtUtc": "2026-10-03T16:00:00Z",
      "finishedAtUtc": "2026-10-03T16:36:50Z",
      "distanceMeters": 6800,
      "durationSeconds": 2210,
      "averagePaceSecondsPerKm": 325,
      "overtakesCount": 4,
      "pointsEarned": 59
    }
  ]
}
```

Empty history returns `200` with an empty array.

---

# 11. Personal Progress

Create:

```text
ProgressService
```

Calculate from real completed run history.

Do not persist calculated progress documents.

Lifetime output:

```text
completedRuns
totalDistanceMeters
totalDurationSeconds
averagePaceSecondsPerKm
totalOvertakes
totalPointsEarned
longestRunDistanceMeters
fastestRunAveragePaceSecondsPerKm
```

Weighted average pace:

```text
totalDurationSeconds / (totalDistanceMeters / 1000)
```

when distance > 0, otherwise null.

Fastest run = lowest valid average pace.

---

# 12. Period Progress

Return real aggregates for:

```text
currentWeek
currentMonth
previousMonth
```

Each period:

```text
completedRuns
distanceMeters
durationSeconds
averagePaceSecondsPerKm
pointsEarned
```

Comparison:

```text
monthlyDistanceDeltaMeters
monthlyAveragePaceDeltaSecondsPerKm
```

Rules:

```text
distanceDelta = currentMonth.distance - previousMonth.distance

paceDelta = currentMonth.averagePace - previousMonth.averagePace
```

Negative pace delta = faster.

If either pace is unavailable:

```text
monthlyAveragePaceDeltaSecondsPerKm = null
```

---

# 13. Progress API

Add:

```http
GET /api/progress
```

All-zero progress is valid and returns `200`.

Do not return `404` merely because the user has no completed runs.

---

# 14. Current User Period Score

The leaderboard layer must compute:

```text
today points
current-month points
```

Source:

```text
completed runs in the period
+
active run points if active run started in the same period
```

Do not double count after completion.

Use:

- `IRunHistoryReader` for completed runs;
- existing `IRunSessionStore.GetActiveByUserIdAsync` for active run.

---

# 15. Demo Leaderboard Provider

Create:

```text
ILeaderboardProvider
        ↑
DemoLeaderboardProvider
```

The provider receives the current user's actual point total and produces deterministic leaderboard data.

The provider owns all fake competitor data.

Do not:

- persist fake competitors in MongoDB;
- add fake users to the `users` collection;
- hardcode fake leaderboard rows inside controllers.

Required periods:

```text
Today
Month
```

Current user must be inserted dynamically.

Sort:

```text
points descending
```

Use deterministic tie-breaking.

Mark:

```text
isCurrentUser = true
```

---

# 16. Leaderboard Response

Return:

```text
period
periodStartUtc
periodEndUtc
currentUser
top
aroundMe
```

Example:

```json
{
  "period": "today",
  "currentUser": {
    "rank": 37,
    "points": 59
  },
  "top": [],
  "aroundMe": [
    {
      "rank": 36,
      "displayName": "Marta",
      "points": 64,
      "isCurrentUser": false
    },
    {
      "rank": 37,
      "displayName": "You",
      "points": 59,
      "isCurrentUser": true
    }
  ]
}
```

Return at most:

```text
top 10
```

Recommended `aroundMe`:

```text
2 above
current user
2 below
```

---

# 17. Demo Ranking Consistency

Where practical, choose deterministic daily competitor scores so the first complete Stage 2 demo run approximately aligns with the existing run story:

```text
0 points  -> around #41
16 points -> around #40
30 points -> around #39
41 points -> around #38
59 points -> around #37
```

This is presentation consistency only.

Do not rewrite the Stage 2 competition engine solely to perfect this mapping.

---

# 18. Leaderboard APIs

Add:

```http
GET /api/leaderboards/today
```

and:

```http
GET /api/leaderboards/month
```

No client-provided `userId`.

Resolve current demo user server-side.

No activity is valid:

```text
points = 0
```

---

# 19. Privacy-Safe Kraków Activity Map

Create:

```text
IActivityMapProvider
        ↑
DemoKrakowActivityMapProvider
```

Return aggregate zones only.

Minimum demo zones:

```text
Błonia
Bulwary Wiślane
Zakrzówek
Park Jordana
Las Wolski
```

Each zone may contain:

```text
id
name
latitude
longitude
activeRunners
runs
averagePaceSecondsPerKm
activityLevel
```

Coordinates represent approximate public-area centers.

They must never represent an individual runner's position.

---

# 20. Activity Periods

Support:

```text
live
today
month
```

The demo provider may return deterministic different values for each period.

No real geolocation ingestion.

No external map API.

---

# 21. Map API

Add:

```http
GET /api/map/activity
```

Query:

```text
period=live|today|month
```

Default:

```text
today
```

Invalid period:

```text
400 Bad Request
```

Return:

```text
period
generatedAtUtc
zones
```

Do not expose user IDs or individual locations.

---

# 22. Mongo Collections

Stage 3 should normally reuse:

```text
users
runs
```

Do not create collections for:

```text
leaderboards
seasons
map activity
demo competitors
progress
statistics
```

These remain derived or provider-based in this stage.

---

# 23. Controllers

Recommended:

```text
RunsController
    + GET /api/runs/history

LeaderboardsController
    GET /api/leaderboards/today
    GET /api/leaderboards/month

ProgressController
    GET /api/progress

MapController
    GET /api/map/activity
```

Controllers must not:

- calculate totals;
- query Mongo directly;
- sort leaderboards;
- contain demo competitors;
- contain demo map zones.

---

# 24. Dependency Injection

Expected registrations:

```text
IRunHistoryReader
    -> MongoRunHistoryReader

ILeaderboardProvider
    -> DemoLeaderboardProvider

IActivityMapProvider
    -> DemoKrakowActivityMapProvider

KrakowPeriodResolver
ProgressService
LeaderboardService
```

Reuse:

```text
ICurrentUserAccessor
IRunSessionStore
IMongoDatabase
TimeProvider
```

---

# 25. Error Behavior

Use existing Problem Details.

| Situation | HTTP |
|---|---:|
| invalid history limit | 400 |
| invalid map period | 400 |
| no run history | 200 |
| no leaderboard points | 200 |
| no previous-month data | 200 |
| unexpected persistence failure | sanitized 500 |

---

# 26. Tests — Periods

Test:

- local Kraków day -> UTC range;
- current week;
- current month;
- previous month;
- representative winter/summer timestamps where practical.

Use deterministic time.

---

# 27. Tests — Progress

Test:

- no completed runs;
- one run;
- multiple runs;
- weighted average pace;
- overtakes total;
- points total;
- longest run;
- fastest run;
- current week;
- current month;
- previous month;
- distance delta;
- pace delta;
- missing previous pace -> null delta.

---

# 28. Tests — Leaderboards

Test:

- 0-point user insertion;
- current score from real run totals;
- active-run points included once;
- completed run not double-counted;
- descending ordering;
- deterministic tie handling;
- current rank;
- top max 10;
- aroundMe includes user;
- today/month demo populations can differ.

---

# 29. Tests — History

Test:

- completed runs only;
- newest first;
- limit respected;
- other users excluded;
- empty list;
- period filtering where appropriate.

---

# 30. Tests — Map

Test:

- live;
- today;
- month;
- invalid period -> 400;
- aggregate zones exist;
- no user id;
- no individual-runner coordinate model.

---

# 31. API / Integration Tests

Add focused tests for:

```text
GET /api/runs/history
GET /api/leaderboards/today
GET /api/leaderboards/month
GET /api/progress
GET /api/map/activity
```

Verify that after a persisted completed run:

- history contains it;
- progress reflects it;
- leaderboards reflect its points.

All existing Stage 1/2 tests must continue passing.

Automated tests must not require Atlas credentials.

---

# 32. Manual Demo Verification

Use configured real MongoDB.

```text
1. Login as demo user.

2. GET /api/leaderboards/today
   Record baseline.

3. GET /api/progress
   Record baseline.

4. POST /api/runs.

5. PATCH run to 2800 m / 840 s.
   Existing Stage 2 should award 30 run points.

6. GET /api/leaderboards/today.
   Active points should appear once.

7. Finish at 6800 m / 2210 s.
   Existing Stage 2 should finish with 59 run points.

8. GET /api/leaderboards/today.
   Completed score must be present and not double counted.

9. GET /api/leaderboards/month.

10. GET /api/runs/history.
    Completed run must appear.

11. GET /api/progress.
    Totals must reflect persisted run.

12. Restart API/container.

13. Repeat leaderboard/history/progress.
    Mongo-derived values must survive restart.

14. GET /api/map/activity?period=live
15. GET /api/map/activity?period=today
16. GET /api/map/activity?period=month
```

If the database already has completed demo runs, compare deltas rather than assuming zero state.

---

# 33. Implementation Order

## Task 1 — Period Boundaries
- [x] Add `KrakowPeriodResolver`.
- [x] Add today/week/current-month/previous-month ranges.
- [x] Add deterministic tests.

## Task 2 — Run History Reader
- [x] Add `RunHistoryItem`.
- [x] Add `IRunHistoryReader`.
- [x] Add `MongoRunHistoryReader`.
- [x] Add useful Mongo index if needed.
- [x] Add tests.

## Task 3 — Run History API
- [x] Add history contract.
- [x] Add `GET /api/runs/history`.
- [x] Add `limit` validation.
- [x] Add API tests.

## Task 4 — Personal Progress
- [x] Add progress models.
- [x] Add `ProgressService`.
- [x] Implement lifetime aggregates.
- [x] Implement period aggregates.
- [x] Implement comparison.
- [x] Add unit tests.
- [x] Add `GET /api/progress`.
- [x] Add API tests.

## Task 5 — Demo Leaderboard
- [x] Add leaderboard models.
- [x] Add `ILeaderboardProvider`.
- [x] Add `DemoLeaderboardProvider`.
- [x] Add deterministic today/month competitors.
- [x] Insert current user dynamically.
- [x] Implement top 10.
- [x] Implement aroundMe.
- [x] Add unit tests.

## Task 6 — Leaderboard Service
- [x] Add `LeaderboardService`.
- [x] Aggregate completed current-user points.
- [x] Include active-run points when applicable.
- [x] Prevent double counting.
- [x] Use `KrakowPeriodResolver`.
- [x] Add tests.

## Task 7 — Leaderboard API
- [x] Add today endpoint.
- [x] Add month endpoint.
- [x] Add contracts.
- [x] Add integration tests.

## Task 8 — Activity Map Provider
- [x] Add activity models.
- [x] Add `IActivityMapProvider`.
- [x] Add `DemoKrakowActivityMapProvider`.
- [x] Add aggregate zones.
- [x] Add live/today/month periods.
- [x] Add unit tests.

## Task 9 — Map API
- [x] Add `GET /api/map/activity`.
- [x] Default to `today`.
- [x] Validate period.
- [x] Add API tests.

## Task 10 — Regression Validation
- [x] Build solution.
- [x] Run full test suite.
- [x] Verify Stage 1 auth/health.
- [x] Verify Stage 2 run flow.
- [x] Verify Stage 3 endpoints.
- [x] Verify OpenAPI.
- [x] Verify Docker build.

## Task 11 — Real Mongo Demo Flow
- [x] Execute Section 32.
- [x] Verify history persistence.
- [x] Verify progress persistence.
- [x] Verify leaderboard derives from runs.
- [x] Verify no active/completed double count.
- [x] Verify restart behavior.

## Task 12 — Minimal Documentation
- [x] Update README with Stage 3 endpoints.
- [x] Add minimal curl examples.
- [x] Document real-vs-demo boundaries.
- [x] Do not rewrite unrelated documentation.

---

# 34. Acceptance Criteria

Stage 3 is complete only when:

- [x] Stage 1 still works.
- [x] Stage 2 still works.
- [x] Existing tests still pass.
- [x] History endpoint returns persistent completed runs.
- [x] History is current-user only.
- [x] History is newest first.
- [x] History limit is validated.
- [x] Progress is derived from actual run data.
- [x] Weighted average pace is correct.
- [x] Current-week aggregation is correct.
- [x] Current-month aggregation is correct.
- [x] Previous-month aggregation is correct.
- [x] Comparison safely handles missing data.
- [x] Daily points derive from run data.
- [x] Monthly points derive from run data.
- [x] Active run points can be included without double counting.
- [x] Demo competitors are isolated behind an interface.
- [x] Today leaderboard returns top + user position.
- [x] Monthly leaderboard returns top + user position.
- [x] Empty activity returns valid zero-state responses.
- [x] Map uses replaceable provider.
- [x] Map exposes aggregate Kraków zones only.
- [x] Map exposes no precise individual-runner location.
- [x] live/today/month map periods work.
- [x] No unnecessary Mongo collections were introduced.
- [x] Full automated test suite passes.
- [x] Docker build succeeds.
- [x] Real Mongo manual flow succeeds.

---

# 35. Architectural Guardrails

Keep these responsibilities separate:

```text
RunSession
    = one-run lifecycle/state

IRunHistoryReader
    = read persisted completed activity

ProgressService
    = calculate user's real progress

LeaderboardService
    = calculate user's period score

ILeaderboardProvider
    = replaceable demo city competition population

IActivityMapProvider
    = replaceable aggregated city-map data
```

Do not merge everything into a large dashboard service.

Do not place Mongo-specific code in Application.

Do not place demo city data in Domain.

Do not calculate statistics in controllers.

---

# 36. Stage 4 — Do Not Implement Yet

After Stage 3, choose the next stage based on frontend/demo needs:

```text
A. Profile + richer personal progression
B. Rivals / next-goal system
C. Route catalog + route competition demo
D. deployment hardening / production demo environment
```

Do not start Stage 4 during this plan.
