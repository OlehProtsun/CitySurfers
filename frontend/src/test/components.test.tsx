import HomePage from "../pages/HomePage";
import { afterEach, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { api } from "../api/citySurfersApi";
import { ApiError } from "../api/errors";
import { GoalCard } from "../components/ui";
import LoginPage from "../pages/LoginPage";
import RankingPage from "../pages/RankingPage";
import ActivityMapPage from "../pages/ActivityMapPage";
import ProgressPage from "../pages/ProgressPage";
import { OvertakeOverlay } from "../features/run/OvertakeOverlay";
import { RunContext, type RunState } from "../features/run/context";
import fixtures from "./server-fixtures.json";
vi.mock("../features/map/ActivityMap", () => ({
  default: () => <div>City map</div>,
}));
vi.mock("recharts", () => ({
  ResponsiveContainer: ({ children }: { children: ReactNode }) => (
    <div>{children}</div>
  ),
  AreaChart: () => null,
  LineChart: () => null,
  Area: () => null,
  Line: () => null,
  XAxis: () => null,
  YAxis: () => null,
  Tooltip: () => null,
}));
afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});
function renderQuery(node: ReactNode) {
  return render(
    <QueryClientProvider
      client={
        new QueryClient({
          defaultOptions: {
            queries: { retry: false },
            mutations: { retry: false },
          },
        })
      }
    >
      {node}
    </QueryClientProvider>,
  );
}
it("login submits credentials and enters after server success", async () => {
  const identity = { id: "demo", username: "demo", displayName: "Demo Runner" };
  vi.spyOn(api, "login").mockResolvedValue(identity);
  const done = vi.fn();
  renderQuery(<LoginPage onLogin={done} />);
  await userEvent.click(screen.getByRole("button", { name: "Enter the city" }));
  await waitFor(() => expect(done).toHaveBeenCalledWith(identity));
  expect(api.login).toHaveBeenCalledWith(
    { username: "demo", password: "1234" },
    expect.anything(),
  );
});
it("login handles unauthorized response", async () => {
  vi.spyOn(api, "login").mockRejectedValue(new ApiError(401));
  renderQuery(<LoginPage onLogin={vi.fn()} />);
  await userEvent.click(screen.getByRole("button", { name: "Enter the city" }));
  expect(await screen.findByRole("alert")).toHaveTextContent(
    "Invalid demo credentials",
  );
});
it("goal card handles rival, overtake and null", () => {
  const goal = { ...fixtures.home.nextGoal, type: "rival_points" as const };
  const view = render(<GoalCard goal={goal} />);
  expect(screen.getByText("9 pts to pass")).toBeVisible();
  view.rerender(
    <GoalCard
      goal={{
        ...goal,
        type: "run_overtake",
        remainingDistanceMeters: 600,
        potentialPoints: 16,
      }}
    />,
  );
  expect(screen.getByText("600 m to overtake")).toBeVisible();
  view.rerender(<GoalCard goal={null} />);
  expect(screen.getByText("You’re at the top.")).toBeVisible();
});
it("ranking switches periods and highlights server current-user flag", async () => {
  const get = vi
    .spyOn(api, "leaderboard")
    .mockResolvedValue(fixtures["leaderboards/today"]);
  vi.spyOn(api, "rival").mockResolvedValue(fixtures["rivals/current"]);
  renderQuery(<RankingPage />);
  expect(await screen.findByLabelText("Your ranking")).toBeVisible();
  await userEvent.click(screen.getByRole("button", { name: "month" }));
  await waitFor(() =>
    expect(get).toHaveBeenCalledWith("month", expect.any(AbortSignal)),
  );
});
it("map switches query period and opens aggregate zone details", async () => {
  const get = vi.spyOn(api, "map").mockResolvedValue({
    ...fixtures["map/activity?period=live"],
    period: "live",
  });
  renderQuery(<ActivityMapPage />);
  await userEvent.click(
    (await screen.findAllByRole("button", { name: /runners in area/ }))[0]!,
  );
  expect(screen.getByText("ACTIVITY ZONE")).toBeVisible();
  await userEvent.click(screen.getByRole("button", { name: "month" }));
  await waitFor(() =>
    expect(get).toHaveBeenCalledWith("month", expect.any(AbortSignal)),
  );
  expect(screen.queryByText("ACTIVITY ZONE")).not.toBeInTheDocument();
});
it("progress handles zero history and real aggregates", async () => {
  vi.spyOn(api, "progress").mockResolvedValue(fixtures.progress);
  vi.spyOn(api, "history").mockResolvedValue({ items: [] });
  renderQuery(<ProgressPage />);
  expect(
    await screen.findByText("Your first chapter starts outside"),
  ).toBeVisible();
  expect(screen.getByText("0 completed runs · 0 overtakes")).toBeVisible();
});
it("overtake displays server opponent, transition and award", async () => {
  const state: RunState = {
    run: null,
    summary: null,
    events: [
      {
        type: "OVERTAKE",
        opponent: "Marta",
        rankBefore: 40,
        rankAfter: 39,
        pointsAwarded: 14,
      },
    ],
    busy: false,
    running: false,
    error: null,
    start: vi.fn(),
    finish: vi.fn(),
    recover: vi.fn(),
    toggle: vi.fn(),
    dismissSummary: vi.fn(),
    dismissEvent: vi.fn(),
  };
  render(
    <RunContext.Provider value={state}>
      <OvertakeOverlay />
    </RunContext.Provider>,
  );
  await waitFor(() =>
    expect(screen.getByRole("heading", { name: "OVERTAKE!" })).toBeVisible(),
  );
  expect(screen.getByText("Marta")).toBeVisible();
  expect(screen.getByText("#40 → #39")).toBeVisible();
  expect(screen.getByText("+14 pts")).toBeVisible();
});

it("progress renders populated history, statistics and both charts", async () => {
  vi.spyOn(api, "progress").mockResolvedValue({
    ...fixtures.progress,
    lifetime: {
      ...fixtures.progress.lifetime,
      completedRuns: 1,
      totalDistanceMeters: 6800,
      totalOvertakes: 4,
      totalPointsEarned: 59,
    },
  });
  vi.spyOn(api, "history").mockResolvedValue({
    items: [
      {
        id: "run-1",
        startedAtUtc: "2026-10-03T10:00:00Z",
        finishedAtUtc: "2026-10-03T10:36:50Z",
        distanceMeters: 6800,
        durationSeconds: 2210,
        averagePaceSecondsPerKm: 325,
        overtakesCount: 4,
        pointsEarned: 59,
      },
    ],
  });
  renderQuery(<ProgressPage />);
  expect(
    await screen.findByText("1 completed runs · 4 overtakes"),
  ).toBeVisible();
  expect(screen.getByText("+59 pts")).toBeVisible();
  expect(
    screen.getByLabelText("Recent run distance chart in kilometres"),
  ).toBeVisible();
  expect(
    screen.getByLabelText(
      "Recent pace chart, lower seconds per kilometre is faster",
    ),
  ).toBeVisible();
});

it("live Home renders authoritative target and metrics and disables pending controls", async () => {
  vi.spyOn(api, "home").mockResolvedValue({
    ...fixtures.home,
    nextGoal: { ...fixtures.home.nextGoal, type: "rival_points" },
  });
  vi.spyOn(api, "map").mockResolvedValue({
    ...fixtures["map/activity?period=live"],
    period: "live",
  });
  const state: RunState = {
    run: {
      id: "run-1",
      status: "active",
      startedAtUtc: "2026-10-03T10:00:00Z",
      updatedAtUtc: "2026-10-03T10:03:00Z",
      distanceMeters: 600,
      durationSeconds: 180,
      averagePaceSecondsPerKm: 300,
      competition: {
        rank: 41,
        seasonPointsEarned: 0,
        currentTarget: {
          opponent: "Runner_92",
          distanceToOvertakeMeters: 600,
          potentialPoints: 16,
        },
      },
      events: [],
    },
    summary: null,
    events: [],
    busy: true,
    running: true,
    error: null,
    start: vi.fn(),
    finish: vi.fn(),
    recover: vi.fn(),
    toggle: vi.fn(),
    dismissSummary: vi.fn(),
    dismissEvent: vi.fn(),
  };
  renderQuery(
    <RunContext.Provider value={state}>
      <HomePage />
    </RunContext.Provider>,
  );
  expect(await screen.findByText("0.60 km")).toBeVisible();
  expect(screen.getByText("03:00")).toBeVisible();
  expect(screen.getByText("5:00 /km")).toBeVisible();
  await waitFor(() => expect(screen.getByRole("heading", { name: "Runner_92" })).toBeVisible());
  expect(screen.getByText("+16 pts")).toBeVisible();
  expect(screen.getByRole("button", { name: "Saving…" })).toBeDisabled();
  expect(screen.getByRole("button", { name: "Pause demo" })).toBeDisabled();
});
