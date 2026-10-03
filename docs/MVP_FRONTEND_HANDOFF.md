# MVP frontend handoff — Stage 6

Configure one external `API_BASE_URL`: `http://localhost:5092` for Development,
`http://localhost:8080` for the local Docker demo, or `https://<deployed-api-host>`.
Append the paths below; the framework-specific configuration mechanism belongs to the frontend.
Send JSON with `Content-Type: application/json`. Responses use camelCase; timestamps are UTC,
distance is metres, duration is seconds, and pace is seconds/km (null at zero distance).

## Screen contracts

| Screen / responsibility | Request | Successful response |
| --- | --- | --- |
| Demo entry | `POST /api/auth/login` | 200: `id`, `username`, `displayName` |
| Home / resume | `GET /api/home` | 200: `today {rank, points}`, nullable `activeRun`, nullable `nextGoal` |
| Start | `POST /api/runs` (no body) | 201: active run; Location points to run |
| Resume active run | `GET /api/runs/active` | 200: active run; 404 when idle |
| Run detail | `GET /api/runs/{runId}` | 200: active run or completed summary |
| Live progress | `PATCH /api/runs/{runId}/progress` | 200: active run with competition and new events |
| Finish / summary | `POST /api/runs/{runId}/finish` | 200: completed summary |
| History | `GET /api/runs/history?limit=10` | 200: `items`; limit 1–50, default 10 |
| Personal progress | `GET /api/progress` | 200: `lifetime`, `currentWeek`, `currentMonth`, `previousMonth`, `comparison` |
| Rankings | `GET /api/leaderboards/today`, `GET /api/leaderboards/month` | 200: `period`, `periodStartUtc`, `periodEndUtc`, `currentUser`, `top`, `aroundMe` |
| Monthly rival | `GET /api/rivals/current` | 200: period boundaries, `currentUser`, nullable `rival` |
| Dedicated goal | `GET /api/goals/next` | 200: nullable `goal` |
| Activity map | `GET /api/map/activity?period=today` | 200: `period`, `generatedAtUtc`, `zones`; also accepts `live`, `month` |
| Operator health | `GET /health`, `GET /health/ready` | 200 when healthy; readiness 503 when Mongo unavailable |

Login body: `{"username":"demo","password":"1234"}` (fictional demo credentials).
Login validates credentials but issues **no JWT, token or session**. Subsequent requests resolve
the demo user server-side, even without login. Do not send a user id or invent authorization headers.
This shared hackathon demo is not real multi-user production authentication.

## Bootstrap and refresh

After login and on app entry/resume, call Home once rather than reconstructing it from multiple endpoints.
`activeRun: null` means idle; `nextGoal: null` means no target. Home's active run contains
`id`, `startedAtUtc`, `distanceMeters`, `durationSeconds`, `averagePaceSecondsPerKm`.
Lazy-load dedicated screens. During a run render each progress response; aggressive polling is unnecessary.
After finish render the returned summary, then refresh Home for the monthly rival fallback.

## Run lifecycle and JSON fields

Start returns `id`, `status` (`active`), `startedAtUtc`, `updatedAtUtc`, `distanceMeters`,
`durationSeconds`, `averagePaceSecondsPerKm`, `competition`, `events`.
Competition contains `rank`, `seasonPointsEarned`, nullable `currentTarget` with
`opponent`, `distanceToOvertakeMeters`, `potentialPoints`.

Progress **and finish require both cumulative totals**:

```json
{"distanceMeters":1300,"durationSeconds":390}
```

Values must be finite, non-negative and non-decreasing relative to stored progress.
Serialize writes; recover authoritative state after a conflict or ambiguous network failure.
The backend creates overtakes, awards points, updates rank and selects targets.
`events` contains only newly created events for that mutation: `type` (`OVERTAKE`), `opponent`,
`rankBefore`, `rankAfter`, `pointsAwarded`. Do not award points locally or rely on replayed events.
One request can cross multiple targets; rewards occur once per target.
A frontend demo simulator or future sensor source submits these same cumulative totals;
there is no GPS-track ingestion, backend timer, or public demo-advance/reset API.

Finish returns `runId` (rather than active response `id`), `startedAtUtc`, `finishedAtUtc`,
`distanceMeters`, `durationSeconds`, `averagePaceSecondsPerKm`, `overtakesCount`, `overtakes`,
`rankBefore`, `rankAfter`, `seasonPointsEarned`, nullable `nextTarget`.
Each stored overtake contains `opponentKey`, `opponent`, `completedAtUtc`,
`distanceThresholdMeters`, `pointsAwarded`, `rankBefore`, `rankAfter`.
Completed detail returns this same summary. Finishing again returns 409; fetch detail to recover.

Goals contain `type`, `source`, `targetDisplayName`, nullable `runId`, `remainingDistanceMeters`,
`remainingPoints`, `potentialPoints`, `currentRank`, `targetRank`.
`run_overtake` takes priority while an active target exists; otherwise `rival_points` uses monthly standings.
These are semantic fields; localize UI text on the client.

Clean demo sequence (Compose resets on startup):

| Progress distance / seconds | Points | Rank |
| --- | --- | --- |
| Start 0 / 0 | 0 | 41 |
| 1200 / 360 | 16 | 40 |
| 2500 / 750 | 30 | 39 |
| 4000 / 1200 | 41 | 38 |
| 5500 / 1650 | 59 | 37 |
| Finish 6800 / 2210 | 59 | 37 |

After finish Home has no active run; rival is rank 36, 64 points, gap 5, **6 points to pass**.
Rival fields are `displayName`, `rank`, `points`, `pointsGap`, `pointsToPass`.
Leaderboard rows contain `rank`, `displayName`, `points`, `isCurrentUser`.
Competitors are deterministic demo presentation data; current-user points come from persisted runs.

History items use `id`, `startedAtUtc`, `finishedAtUtc`, `distanceMeters`, `durationSeconds`,
`averagePaceSecondsPerKm`, `overtakesCount`, `pointsEarned`.
Lifetime progress contains `completedRuns`, `totalDistanceMeters`, `totalDurationSeconds`,
`averagePaceSecondsPerKm`, `totalOvertakes`, `totalPointsEarned`, `longestRunDistanceMeters`,
`fastestRunAveragePaceSecondsPerKm`. Period aggregates contain `completedRuns`, `distanceMeters`,
`durationSeconds`, `averagePaceSecondsPerKm`, `pointsEarned`. Comparison contains
`monthlyDistanceDeltaMeters`, `monthlyAveragePaceDeltaSecondsPerKm` (negative pace delta means faster).
History/progress use completed runs; leaderboards also count eligible active-run points once.
Calendar periods use Europe/Warsaw, Monday week starts, inclusive start/exclusive end,
and run `startedAtUtc` determines period membership. Empty history/stats are successful empty/zero values.

## Errors and recovery

Errors use Problem Details (`status`, `title`, optional `detail`/`traceId`; validation may include `errors`).
Treat status as authoritative; avoid depending on exact English error text.

| Status | Client action |
| --- | --- |
| 400 | Show validation error; fix payload instead of retrying identical input |
| 401 | Login failed: show generic invalid credentials |
| 404 | Missing run: refresh Home; active discovery is 404 while idle, Home uses null |
| 409 | Existing active run, completed mutation or concurrent conflict: refresh Home/run before retry |
| 500 / 503 | Temporary service failure: offer retry; keep authoritative game state on server |

For uncertain start/finish outcomes, fetch Home/detail before sending another mutation.

## Map privacy

Zone fields: `id`, `name`, `latitude`, `longitude`, `activeRunners`, `runs`,
`averagePaceSecondsPerKm`, `activityLevel`.
Coordinates are approximate public-area centers for aggregate demo Kraków activity zones.
Even `period=live` is simulated aggregate activity, **not individual live runner GPS positions**.
There are no exact individual locations or persisted GPS tracks.

## OpenAPI and freeze

With Mongo configuration supplied, run `dotnet run --project src/CitySurfers.Api` and fetch
`http://localhost:5092/openapi/v1.json`. Development exposes the real schema; Production intentionally does not.
No committed snapshot is required. See sample requests in `src/CitySurfers.Api/CitySurfers.Api.http`.

Successful MVP routes, JSON property names, meanings and normal success statuses are frozen for the hackathon.
Future fixes may add optional fields/endpoints or fix genuine defects; avoid renaming/removing existing fields
or changing routes/statuses. No version-routing change is introduced. Backend Core MVP feature work stops here;
the next step is direct frontend integration and deployment of this container.

See [deployment and local demo](MVP_DEPLOYMENT.md) for startup, reset, CORS and verification commands.
