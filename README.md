# CitySurfers — Stage 1 backend

.NET 10 modular monolith: API → Application/Infrastructure; Application → Domain.
MongoDB-specific code stays in Infrastructure. Domain is intentionally empty until a domain use case needs it.

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

Login request: `{"username":"demo","password":"1234"}`.
Success contains only `id`, `username`, and `displayName`.
Malformed/missing inputs return `400` validation Problem Details.
Unexpected request failures return sanitized `500` Problem Details, including when the client requests a non-JSON content type.
Logs include exception type and request trace ID; driver exception text and credentials are not logged.

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

See `PLAN.md` for completed tasks and outstanding Atlas verification.
Local MongoDB smoke checks verify persistence, restart idempotence, unique username enforcement, seeding configuration, and database outage behavior.
These checks do not establish connectivity to your Atlas cluster.

References: [MongoDB C# driver](https://www.mongodb.com/docs/drivers/csharp/current/),
[ASP.NET Core](https://learn.microsoft.com/aspnet/core/).
