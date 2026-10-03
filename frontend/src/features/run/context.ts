import { createContext, useContext } from "react";
import type {
  ActiveRunResponse,
  OvertakeEvent,
  RunFinishResponse,
} from "../../api/types";
export interface RunState {
  run: ActiveRunResponse | null;
  summary: RunFinishResponse | null;
  events: OvertakeEvent[];
  busy: boolean;
  running: boolean;
  error: unknown;
  start: () => void;
  finish: () => void;
  recover: () => void;
  toggle: () => void;
  dismissSummary: () => void;
  dismissEvent: () => void;
}
export const RunContext = createContext<RunState | null>(null);
export function useRun() {
  const value = useContext(RunContext);
  if (!value) throw new Error("Missing RunProvider");
  return value;
}
export function appendEvents(
  queue: OvertakeEvent[],
  incoming: OvertakeEvent[],
) {
  return [
    ...queue,
    ...incoming.filter(
      (e) =>
        !queue.some(
          (q) =>
            q.opponent === e.opponent &&
            q.rankBefore === e.rankBefore &&
            q.rankAfter === e.rankAfter,
        ),
    ),
  ];
}
