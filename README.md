# 🏃 CitySurfers

### Turn every run into a game.

**CitySurfers** is a gamified running platform that transforms real-world running into a competitive experience.

Instead of seeing only distance, pace and time, runners get immediate competitive goals:

> **Run 1.1 km more → overtake another runner → move from #38 to #37.**

The idea is simple: make every extra kilometer feel meaningful.

---

## ⚡ Built in ~7 Hours at HackYeah 2026

CitySurfers was created as a rapid full-stack MVP during **HackYeah 2026 in Kraków**.

HackYeah 2026 is the **12th edition of one of Europe's largest in-person hackathons**, bringing thousands of developers, designers and technology enthusiasts together at **TAURON Arena Kraków** for a 24-hour building marathon.

While the hackathon provides a 24-hour development window, the core CitySurfers MVP presented in this repository was built in approximately:

# **7 hours.**

The goal was not to create a collection of disconnected mockups.

The goal was to prove that the complete product loop could work end-to-end:

**idea → architecture → backend → database → frontend → gameplay simulation → automated validation**

---

# 🎯 The Problem

Running apps are great at telling you what you already did.

But they are often less effective at answering:

> **Why should I run one more kilometer?**

Imagine planning a 5 km run and wanting to stop at 4 km.

Normally, nothing meaningful happens if you stop.

CitySurfers adds an immediate consequence:

> **“1.1 km more and you will overtake Runner_42 and move from #38 to #37.”**

Instead of one large fitness goal, a run becomes a sequence of small, visible and achievable competitive objectives.

---

# 💡 The Concept

CitySurfers adds a multiplayer game layer on top of real-world running.

During a run, the application can show:

- your current city ranking;
- the runner ahead of you;
- distance required to overtake them;
- points available for an overtake;
- rank progression;
- daily and monthly leaderboards;
- your current rival;
- personal progress;
- city running activity.

The central gameplay mechanic is the **Overtake**.

```text
#38

Runner_92 is ahead
1.1 km remaining

        ↓

OVERTAKE!

Runner_92 passed
#38 → #37

+16 points
```

This turns physical activity into a continuous game loop.

---

# 🕹️ Hackathon Demo Flow

The repository contains a deterministic end-to-end demo that runs against the real backend.

```text
Login
  ↓
Home
  ↓
Start Run
  ↓
Runner_92
#41 → #40
+16 pts
  ↓
Marta
#40 → #39
+14 pts
  ↓
Runner_17
#39 → #38
+11 pts
  ↓
Kamil_24
#38 → #37
+18 pts
  ↓
Finish Run
  ↓
59 points
  ↓
New monthly rival
#36 — 64 points
  ↓
6 more points to overtake
```

The frontend does **not** calculate authoritative rewards or ranking changes.

It submits progress to the backend, while the server determines:

- overtakes;
- points;
- ranking changes;
- competition targets;
- next goals;
- post-run results.

---

# ✨ MVP Features

### 🏃 Running Session

- Start a run
- Update distance and duration
- Calculate pace
- Finish a run
- Persist completed sessions
- Resume authoritative state after refresh

### ⚔️ Overtake System

- Dynamic next target
- Distance-to-overtake goals
- Overtake events
- Point rewards
- Ranking progression

### 🏆 Leaderboards

- Daily leaderboard
- Monthly leaderboard
- Current-user ranking
- Nearby competitors

### 🎯 Rival System

When no active run target exists, CitySurfers can select the runner directly above the user in the monthly leaderboard.

The app shows:

- rival position;
- rival points;
- point gap;
- points required to pass them.

### 📈 Personal Progress

- Running history
- Lifetime statistics
- Weekly statistics
- Monthly statistics
- Pace aggregation
- Month-to-month comparison

### 🗺️ Kraków Activity Map

The MVP includes an interactive city activity map based on aggregate running zones around Kraków.

For privacy, demo map points represent **aggregate activity areas**, not individual runner GPS coordinates.

### 📱 Mobile-First UI

The frontend provides dedicated screens for:

- Home
- Active Run
- Ranking
- Activity Map
- Progress
- Demo Login
- Post-run feedback / overtake interactions

---

# 🏗️ Architecture

CitySurfers uses a modular **N-Layer architecture** with clear dependency boundaries.

```text
┌──────────────────────────────────────────────┐
│              React Frontend                  │
│       TypeScript · Vite · Tailwind           │
└─────────────────────┬────────────────────────┘
                      │ REST API
                      ▼
┌──────────────────────────────────────────────┐
│               ASP.NET Core API               │
│        HTTP · Validation · API Contracts      │
└─────────────────────┬────────────────────────┘
                      │
                      ▼
┌──────────────────────────────────────────────┐
│             Application Layer                │
│     Use Cases · Services · Interfaces        │
└─────────────────────┬────────────────────────┘
                      │
                      ▼
┌──────────────────────────────────────────────┐
│                Domain Layer                  │
│       Business Rules · Run Lifecycle         │
└──────────────────────────────────────────────┘

            ▲
            │ implements abstractions
            │

┌──────────────────────────────────────────────┐
│            Infrastructure Layer              │
│          MongoDB · Persistence               │
└─────────────────────┬────────────────────────┘
                      │
                      ▼
                 ┌─────────┐
                 │ MongoDB │
                 └─────────┘
```

The project is intentionally implemented as a **modular monolith**, keeping the MVP simple while preserving boundaries that allow individual components to be replaced later.

---

# 🧱 Backend Structure

```text
src/
├── CitySurfers.Api
├── CitySurfers.Application
├── CitySurfers.Domain
└── CitySurfers.Infrastructure
```

### `CitySurfers.Api`

ASP.NET Core presentation layer.

Responsible for:

- REST endpoints;
- HTTP contracts;
- configuration;
- dependency composition;
- error handling;
- health endpoints;
- CORS.

### `CitySurfers.Application`

Application orchestration and use cases.

Contains abstractions and services responsible for application workflows while remaining independent from MongoDB implementation details.

### `CitySurfers.Domain`

Core business logic.

Contains rules related to:

- running lifecycle;
- competition;
- state transitions;
- domain behaviour.

The domain has no dependency on ASP.NET Core or MongoDB.

### `CitySurfers.Infrastructure`

External technical implementations.

Currently responsible primarily for:

- MongoDB persistence;
- repositories/stores;
- database initialization;
- infrastructure configuration.

---

# 🛠️ Technology Stack

## Backend

- **C#**
- **.NET 10**
- **ASP.NET Core**
- REST API
- Built-in Dependency Injection
- Async I/O
- Strongly typed configuration
- Problem Details error handling
- OpenAPI for development
- xUnit

## Database

- **MongoDB**
- MongoDB .NET Driver
- Persistent users and runs
- Database indexes
- Health/readiness verification

## Frontend

- **React**
- **TypeScript**
- **Vite**
- **Tailwind CSS**
- React Router
- TanStack Query
- Motion
- Lucide
- Recharts

## Maps

- **MapLibre GL JS**
- OpenFreeMap
- OpenStreetMap-derived map data

## Testing

- xUnit
- Vitest
- React Testing Library
- Playwright
- API integration tests
- Docker/MongoDB smoke tests
- Production-mode smoke tests
- End-to-end browser tests

## DevOps

- Docker
- Docker Compose
- Multi-stage container builds
- Non-root runtime container
- GitHub Actions
- Backend CI
- Frontend CI
- Environment-based configuration

---

# 🤖 AI Engineering

CitySurfers was also an experiment in **structured AI-assisted software engineering**.

AI was used as an engineering accelerator — not as an uncontrolled code generator.

The development workflow was based on three major project-level context layers:

```text
AppContext.md
      ↓
What are we building?

AGENT.md
      ↓
How must it be engineered?

PLAN.md
      ↓
What should be implemented next?

      ↓

AI-assisted implementation

      ↓

Build + Tests + Smoke Validation
```

## Context Engineering

`AppContext.md` describes the complete product vision, including:

- problem definition;
- user experience;
- game mechanics;
- ranking concepts;
- running data;
- product boundaries;
- MVP scope;
- future product direction.

This gives AI agents persistent high-level product context instead of relying on isolated prompts.

## Engineering Guardrails

`AGENT.md` defines the engineering rules that AI-assisted development must follow:

- N-Layer Architecture;
- SOLID principles;
- Dependency Inversion;
- separation of concerns;
- replaceable modules;
- isolated MVP implementations;
- thin API endpoints;
- testability;
- production-oriented configuration;
- controlled scope.

## Plan-Driven Development

Implementation was divided into explicit stages through `PLAN.md`.

Instead of asking an AI agent to repeatedly analyze the entire repository and invent a new roadmap, each stage provides:

- a defined goal;
- constraints;
- implementation tasks;
- architectural boundaries;
- acceptance criteria;
- validation requirements.

The agent then executes that plan incrementally.

## AI + Verification

Generated or AI-assisted code was not treated as correct simply because it compiled.

Changes were validated through:

```text
Implementation
     ↓
Unit Tests
     ↓
Integration Tests
     ↓
Real MongoDB Smoke Tests
     ↓
Docker Production Smoke
     ↓
Frontend Tests
     ↓
Playwright E2E
```

This approach combines the development speed of AI-assisted coding with traditional software-engineering controls.

### The principle

> **Use AI to increase implementation speed, while architecture, contracts and automated verification constrain the solution.**

For a hackathon environment, this allowed significantly faster iteration without intentionally sacrificing the structure needed to continue development after the event.

---

# 🧠 Engineering Decisions

### Modular Monolith over Microservices

A hackathon MVP does not need premature distributed-system complexity.

A modular monolith provides:

- fast development;
- simple deployment;
- clear domain boundaries;
- future extensibility.

### Replaceable MVP Components

Temporary hackathon implementations are isolated behind abstractions.

For example, deterministic competition providers can later be replaced with systems driven by real users and live data without rewriting unrelated modules.

### Backend-Authoritative Game State

The frontend never decides whether an overtake occurred.

The server owns:

- scoring;
- rank transitions;
- targets;
- rewards;
- run state.

This keeps future mobile/web clients consistent.

### Privacy-Aware Map Design

The current activity map exposes aggregate areas rather than simulated individual GPS locations.

### Deterministic Demo Mode

Hackathon demonstrations need to be repeatable.

The local environment can reset demo runs on startup so every presentation starts from the same known state.

---

# 🐳 Run Locally

## Requirements

You need:

- Docker
- Docker Compose
- Node.js
- npm

---

## 1. Start Backend + MongoDB

From the repository root:

```bash
docker compose -f docker-compose.demo.yml up --build
```

This starts:

```text
MongoDB
   +
CitySurfers API
```

The API is exposed on:

```text
localhost:8080
```

MongoDB runs inside Docker, so no external MongoDB instance is required for the local hackathon demo.

---

## 2. Start Frontend

Open another terminal:

```bash
cd frontend

npm ci
cp .env.example .env
npm run dev
```

Open:

```text
localhost:5173
```

---

## 3. Demo Login

```text
Username: demo
Password: 1234
```

Then:

```text
Enter the city
→ Start run
→ Catch four runners
→ Finish
→ View result
→ Check ranking / progress / map
```

---

# 🧪 Validation

## Backend

```bash
dotnet restore CitySurfers.sln
dotnet build CitySurfers.sln --no-restore
dotnet test CitySurfers.sln --no-build --no-restore
```

The backend contains extensive automated coverage around:

- run state transitions;
- ranking behaviour;
- scoring;
- overtakes;
- validation;
- leaderboards;
- progress aggregation;
- MongoDB persistence;
- concurrency;
- API contracts.

---

## Frontend

```bash
cd frontend

npm run lint
npm run typecheck
npm run test:run
npm run build
```

E2E:

```bash
npx playwright install chromium
npm run e2e
```

Playwright validates the complete browser demo against the real backend.

---

# 🔄 CI

GitHub Actions automatically validates backend and frontend changes.

The frontend pipeline performs:

```text
Install
→ Lint
→ Type Check
→ Tests
→ Production Build
```

The backend pipeline performs restore, Release build and automated testing without requiring cloud database credentials.

---

# 🔐 Current MVP Boundaries

CitySurfers is a **hackathon MVP**, not a production fitness platform.

The current version intentionally does not include:

- real GPS recording;
- real multi-user authentication;
- production user accounts;
- live WebSocket competition;
- fitness-device integrations;
- Strava / Garmin / Apple Health integrations;
- push notifications;
- anti-cheat infrastructure;
- 1v1 matchmaking;
- production AI Coach;
- production deployment.

The demo login uses a shared fictional user and does not issue an authentication token.

These constraints are deliberate: the hackathon focused on proving the product concept and architecture rather than pretending unfinished production features already exist.

---

# 🚀 Where It Can Go Next

The architecture was designed so that the current MVP can evolve toward:

```text
Real GPS
   ↓
Real runners
   ↓
Live city competition
   ↓
Dynamic rivals
   ↓
Routes & King of Route
   ↓
1v1 races
   ↓
Achievements
   ↓
Fitness platform integrations
   ↓
AI Running Coach
```

The long-term vision is a platform where:

> **the city itself becomes the game map and every run becomes a multiplayer session.**

---

# 🏁 Hackathon Outcome

In approximately **7 hours**, CitySurfers went from a product concept to a working full-stack prototype containing:

- a structured .NET backend;
- MongoDB persistence;
- REST contracts;
- running-session lifecycle;
- gamified overtakes;
- ranking and rival systems;
- a mobile-first React interface;
- interactive Kraków map;
- Dockerized demo environment;
- automated tests;
- CI;
- end-to-end browser validation;
- an AI-assisted engineering workflow.

The project demonstrates not only rapid prototyping, but the ability to combine:

**product thinking + backend architecture + frontend development + DevOps + testing + AI engineering**

under extreme time constraints.

---

## Status

**Hackathon MVP / Proof of Concept**

Built for **HackYeah 2026 — Kraków, Poland**.

**Build time: ~7 hours.**

---

### CitySurfers

**Run. Compete. Overtake.**
