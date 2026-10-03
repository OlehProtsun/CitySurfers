# Stage 6 demo and deployment

## Local fallback

From the repository root, with Docker running:

```sh
docker compose -f docker-compose.demo.yml up --build
```

API: `http://localhost:8080`; liveness: `/health`; readiness: `/health/ready`.
Mongo is private to the demo network, with a demo-only volume and database `citysurfers_demo`.
API startup waits for healthy Mongo, initializes indexes and seeds fictional `demo / 1234`.
The API runs in Production and OpenAPI returns 404. Check readiness before frontend entry.
Only loopback port 8080 is published; Mongo has no host port. This local demo does not use cloud secrets.
Initial image downloads require network access; with images cached this is a local fallback.

Every API startup deletes the demo user's active/completed run history for a clean deterministic showcase.
The user's account, unrelated users/runs and indexes are preserved. This is operator configuration,
not a public reset endpoint. Override `DemoData__ResetRunsOnStartup=false` for persistence.
The configured browser origin is `http://localhost:5173`; adjust the allowlist for your actual frontend.

Stop and remove only this demo's data volume:

```sh
docker compose -f docker-compose.demo.yml down -v
```

## Provider-neutral deployment

Build the ordinary single-service image with `docker build -t citysurfers-api .`.
Deploy to a platform supporting Docker, environment variables, outbound Mongo connectivity and public HTTPS routing.
No provider or frontend framework is selected here. Required environment:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
MongoDb__ConnectionString=<secret>
MongoDb__DatabaseName=<database>
DemoData__SeedOnStartup=true
DemoData__ResetRunsOnStartup=<true for deterministic restart OR false for persistence>
Cors__AllowedOrigins__0=<frontend-origin>
```

Supply secrets outside source control/logs. Database name is configurable.
Mongo must be reachable from the hosting environment; Atlas-like network allowlists must permit its egress,
and the database user needs read/write/index permissions. Smoke scripts never need remote DB credentials.
Missing configuration or unsuccessful database initialization stops startup with sanitized diagnostics.
Readiness should probe `/health/ready` (200 healthy, 503 database unavailable); `/health` is process liveness only.
TLS/HTTPS termination may occur outside the ASP.NET container, at the platform/reverse proxy.
Expose the API container's port 8080 through that routing layer. It runs as a non-root user.

CORS must list the actual browser frontend origin (scheme, host and port, no path/trailing slash).
Add numbered entries for additional origins. No wildcard Production CORS; credentials are disabled.
Native mobile clients are not governed by browser CORS in the same way.
Demo login provides no session/token or access isolation; this is a shared fictional hackathon demo,
not an authentication system for real users.

The .NET 10 base images are pinned to the digests already validated in Stage 5 for repeatable builds.
Update these pins deliberately for runtime/security servicing and re-run release checks.

## Release and remote validation

```powershell
pwsh -NoProfile -File tests/Stage6.ReleaseSmoke.ps1
pwsh -NoProfile -File tests/Stage6.RemoteSmoke.ps1 -BaseUrl https://your-api-host
pwsh -NoProfile -File tests/Stage6.RemoteSmoke.ps1 -BaseUrl https://your-api-host -FrontendOrigin https://your-frontend-host
```

Release smoke builds the actual Dockerfile, creates isolated temporary Mongo/API containers and network,
tests Production health/readiness, no OpenAPI, CORS allow/deny/preflight, the entire four-overtake flow,
59 points/rank 37, six-point rival, history/progress and unchanged `users,runs` collections.
Its finally block removes containers, anonymous Mongo volumes and network even on failure.
Use `-Port` if 18086 is occupied; `-ApiImage` changes the output image tag.

Remote smoke defaults to health/readiness, demo login and read contracts only. It never starts/finishes runs
or resets data without **explicit `-FullDemo`**. `-FrontendOrigin` enables allow-origin/preflight checks.
It handles HTTP(S) supplied by the caller, trailing slashes, timeouts, expected statuses and non-zero failures.
It prints concise results without server bodies, credentials or sensitive headers.

For a fresh dedicated demo environment only:

```powershell
pwsh -NoProfile -File tests/Stage6.RemoteSmoke.ps1 -BaseUrl http://localhost:8080 -FullDemo -FrontendOrigin http://localhost:5173
```

FullDemo warns before mutating and requires clean rank 41/zero points/no active run/empty history.
It leaves one completed 59-point run; it does not delete or reset remote data.
Run the default check against a real deployed URL after provider credentials/routing become available.
Local checks validate script behavior, not remote hosting connectivity.

## CI and contract

`.github/workflows/backend-ci.yml` runs on push and pull requests, including main, with .NET 10
restore/build Release/test Release. Replacement stores/health checks require no Mongo/cloud secrets.
No deployment or registry publication is configured. Docker release validation remains the separate
release smoke gate because registry availability should not make the mandatory build/test gate flaky.

Development only: supply Mongo configuration, run `dotnet run --project src/CitySurfers.Api`, then fetch
`http://localhost:5092/openapi/v1.json`. Production OpenAPI stays disabled.
Frontend contracts and the expected call sequence are in [MVP_FRONTEND_HANDOFF.md](MVP_FRONTEND_HANDOFF.md).
Backend Core MVP features/contracts are frozen for the hackathon; next work is frontend integration.
