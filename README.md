# CitySurfers — Stage 5 backend

.NET 10 modular monolith: API → Application/Infrastructure; Application → Domain.
MongoDB-specific code stays in Infrastructure. Domain owns run state and lifecycle rules.

## Build and test

Install the .NET 10 SDK, then run from the repository root:

```sh
dotnet restore CitySurfers.sln
dotnet build CitySurfers.sln --no-restore
dotnet test CitySurfers.sln --no-build --no-restore
```

Unit and API integration tests use replacement stores, initialization and health checks; they never connect to Atlas.

## Configuration and local startup

Set `MongoDb__ConnectionString` in your local environment to an Atlas URI using a dedicated database user.
Atlas must allow the machine or hosting provider's network access; the database user needs access to the configured database, including index creation.

| Variable | Default / purpose |
| --- | --- |
| `MongoDb__ConnectionString` | Required; secret supplied outside source control |
| `MongoDb__DatabaseName` | `citysurfers` |
| `DemoData__SeedOnStartup` | `true`; set to `false` to disable inserting demo accounts |
| `DemoData__ResetRunsOnStartup` | `false`; opt-in deletion of demo-user runs on API startup; requires seeding |
| `Cors__AllowedOrigins__0` | Deployed frontend origin, e.g. `https://frontend.example` |
| `ASPNETCORE_ENVIRONMENT` | Use `Production` for deployment |
| `ASPNETCORE_HTTP_PORTS` | `8080` in the Docker image |

Development allows `http://localhost:5173`. Production allows only explicitly configured origins.
Origins must include HTTP(S) scheme and host/port, without paths or trailing slashes. CORS credentials are disabled.

```sh
dotnet run --project src/CitySurfers.Api
```

The development launch profile listens at `http://localhost:5092`.
OpenAPI is at `/openapi/v1.json` in Development only. Sample requests are in
`src/CitySurfers.Api/CitySurfers.Api.http`.

Startup validates Mongo configuration, ensures a unique username index, and optionally inserts `demo / 1234`.
The upsert preserves existing account data and creation timestamps. Index initialization still runs when seeding is disabled.
Missing/invalid Mongo configuration or failed initialization stops startup with a sanitized error.

## API

| Endpoint | Behavior |
| --- | --- |
| `GET /health` | `200` process liveness; no database query |
| `GET /health/ready` | `200` when Mongo responds; `503` when unavailable |
| `POST /api/auth/login` | Persisted user data on `200`; generic `401` for invalid credentials |
| `GET /api/home` | Compact bootstrap: today rank/points, nullable active-run metrics, existing primary next goal; idle is `200` |
| `POST /api/runs` | `201` active run; `409` if the demo user already has one |
| `GET /api/runs/active` | Active run and competition snapshot; `404` if none exists |
| `GET /api/runs/{runId}` | Active run response or completed post-run summary; `404` for missing/foreign runs |
| `PATCH /api/runs/{runId}/progress` | Updated metrics, competition snapshot, and newly created `OVERTAKE` events |
| `POST /api/runs/{runId}/finish` | Final progress and persistent post-run summary; `409` if already completed |
| `GET /api/runs/history?limit=10` | Current user's completed runs, newest first; limit 1–50, default 10 |
| `GET /api/progress` | Real lifetime, current-week/month, previous-month aggregates and monthly comparison |
| `GET /api/leaderboards/today` | Real current-user daily points, demo top 10 and nearby ranks |
| `GET /api/leaderboards/month` | Real current-user monthly points, demo top 10 and nearby ranks |
| `GET /api/rivals/current` | Monthly current-user rank/points and optional rival directly above |
| `GET /api/goals/next` | Active-run overtake goal, monthly rival fallback, or null |
| `GET /api/map/activity?period=today` | Aggregate demo Kraków zones; live/today/month, default today |

Login request: `{"username":"demo","password":"1234"}`.
Success contains only `id`, `username`, and `displayName`.
Malformed/missing inputs return `400` validation Problem Details.
Unexpected request failures return sanitized `500` Problem Details, including when the client requests a non-JSON content type.
Logs include exception type and request trace ID; driver exception text and credentials are not logged.

### Demo run flow

Run endpoints resolve the persisted `demo` user server-side through `ICurrentUserAccessor`.
Login issues no session; requests do not accept a user id. Both demo identity and competition are replaceable through DI.
Targets are Runner_92 at 1200 m (+16), Marta at 2500 m (+14), Runner_17 at 4000 m (+11), and Kamil_24 at 5500 m (+18).
Each target awards once and improves the starting rank of 41 by one. One request can cross multiple targets.

```sh
curl -X POST http://localhost:5092/api/auth/login \
  -H 'Content-Type: application/json' -d '{"username":"demo","password":"1234"}'
curl -X POST http://localhost:5092/api/runs
# Replace RUN_ID with the returned id.
curl -X PATCH http://localhost:5092/api/runs/RUN_ID/progress \
  -H 'Content-Type: application/json' -d '{"distanceMeters":2800,"durationSeconds":840}'
curl http://localhost:5092/api/runs/active
curl -X POST http://localhost:5092/api/runs/RUN_ID/finish \
  -H 'Content-Type: application/json' -d '{"distanceMeters":6800,"durationSeconds":2210}'
curl http://localhost:5092/api/runs/RUN_ID
```

Progress and finish require both non-negative, non-decreasing distance and duration; invalid values return `400` Problem Details.
Completed runs cannot change. Concurrent conflicting writes return `409`; reload the run before retrying.
Pace is seconds/km, or `null` at zero distance. UTC timestamps use millisecond precision for consistent Mongo round trips.
The `runs` collection stores all metrics and overtakes. A partial unique `userId + status` index prevents multiple active runs while allowing completed history.

### Stage 3 read side

```sh
curl 'http://localhost:5092/api/runs/history?limit=10'
curl http://localhost:5092/api/progress
curl http://localhost:5092/api/leaderboards/today
curl http://localhost:5092/api/leaderboards/month
curl 'http://localhost:5092/api/map/activity?period=live'
```

`IRunHistoryReader` reads completed runs from MongoDB. `ProgressService` calculates completed-run statistics,
including weighted pace: total duration / total distance in kilometres. A missing pace is `null`.
Monthly comparison subtracts previous month from current month; a negative pace delta means faster.
Periods use Europe/Warsaw local calendar boundaries (weeks start Monday), converted to UTC with daylight-saving support.
Run membership uses `startedAtUtc`; period starts are inclusive and ends exclusive.

`LeaderboardService` sums persisted completed-run points and includes an active run when it started in that period.
A run transitioning to completed is counted once. `ILeaderboardProvider` supplies deterministic demo competitors;
their ranks and totals are presentation data, and no fake users or leaderboard documents are persisted.
`IActivityMapProvider` supplies deterministic aggregate zones at approximate public-area centers.
Map coordinates never represent individual runners; there is no GPS ingestion or external map service.
Only the existing `users` and `runs` collections are used. History/progress and current-user scores survive API restarts.
Empty activity returns `200` with empty history or zero statistics/scores. Invalid history limits and map periods return `400` Problem Details.

### Stage 4 motivation

```sh
curl http://localhost:5092/api/rivals/current
curl http://localhost:5092/api/goals/next
```

`RivalService` reuses the current monthly leaderboard. The temporary MVP rival is the competitor
immediately above the current user; rank one returns `rival: null`. The response includes monthly
UTC boundaries, current-user rank/points and optional rival rank/points. `pointsGap` is
max(0, rival points - user points); `pointsToPass` is max(1, rival points - user points + 1).
Current-user points derive from persisted runs, including active-run points under existing rules;
other competitors remain deterministic demo data. Rivals are derived, never persisted.

`NextGoalService` first returns `run_overtake` from the existing active-run competition snapshot,
including remaining distance, potential points and rank transition. Without an active target,
it returns `rival_points` from the monthly rival's `pointsToPass`. Without either source,
it returns `200` with `goal: null`. Models contain semantic data, not localized UI sentences.
No new MongoDB collections, rival relationships or fake competitor documents are introduced.

### Stage 5 frontend bootstrap and repeatable demo

After demo login, `GET /api/home` supplies `today` (rank and points), nullable `activeRun`
(id, startedAtUtc, distanceMeters, durationSeconds, averagePaceSecondsPerKm), and nullable `nextGoal`.
It composes the existing leaderboard and goal services; it does not include full history, leaderboards,
analytics or map zones. Pace is the stored seconds/km value, nullable at zero distance.
Idle and null-goal states return `200` with explicit JSON nulls.

Recommended frontend sequence:

```text
POST /api/auth/login
GET  /api/home

# Dedicated screens / lazy loading
GET /api/map/activity
GET /api/leaderboards/today
GET /api/leaderboards/month
GET /api/progress
GET /api/runs/history

# During and after a run
POST  /api/runs
PATCH /api/runs/{runId}/progress
POST  /api/runs/{runId}/finish
GET   /api/home
```

Today and Month use the same replaceable demo competitor ladder: 8, 24, 36, 50, 64 points
around the fresh user. Real current-user points still aggregate persisted runs using each period's boundaries.
A clean demo progresses from 0 points/#41 through 16/#40, 30/#39, 41/#38 to 59/#37.
After all four overtakes and finish, the monthly rival is #36 with 64 points: gap 5, points to pass 6.
Home prefers `run_overtake` while a target exists and falls back to `rival_points` afterward.
These competitor scores are demo presentation data, not permanent scoring rules.

**Reset warning:** `DemoData__ResetRunsOnStartup=true` deletes all active and completed runs belonging
only to the persisted demo user on every API startup. It preserves that user's document, other users,
other users' runs, collections and indexes. Leave it `false` (the default) to preserve runs across restarts.
Reset requires `DemoData__SeedOnStartup=true`; the invalid combination fails startup validation.
There is no HTTP reset endpoint. Enable this switch intentionally through operator configuration for a fresh demo.

For browser integration, set `Cors__AllowedOrigins__0=https://frontend.example` to your frontend origin
(and numbered entries for additional origins). Production permits only configured origins; credentials are disabled.
Allowed/disallowed origins and PATCH content-type preflight are covered by integration tests and the Mongo smoke.

## Docker deployment

```sh
docker build -t citysurfers-api .
```

Copy `.env.example` to a local `.env`, replace its placeholders, and keep that file outside source control.
Docker reads this file; `dotnet run` does not load `.env` automatically.

```sh
docker run --rm --name citysurfers-api --env-file .env -p 8080:8080 citysurfers-api
```

The container uses the runtime's non-root application user and contains no database or deployment secrets.
Configure HTTPS and the public origin at your hosting provider.
Check `/health`, `/health/ready`, and correct/incorrect login after deployment.

Demo authentication is presentation-only: it issues no token/session and stores only fictional demo credentials.
Before production, replace `DemoAuthService` and demo credential storage with real authentication.

## Validation status

See `PLAN.md` for Stage 5 completion and acceptance criteria.
Local MongoDB smoke checks verify persistence, restart idempotence, unique username enforcement, seeding configuration, and database outage behavior.
These checks do not establish connectivity to your Atlas cluster.

Stage 2 validation: solution build (zero warnings), all 75 automated tests, Docker image build, and the real Mongo run flow passed.
Active and completed runs survived API container restarts; concurrent starts, progress, and finishes did not duplicate rewards.
To repeat the real Mongo check after building the image, with the configured local `citysurfers-mongo` container running:

```powershell
pwsh -NoProfile -File tests/Stage2.MongoSmoke.ps1 -ApiImage citysurfers-api
```

The check requires no existing active demo run, leaves one completed demo run in `citysurfers`, and removes its temporary API container.

Stage 3 validation: solution build with zero warnings/errors, all 126 automated tests, OpenAPI, Docker build,
the existing Stage 2 real Mongo concurrency/restart checks, and the Stage 3 real Mongo flow passed.
The Stage 3 check compares score/statistic deltas against persisted Mongo runs and verifies history/progress/leaderboards after restart.

```powershell
docker build -t citysurfers-api:stage3 .
pwsh -NoProfile -File tests/Stage3.MongoSmoke.ps1
```

The Stage 3 check uses the configured local `citysurfers-mongo` container, requires no active demo run,
leaves one completed demo run, and removes its temporary API container. It does not verify Atlas connectivity.

Stage 4 validation: solution build with zero warnings/errors, all 145 automated tests, OpenAPI response
schemas, and Stage 2–4 real Mongo smoke flows passed. The Docker image built using cached base-image
digests after registry tag lookup returned EOF; see `PLAN.md` for the validation detail.

To repeat the Stage 4 real Mongo flow:

```powershell
docker build -t citysurfers-api:stage4 .
pwsh -NoProfile -File tests/Stage4.MongoSmoke.ps1
```

The check reuses the local `citysurfers-mongo` container, requires no active demo run,
leaves one completed run and removes its temporary API container. It verifies idle and active goals,
distance/target changes, monthly fallback, Mongo score deltas, unchanged collections and restart persistence.
Accumulated monthly points may yield rank one; in that case null rival/goal is expected.

Stage 5 validation: build with zero warnings/errors, all 161 automated tests, Home and existing OpenAPI
contracts, CORS integration tests, Docker image build, and the real Mongo Stage 5 smoke passed.
Registry tag lookup returned EOF; Docker validation used an ignored copy of the existing Dockerfile
with only the .NET 10 base-image references pinned to cached digests. The repository Dockerfile is unchanged.

```powershell
docker build -t citysurfers-api:stage5 .
pwsh -NoProfile -File tests/Stage5.MongoSmoke.ps1
```

The Stage 5 check reuses local `citysurfers-mongo` and creates `citysurfers_stage5_smoke`.
It refuses an existing database; use `-Database citysurfers_stage5_smoke_unique` if needed.
It verifies clean Home, all four rank transitions in both leaderboards, 59-point finish, the 6-point rival,
CORS/preflight, OpenAPI, history/progress, persistence with reset disabled, and reset with it enabled.
An existing demo account with a non-default id, unrelated users/runs, and indexes verify reset isolation.
The check removes its temporary API container and only the database it created, including on failure.
It does not verify Atlas connectivity or deployed frontend behavior.

References: [MongoDB C# driver](https://www.mongodb.com/docs/drivers/csharp/current/),
[ASP.NET Core](https://learn.microsoft.com/aspnet/core/).
