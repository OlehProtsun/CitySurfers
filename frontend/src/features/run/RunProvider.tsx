import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "../../api/citySurfersApi";
import { keys, useHome } from "../../api/queries";
import type {
  ActiveRunResponse,
  OvertakeEvent,
  RunDetail,
  RunFinishResponse,
} from "../../api/types";
import { ApiError } from "../../api/errors";
import { env } from "../../lib/env";
import { nextCheckpoint, RunWriteGate } from "./demoRunSimulator";
import { appendEvents, RunContext } from "./context";
export function RunProvider({ children }: { children: ReactNode }) {
  const client = useQueryClient();
  const home = useHome();
  const active = useQuery({
    queryKey: keys.active,
    queryFn: async ({ signal }) => {
      try {
        return await api.active(signal);
      } catch (e) {
        if (e instanceof ApiError && e.status === 404) return null;
        throw e;
      }
    },
    enabled: !!home.data?.activeRun,
    refetchOnWindowFocus: false,
  });
  const [summary, setSummary] = useState<RunFinishResponse | null>(null);
  const [events, setEvents] = useState<OvertakeEvent[]>([]);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const gate = useRef(new RunWriteGate());
  const scheduled = useRef(false);
  const run = active.data ?? null;
  const reconcile = useCallback(async () => {
    await Promise.all(
      [
        keys.home,
        keys.history,
        keys.progress,
        ["leaderboard"],
        keys.rival,
        keys.goal,
      ].map((queryKey) => client.invalidateQueries({ queryKey })),
    );
  }, [client]);
  const accept = useCallback(
    (detail: RunDetail, withEvents = false) => {
      if ("runId" in detail) {
        client.setQueryData(keys.active, null);
        setSummary(detail);
        setError(null);
        setRunning(false);
        setEvents([]);
      } else {
        client.setQueryData(keys.active, detail);
        if (withEvents) setEvents((q) => appendEvents(q, detail.events));
      }
    },
    [client],
  );
  const recoverState = useCallback(
    async (id?: string) => {
      if (id) {
        try {
          accept(await api.run(id));
        } catch (e) {
          if (!(e instanceof ApiError && e.status === 404)) throw e;
          client.setQueryData(keys.active, null);
        }
      } else {
        const fresh = await api.home();
        client.setQueryData(keys.home, fresh);
        if (fresh.activeRun) accept(await api.run(fresh.activeRun.id));
        else client.setQueryData(keys.active, null);
      }
      await reconcile();
    },
    [accept, client, reconcile],
  );
  const mutation = useMutation({
    retry: false,
    mutationFn: (action: "start" | "progress" | "finish" | "recover") =>
      gate.current.execute(async () => {
        const current = client.getQueryData<ActiveRunResponse | null>(
          keys.active,
        );
        setError(null);
        try {
          if (action === "recover") {
            await recoverState(current?.id);
            return;
          }
          if (action === "start") {
            accept(await api.start());
            setRunning(env.simulation);
            await reconcile();
            return;
          }
          if (!current) return;
          if (action === "progress") {
            const next = nextCheckpoint(current);
            if (!next) {
              setRunning(false);
              return;
            }
            accept(await api.progressRun(current.id, next), true);
            return;
          }
          setRunning(false);
          accept(
            await api.finish(current.id, {
              distanceMeters: current.distanceMeters,
              durationSeconds: current.durationSeconds,
            }),
          );
          await reconcile();
        } catch (failure) {
          setRunning(false);
          setError(failure);
          try {
            await recoverState(current?.id);
          } catch {
            /* Explicit recovery remains required before further writes. */
          }
          throw failure;
        }
      }),
  });
  const act = useCallback(
    (action: "start" | "progress" | "finish" | "recover") => {
      if (scheduled.current || gate.current.busy) return;
      scheduled.current = true;
      mutation.mutate(action, {
        onSettled: () => {
          scheduled.current = false;
        },
      });
    },
    [mutation],
  );
  useEffect(() => {
    if (!running || !run || mutation.isPending || error || events.length)
      return;
    const timer = setTimeout(() => act("progress"), env.interval);
    return () => clearTimeout(timer);
  }, [running, run, mutation.isPending, error, events.length, act]);
  const dismissEvent = useCallback(() => setEvents((q) => q.slice(1)), []);
  return (
    <RunContext.Provider
      value={{
        run,
        summary,
        events,
        busy: mutation.isPending,
        running,
        error: error || active.error,
        start: () => act("start"),
        finish: () => {
          setRunning(false);
          act("finish");
        },
        recover: () => act("recover"),
        toggle: () => setRunning((v) => !v),
        dismissSummary: () => setSummary(null),
        dismissEvent,
      }}
    >
      {children}
    </RunContext.Provider>
  );
}
