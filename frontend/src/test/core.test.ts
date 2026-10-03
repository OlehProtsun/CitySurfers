import { afterEach, describe, expect, it, vi } from "vitest";
import { distance, duration, pace, paceComparison } from "../lib/format";
import { ApiError, errorMessage, parseProblem } from "../api/errors";
import { request } from "../api/client";
import {
  checkpoints,
  nextCheckpoint,
  RunWriteGate,
} from "../features/run/demoRunSimulator";
import { appendEvents } from "../features/run/context";
import type { OvertakeEvent } from "../api/types";
afterEach(() => vi.unstubAllGlobals());
describe("display semantics", () => {
  it("formats distance, long time and rounded pace safely", () => {
    expect(distance(6800)).toBe("6.80 km");
    expect(duration(2210)).toBe("36:50");
    expect(duration(3661)).toBe("1:01:01");
    expect(pace(299.8)).toBe("5:00 /km");
    expect(pace(null)).toBe("—");
    expect(pace(NaN)).toBe("—");
  });
  it("explains negative pace as faster", () => {
    expect(paceComparison(-12)).toContain("12 sec/km faster");
    expect(paceComparison(12)).toContain("slower");
    expect(paceComparison(null)).toContain("available");
  });
});
describe("HTTP boundary", () => {
  it("preserves Problem Details status and validation fields", () => {
    const p = parseProblem({
      status: 409,
      title: "Conflict",
      errors: { distance: ["Invalid"], bad: 4 },
    });
    expect(new ApiError(500, p).status).toBe(409);
    expect(p.errors).toEqual({ distance: ["Invalid"] });
    expect(parseProblem("<html>")).toEqual({});
    expect(errorMessage(new ApiError(401))).toContain("Invalid demo");
  });
  it("sends JSON, cancellation and the configured base URL", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(new Response('{"ok":true}', { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);
    const signal = new AbortController().signal;
    await expect(
      request("/api/test", { method: "PATCH", body: "{}", signal }),
    ).resolves.toEqual({ ok: true });
    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:8080/api/test",
      expect.objectContaining({
        signal,
        headers: expect.objectContaining({
          "Content-Type": "application/json",
        }),
      }),
    );
  });
  it("does not hide non-JSON failures or aborts", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response("offline", { status: 503 })),
    );
    await expect(request("/api/test")).rejects.toMatchObject({ status: 503 });
    vi.stubGlobal(
      "fetch",
      vi.fn().mockRejectedValue(new DOMException("Aborted", "AbortError")),
    );
    await expect(request("/api/test")).rejects.toMatchObject({
      name: "AbortError",
    });
  });
});
describe("serialized demo progress", () => {
  it("uses cumulative checkpoints with canonical finish duration and no rewards", () => {
    expect(checkpoints.at(-1)).toEqual({
      distanceMeters: 6800,
      durationSeconds: 2210,
    });
    let last = { distanceMeters: 0, durationSeconds: 0 };
    for (const p of checkpoints) {
      expect(p.distanceMeters).toBeGreaterThan(last.distanceMeters);
      expect(p.durationSeconds).toBeGreaterThan(last.durationSeconds);
      expect(Object.keys(p)).toEqual(["distanceMeters", "durationSeconds"]);
      last = p;
    }
    expect(nextCheckpoint(last)).toBeUndefined();
    expect(
      nextCheckpoint({ distanceMeters: 1300, durationSeconds: 390 })
        ?.distanceMeters,
    ).toBe(1800);
  });
  it("rejects concurrent writes and releases its lock after errors", async () => {
    const gate = new RunWriteGate();
    let release!: () => void;
    const pending = gate.execute(
      () =>
        new Promise<void>((resolve) => {
          release = resolve;
        }),
    );
    await expect(gate.execute(async () => 1)).rejects.toThrow(
      "already pending",
    );
    release();
    await pending;
    await expect(
      gate.execute(async () => {
        throw new Error("network");
      }),
    ).rejects.toThrow("network");
    expect(gate.busy).toBe(false);
    await expect(gate.execute(async () => 2)).resolves.toBe(2);
  });
  it("queues all authoritative events without duplicates", () => {
    const a: OvertakeEvent = {
      type: "OVERTAKE",
      opponent: "A",
      rankBefore: 41,
      rankAfter: 40,
      pointsAwarded: 16,
    };
    const b = { ...a, opponent: "B", rankBefore: 40, rankAfter: 39 };
    expect(appendEvents([a], [a, b])).toEqual([a, b]);
  });
});
