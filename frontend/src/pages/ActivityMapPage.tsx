import { lazy, Suspense, useState } from "react";
import { X } from "lucide-react";
import { useActivity } from "../api/queries";
import type { MapPeriod } from "../api/types";
import {
  Card,
  Empty,
  ErrorState,
  Loading,
  Metric,
  Segments,
} from "../components/ui";
import { pace } from "../lib/format";
const ActivityMap = lazy(() => import("../features/map/ActivityMap"));
export default function ActivityMapPage() {
  const [period, setPeriod] = useState<MapPeriod>("live");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const query = useActivity(period);
  const selected = query.data?.zones.find((z) => z.id === selectedId);
  return (
    <div className="page-stack">
      <div>
        <span className="eyebrow">FIND THE CITY’S ENERGY</span>
        <h1>
          In motion<span className="accent">.</span>
        </h1>
      </div>
      <Segments
        options={["live", "today", "month"]}
        value={period}
        onChange={(v) => {
          setPeriod(v);
          setSelectedId(null);
        }}
        label="Map period"
      />
      {query.isError ? (
        <ErrorState retry={() => void query.refetch()} />
      ) : !query.data ? (
        <Loading />
      ) : (
        <>
          <Suspense fallback={<Loading />}>
            <ActivityMap
              zones={query.data.zones}
              onSelect={(zone) => setSelectedId(zone.id)}
            />
          </Suspense>
          <p className="footnote">
            Aggregate demo activity · approximate area centres
            <br />
            No individual runner locations.
          </p>
          {selected && (
            <Card className="zone-detail">
              <div className="flex justify-between items-center">
                <span className="eyebrow">ACTIVITY ZONE</span>
                <button
                  className="icon-button"
                  aria-label="Close zone details"
                  onClick={() => setSelectedId(null)}
                >
                  <X size={20} />
                </button>
              </div>
              <h2>{selected.name}</h2>
              <div className="metrics">
                <Metric
                  label="Runners in area"
                  value={selected.activeRunners}
                />
                <Metric label="Runs" value={selected.runs} />
                <Metric
                  label="Average pace"
                  value={pace(selected.averagePaceSecondsPerKm)}
                />
              </div>
              <p>{selected.activityLevel} activity</p>
            </Card>
          )}
          <section>
            <h2 className="section-title">Explore activity zones</h2>
            {query.data.zones.length ? (
              query.data.zones.map((z) => (
                <button
                  className="zone-row"
                  key={z.id}
                  onClick={() => setSelectedId(z.id)}
                  aria-expanded={selectedId === z.id}
                >
                  <span className="zone-symbol" />
                  <span>
                    <strong>{z.name}</strong>
                    <small>
                      {z.activeRunners} runners in area · {z.runs} runs
                    </small>
                  </span>
                  <span>↗</span>
                </button>
              ))
            ) : (
              <Empty title="A quieter city">
                No activity zones for this period.
              </Empty>
            )}
          </section>
        </>
      )}
    </div>
  );
}
