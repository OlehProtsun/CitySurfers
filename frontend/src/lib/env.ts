export function parseEnvironment(values: Record<string, string | undefined>) {
  const api = values.VITE_API_BASE_URL?.trim();
  if (!api || !/^https?:\/\//.test(api))
    throw new Error("Set VITE_API_BASE_URL to the backend HTTP(S) URL.");
  const mapStyle =
    values.VITE_MAP_STYLE_URL || "https://tiles.openfreemap.org/styles/liberty";
  if (!/^https?:\/\//.test(mapStyle)) throw new Error("Invalid map style URL.");
  const interval = Number(values.VITE_DEMO_STEP_INTERVAL_MS || 1200);
  if (!Number.isFinite(interval) || interval < 250)
    throw new Error("Demo interval must be at least 250 ms.");
  return {
    apiBaseUrl: api.replace(/\/$/, ""),
    mapStyle,
    simulation: values.VITE_DEMO_SIMULATION_ENABLED === "true",
    interval,
  };
}
export const env = parseEnvironment(import.meta.env);
