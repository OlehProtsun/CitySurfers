import { request } from "./client";
import type * as T from "./types";
const write = <T>(path: string, method: string, body?: unknown) =>
  request<T>(path, {
    method,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
export const api = {
  login: (body: T.LoginRequest) =>
    write<T.LoginResponse>("/api/auth/login", "POST", body),
  home: (signal?: AbortSignal) =>
    request<T.HomeResponse>("/api/home", { signal }),
  active: (signal?: AbortSignal) =>
    request<T.ActiveRunResponse>("/api/runs/active", { signal }),
  run: (id: string, signal?: AbortSignal) =>
    request<T.RunDetail>(`/api/runs/${encodeURIComponent(id)}`, { signal }),
  start: () => write<T.ActiveRunResponse>("/api/runs", "POST"),
  progressRun: (id: string, body: T.RunProgressRequest) =>
    write<T.ActiveRunResponse>(
      `/api/runs/${encodeURIComponent(id)}/progress`,
      "PATCH",
      body,
    ),
  finish: (id: string, body: T.RunProgressRequest) =>
    write<T.RunFinishResponse>(
      `/api/runs/${encodeURIComponent(id)}/finish`,
      "POST",
      body,
    ),
  history: (signal?: AbortSignal) =>
    request<T.RunHistoryResponse>("/api/runs/history?limit=10", { signal }),
  progress: (signal?: AbortSignal) =>
    request<T.ProgressResponse>("/api/progress", { signal }),
  leaderboard: (period: "today" | "month", signal?: AbortSignal) =>
    request<T.LeaderboardResponse>(`/api/leaderboards/${period}`, { signal }),
  rival: (signal?: AbortSignal) =>
    request<T.RivalResponse>("/api/rivals/current", { signal }),
  goal: (signal?: AbortSignal) =>
    request<{ goal: T.NextGoal | null }>("/api/goals/next", { signal }),
  map: (period: T.MapPeriod, signal?: AbortSignal) =>
    request<T.ActivityMapResponse>(`/api/map/activity?period=${period}`, {
      signal,
    }),
};
