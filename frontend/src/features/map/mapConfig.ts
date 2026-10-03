import type { FeatureCollection, Point } from "geojson";
import type {
  FilterSpecification,
  LngLatLike,
  TransformStyleFunction,
} from "maplibre-gl";
import type { ActivityZone } from "../../api/types";
import { env } from "../../lib/env";
export const mapConfig = {
  style: env.mapStyle,
  center: [19.945, 50.065] as LngLatLike,
  zoom: 11.6,
};
export function zoneGeoJson(zones: ActivityZone[]): FeatureCollection<Point> {
  return {
    type: "FeatureCollection",
    features: zones.map((z) => ({
      type: "Feature",
      id: z.id,
      geometry: { type: "Point", coordinates: [z.longitude, z.latitude] },
      properties: {
        id: z.id,
        activeRunners: z.activeRunners,
        activityLevel: z.activityLevel,
      },
    })),
  };
}
export const guardLibertyShields: TransformStyleFunction = (_, style) => ({
  ...style,
  layers: style.layers.map((layer) => {
    // Liberty's expression filters compare nullable ref_length with a number.
    if (
      [
        "highway-shield-non-us",
        "highway-shield-us-interstate",
        "road_shield_us",
      ].includes(layer.id) &&
      "filter" in layer &&
      layer.filter
    ) {
      return {
        ...layer,
        filter: [
          "all",
          ["has", "ref_length"],
          layer.filter,
        ] as FilterSpecification,
      };
    }
    return layer;
  }),
});
