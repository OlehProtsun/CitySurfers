import { useEffect } from "react";
import { afterEach, expect, it, vi } from "vitest";
import { act, cleanup, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RunProvider } from "../features/run/RunProvider";
import { useRun, type RunState } from "../features/run/context";
import { api } from "../api/citySurfersApi";
import { ApiError } from "../api/errors";
import type {
  ActiveRunResponse,
  HomeResponse,
  RunFinishResponse,
} from "../api/types";
const initial: ActiveRunResponse = {
  id: "run-1",
  status: "active",
  startedAtUtc: "2026-10-03T10:00:00Z",
  updatedAtUtc: "2026-10-03T10:00:00Z",
  distanceMeters: 0,
  durationSeconds: 0,
  averagePaceSecondsPerKm: null,
  competition: {
    rank: 41,
    seasonPointsEarned: 0,
    currentTarget: {
      opponent: "Runner_92",
      distanceToOvertakeMeters: 1200,
      potentialPoints: 16,
    },
  },
  events: [],
};
const home: HomeResponse = {
  today: { rank: 41, points: 0 },
  activeRun: null,
  nextGoal: null,
};
let state: RunState;
function Observer() {
  const current = useRun();
  useEffect(() => {
    state = current;
  }, [current]);
  return (
    <div>
      {current.run?.distanceMeters ?? "idle"}
      <button disabled={current.busy}>finish</button>
      {current.summary && (
        <span>summary {current.summary.seasonPointsEarned}</span>
      )}
    </div>
  );
}
function setup() {
  vi.spyOn(api, "home").mockResolvedValue(home);
  vi.spyOn(api, "active").mockResolvedValue(initial);
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={client}>
      <RunProvider>
        <Observer />
      </RunProvider>
    </QueryClientProvider>,
  );
}
afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
  vi.useRealTimers();
});
it("prevents duplicate start while pending and recovers an existing run on 409", async () => {
  setup();
  vi.spyOn(api, "start").mockRejectedValue(new ApiError(409));
  vi.mocked(api.home).mockResolvedValue({ ...home, activeRun: initial });
  vi.spyOn(api, "run").mockResolvedValue(initial);
  act(() => {
    state.start();
    state.start();
  });
  await waitFor(() => expect(state.busy).toBe(false));
  expect(api.start).toHaveBeenCalledTimes(1);
  await waitFor(() => expect(state.run?.id).toBe(initial.id));
  expect(state.running).toBe(false);
});
it("pauses after failed progress, recovers totals and resumes only explicitly", async () => {
  setup();
  vi.spyOn(api, "start").mockResolvedValue(initial);
  vi.spyOn(api, "progressRun").mockRejectedValue(new TypeError("network"));
  vi.spyOn(api, "run").mockResolvedValue({
    ...initial,
    distanceMeters: 600,
    durationSeconds: 180,
  });
  act(() => state.start());
  await waitFor(() => expect(state.running).toBe(true));
  await waitFor(() => expect(api.progressRun).toHaveBeenCalledTimes(1), {
    timeout: 2500,
  });
  await waitFor(() => expect(state.busy).toBe(false));
  expect(state.running).toBe(false);
  expect(state.run?.distanceMeters).toBe(600);
  expect(state.error).toBeTruthy();
  act(() => state.recover());
  await waitFor(() => expect(state.error).toBeNull());
  expect(state.running).toBe(false);
  act(() => state.toggle());
  expect(state.running).toBe(true);
  act(() => state.toggle());
  expect(state.running).toBe(false);
});
it("serializes slow progress and disables finish until the authoritative write resolves", async () => {
  setup();
  vi.spyOn(api, "start").mockResolvedValue(initial);
  let resolve!: (run: ActiveRunResponse) => void;
  vi.spyOn(api, "progressRun").mockImplementation(
    () =>
      new Promise((r) => {
        resolve = r;
      }),
  );
  act(() => state.start());
  await waitFor(() => expect(api.progressRun).toHaveBeenCalledTimes(1), {
    timeout: 2500,
  });
  expect(screen.getByRole("button", { name: "finish" })).toBeDisabled();
  await act(async () => {
    resolve({ ...initial, distanceMeters: 600, durationSeconds: 180 });
  });
  await waitFor(() => expect(state.busy).toBe(false));
  act(() => state.toggle());
  expect(state.running).toBe(false);
  expect(state.run?.distanceMeters).toBe(600);
});
it("recovers a lost finish response from detail without sending finish again", async () => {
  setup();
  vi.spyOn(api, "start").mockResolvedValue(initial);
  vi.spyOn(api, "finish").mockRejectedValue(new TypeError("lost response"));
  const summary: RunFinishResponse = {
    runId: "run-1",
    startedAtUtc: initial.startedAtUtc,
    finishedAtUtc: initial.startedAtUtc,
    distanceMeters: 0,
    durationSeconds: 0,
    averagePaceSecondsPerKm: null,
    overtakesCount: 0,
    overtakes: [],
    rankBefore: 41,
    rankAfter: 41,
    seasonPointsEarned: 0,
    nextTarget: null,
  };
  vi.spyOn(api, "run").mockResolvedValue(summary);
  act(() => state.start());
  await waitFor(() => expect(state.run).not.toBeNull());
  await waitFor(() => expect(state.busy).toBe(false));
  act(() => state.finish());
  expect(await screen.findByText("summary 0")).toBeVisible();
  expect(api.finish).toHaveBeenCalledTimes(1);
  expect(state.run).toBeNull();
  expect(state.error).toBeNull();
});
