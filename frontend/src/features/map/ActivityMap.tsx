import { useEffect, useRef, useState } from "react";
import * as maplibregl from "maplibre-gl";
import type { GeoJSONSource } from "maplibre-gl";

import workerUrl from "maplibre-gl/dist/maplibre-gl-worker.mjs?worker&url";
maplibregl.setWorkerUrl(workerUrl);
import "maplibre-gl/dist/maplibre-gl.css";
import type { ActivityZone } from "../../api/types";
import { mapConfig, zoneGeoJson, guardLibertyShields } from "./mapConfig";
export default function ActivityMap({
  zones,
  onSelect,
}: {
  zones: ActivityZone[];
  onSelect?: (zone: ActivityZone) => void;
}) {
  const container = useRef<HTMLDivElement>(null);
  const mapRef = useRef<maplibregl.Map | null>(null);
  const latest = useRef({ zones, onSelect });
  const [failed, setFailed] = useState(false);
  const [ready, setReady] = useState(false);
  useEffect(() => {
    latest.current = { zones, onSelect };
    const source = mapRef.current?.getSource("activity") as
      GeoJSONSource | undefined;
    source?.setData(zoneGeoJson(zones));
  }, [zones, onSelect]);
  useEffect(() => {
    if (!container.current) return;
    let map: maplibregl.Map;
    try {
      map = new maplibregl.Map({
        container: container.current,
        center: mapConfig.center,
        zoom: mapConfig.zoom,
        attributionControl: false,
        cooperativeGestures: true,
      });
      mapRef.current = map;
    } catch {
      queueMicrotask(() => setFailed(true));
      return;
    }
    map.addControl(
      new maplibregl.AttributionControl({ compact: false }),
      "bottom-right",
    );
    map.addControl(
      new maplibregl.NavigationControl({ showCompass: false }),
      "top-right",
    );
    map.setStyle(mapConfig.style, { transformStyle: guardLibertyShields });
    const timeout = setTimeout(() => {
      if (!map.isStyleLoaded()) setFailed(true);
    }, 12000);
    map.on("error", () => setFailed(true));
    map.on("load", () => {
      setReady(true);
      clearTimeout(timeout);
      setFailed(false);
      map.addSource("activity", {
        type: "geojson",
        data: zoneGeoJson(latest.current.zones),
      });
      map.addLayer({
        id: "activity-halo",
        type: "circle",
        source: "activity",
        paint: {
          "circle-radius": [
            "interpolate",
            ["linear"],
            ["get", "activeRunners"],
            0,
            20,
            100,
            58,
          ],
          "circle-color": "#b7ef53",
          "circle-opacity": 0.24,
          "circle-blur": 0.5,
        },
      });
      map.addLayer({
        id: "activity-zone",
        type: "circle",
        source: "activity",
        paint: {
          "circle-radius": [
            "interpolate",
            ["linear"],
            ["get", "activeRunners"],
            0,
            9,
            100,
            25,
          ],
          "circle-color": "#b7ef53",
          "circle-opacity": [
            "match",
            ["get", "activityLevel"],
            "high",
            0.9,
            "medium",
            0.7,
            0.5,
          ],
          "circle-stroke-width": 2,
          "circle-stroke-color": "#344128",
        },
      });
      map.on("click", "activity-zone", (e) => {
        const id: unknown = e.features?.[0]?.properties?.id;
        const zone = latest.current.zones.find((z) => z.id === id);
        if (zone) latest.current.onSelect?.(zone);
      });
      map.on("mouseenter", "activity-zone", () => {
        map.getCanvas().style.cursor = "pointer";
      });
      map.on("mouseleave", "activity-zone", () => {
        map.getCanvas().style.cursor = "";
      });
    });
    return () => {
      clearTimeout(timeout);
      map.remove();
      mapRef.current = null;
    };
  }, []);
  return (
    <div className="map-frame" data-ready={ready}>
      <div
        ref={container}
        className="map-canvas"
        aria-label="Aggregated Kraków activity zones"
      />
      {!ready && !failed && (
        <span className="map-loading" role="status">
          Loading city map…
        </span>
      )}
      {failed && (
        <div className="map-fallback">
          <span className="eyebrow">KRAKÓW · ACTIVITY ZONES</span>
          <strong>The city map is taking a break.</strong>
          <span>Your run and goals are still ready.</span>
        </div>
      )}
      <div className="map-credit">
        <a href="https://openfreemap.org/" target="_blank" rel="noreferrer">
          OpenFreeMap
        </a>{" "}
        ·{" "}
        <a
          href="https://www.openstreetmap.org/copyright"
          target="_blank"
          rel="noreferrer"
        >
          © OpenStreetMap
        </a>{" "}
        ·{" "}
        <a href="https://maplibre.org/" target="_blank" rel="noreferrer">
          MapLibre
        </a>
      </div>
    </div>
  );
}
