import { expect, it } from "vitest";
import { parseEnvironment } from "../lib/env";
import { guardLibertyShields, zoneGeoJson } from "../features/map/mapConfig";
import fixtures from "./server-fixtures.json";
it("requires API configuration and safely gates simulation", () => {
  expect(() => parseEnvironment({})).toThrow("VITE_API_BASE_URL");
  expect(
    parseEnvironment({ VITE_API_BASE_URL: "http://localhost:8080/" }),
  ).toMatchObject({
    simulation: false,
    apiBaseUrl: "http://localhost:8080",
    interval: 1200,
  });
  expect(() =>
    parseEnvironment({
      VITE_API_BASE_URL: "http://localhost",
      VITE_DEMO_STEP_INTERVAL_MS: "NaN",
    }),
  ).toThrow("interval");
});
it("maps only aggregate zones to longitude-first geometry", () => {
  const zone = fixtures["map/activity?period=live"].zones[0]!;
  const data = zoneGeoJson([zone]);
  expect(data.features[0]?.geometry.coordinates).toEqual([
    zone.longitude,
    zone.latitude,
  ]);
  expect(data.features[0]?.properties).toEqual({
    id: zone.id,
    activeRunners: zone.activeRunners,
    activityLevel: zone.activityLevel,
  });
});
it("guards only Liberty nullable road-shield filters", () => {
  const style = guardLibertyShields(undefined, {
    version: 8,
    sources: {},
    layers: [
      {
        id: "highway-shield-non-us",
        type: "symbol",
        source: "roads",
        filter: ["<=", ["get", "ref_length"], 6],
      },
      { id: "background", type: "background" },
    ],
  });
  expect(style.layers[0]).toMatchObject({
    filter: ["all", ["has", "ref_length"], ["<=", ["get", "ref_length"], 6]],
  });
  expect(style.layers[1]).toEqual({ id: "background", type: "background" });
});
