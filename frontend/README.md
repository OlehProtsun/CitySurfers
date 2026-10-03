# CitySurfers frontend · Stage 7

React + TypeScript + Vite SPA. React Router, TanStack Query, Tailwind CSS, locally bundled Inter, Lucide, Motion, MapLibre GL JS and Recharts. Node 22.14+ and npm are required.

## Local demo

From the repository root, start the Stage 6 backend:

```sh
docker compose -f docker-compose.demo.yml up --build
```

In another terminal:

```sh
cd frontend
npm ci
cp .env.example .env
npm run dev
```

Open http://localhost:5173 and enter `demo / 1234`. Login calls the API; sessionStorage stores display identity only. There are no tokens or real multi-user authentication. Browser refresh confirms server state through Home. Existing runs resume paused.

## Environment

| Setting | Purpose |
| --- | --- |
| VITE_API_BASE_URL | Required backend origin; local Compose uses http://localhost:8080 |
| VITE_MAP_STYLE_URL | OpenFreeMap style; defaults to https://tiles.openfreemap.org/styles/liberty |
| VITE_DEMO_SIMULATION_ENABLED | Explicit `true` enables demo progress; otherwise disabled |
| VITE_DEMO_STEP_INTERVAL_MS | Time between acknowledged checkpoints, default 1200, minimum 250 ms |

Vite embeds configuration at build time. Never put secrets in frontend variables.

## Demo story

Home → Start run → catch four targets → Finish run → summary → next rival. The simulator sends only cumulative distance and duration to real PATCH endpoints; the final checkpoint is 6800 m / 2210 s. Rank, targets, overtakes and rewards always come from the server. Writes are serialized; errors pause simulation and recover server state. Retry recovery before resuming. Pause affects the simulator, not backend lifecycle. Finish uses the last acknowledged totals and checks run detail if the response is lost.

The fresh deterministic backend awards 59 points with rank 41 → 37 and then a 6-point monthly rival goal. Restarting the configured local demo API resets demo runs. Do not reset a shared/persistent environment to repeat tests.

## Checks

```sh
npm run lint
npm run typecheck
npm run test:run
npm run build
npx playwright install chromium
npm run e2e
```

`npm run test` watches tests. E2E requires a fresh local demo backend; it mutates that isolated demo. Playwright starts/stops Vite automatically and captures screenshots at 375×812, 390×844, 430×932 and 1440×1000. For the repeatable production-build smoke from repository root:

```powershell
pwsh -NoProfile -File tests/Stage7.FrontendSmoke.ps1
```

The smoke requires free ports 8080/5173, Docker, Node and npm. It creates a uniquely named Compose project, waits for readiness, builds the frontend, runs Chromium against Vite preview and cleans its processes/containers/temporary volume in `finally`. No existing demo data is removed. Normal frontend CI runs lint, types, unit/component tests and build without cloud secrets.

## Map and accessibility

OpenFreeMap Liberty uses OpenStreetMap-derived data without an API key. MapLibre's module worker is bundled by Vite. Attribution remains visible, including during provider failure. A narrow compatibility guard skips road-shield features missing `ref_length`; the provider style is otherwise preserved. Map activity is aggregate demo zones at approximate Kraków public-area centres. No geolocation is requested and no individual runner coordinates exist. Zone buttons provide a keyboard-accessible alternative to map taps.

UI supports safe areas, 44px touch controls, visible focus and reduced motion. Charts load only on Progress. MapLibre is a separately loaded GPU map bundle; its approximately 1 MB minified renderer is an expected package-size cost.

## Build and limits

`npm run build` outputs static files to `dist/`; `npm run preview` serves them locally. Future static hosting needs SPA fallback to `index.html` for client routes. No hosting provider or cloud deployment is configured.

This MVP has Home, Ranking, Map and Progress only. No real GPS, offline recording, real authentication, routes, races, game rating, achievements, AI coach, fitness imports or notifications. The demo is a shared server identity, and external map availability depends on the provider/network.
