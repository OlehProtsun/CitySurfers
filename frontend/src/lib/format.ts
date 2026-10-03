export const distance = (meters: number) => `${(meters / 1000).toFixed(2)} km`;
export function duration(seconds: number) {
  const n = Math.max(0, Math.round(seconds));
  const s = String(n % 60).padStart(2, "0");
  const m = String(Math.floor(n / 60) % 60).padStart(2, "0");
  return n >= 3600 ? `${Math.floor(n / 3600)}:${m}:${s}` : `${m}:${s}`;
}
export const pace = (seconds: number | null) =>
  seconds === null || !Number.isFinite(seconds) || seconds < 0
    ? "—"
    : `${Math.floor(Math.round(seconds) / 60)}:${String(Math.round(seconds) % 60).padStart(2, "0")} /km`;
export const date = (utc: string) =>
  new Date(utc).toLocaleDateString(undefined, {
    month: "short",
    day: "numeric",
  });
export const paceComparison = (delta: number | null) =>
  delta === null
    ? "Pace comparison available after both months have runs"
    : delta === 0
      ? "Same pace as last month"
      : `${Math.abs(Math.round(delta))} sec/km ${delta < 0 ? "faster" : "slower"} than last month`;
