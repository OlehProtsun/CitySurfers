# CitySurfers — Stage 2 backend

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
| `POST /api/runs` | `201` active run; `409` if the demo user already has one |
| `GET /api/runs/active` | Active run and competition snapshot; `404` if none exists |
| `GET /api/runs/{runId}` | Active run response or completed post-run summary; `404` for missing/foreign runs |
| `PATCH /api/runs/{runId}/progress` | Updated metrics, competition snapshot, and newly created `OVERTAKE` events |
| `POST /api/runs/{runId}/finish` | Final progress and persistent post-run summary; `409` if already completed |

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

See `PLAN.md` for Stage 2 completion and acceptance criteria.
Local MongoDB smoke checks verify persistence, restart idempotence, unique username enforcement, seeding configuration, and database outage behavior.
These checks do not establish connectivity to your Atlas cluster.

Stage 2 validation: solution build (zero warnings), all 75 automated tests, Docker image build, and the real Mongo run flow passed.
Active and completed runs survived API container restarts; concurrent starts, progress, and finishes did not duplicate rewards.
To repeat the real Mongo check after building the image, with the configured local `citysurfers-mongo` container running:

```powershell
pwsh -NoProfile -File tests/Stage2.MongoSmoke.ps1 -ApiImage citysurfers-api
```

The check requires no existing active demo run, leaves one completed demo run in `citysurfers`, and removes its temporary API container.

References: [MongoDB C# driver](https://www.mongodb.com/docs/drivers/csharp/current/),
[ASP.NET Core](https://learn.microsoft.com/aspnet/core/).
