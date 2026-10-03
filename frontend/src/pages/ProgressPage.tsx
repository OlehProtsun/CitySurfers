import { useQuery } from "@tanstack/react-query";
import {
  Area,
  AreaChart,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { api } from "../api/citySurfersApi";
import { keys } from "../api/queries";
import { Card, Empty, ErrorState, Loading, Metric } from "../components/ui";
import { date, distance, duration, pace, paceComparison } from "../lib/format";
export default function ProgressPage() {
  const progress = useQuery({
    queryKey: keys.progress,
    queryFn: ({ signal }) => api.progress(signal),
  });
  const history = useQuery({
    queryKey: keys.history,
    queryFn: ({ signal }) => api.history(signal),
  });
  if (progress.isError || history.isError)
    return (
      <ErrorState
        retry={() => {
          void progress.refetch();
          void history.refetch();
        }}
      />
    );
  if (!progress.data || !history.data) return <Loading />;
  const { lifetime: l, currentWeek, currentMonth, comparison } = progress.data;
  const chart = [...history.data.items].reverse().map((r) => ({
    date: date(r.startedAtUtc),
    km: Number((r.distanceMeters / 1000).toFixed(2)),
    pace: r.averagePaceSecondsPerKm,
  }));
  return (
    <div className="page-stack">
      <div>
        <span className="eyebrow">A LITTLE FURTHER. A LITTLE STRONGER.</span>
        <h1>
          Your momentum<span className="accent">.</span>
        </h1>
      </div>
      <Card className="progress-hero">
        <span className="eyebrow">LIFETIME DISTANCE</span>
        <strong>{distance(l.totalDistanceMeters)}</strong>
        <p>
          {l.completedRuns} completed runs · {l.totalOvertakes} overtakes
        </p>
      </Card>
      <Card>
        <div className="stats-grid">
          <Metric
            label="Time moving"
            value={duration(l.totalDurationSeconds)}
          />
          <Metric
            label="Average pace"
            value={pace(l.averagePaceSecondsPerKm)}
          />
          <Metric label="Points earned" value={l.totalPointsEarned} />
          <Metric
            label="Longest run"
            value={distance(l.longestRunDistanceMeters)}
          />
          <Metric
            label="Fastest run pace"
            value={pace(l.fastestRunAveragePaceSecondsPerKm)}
          />
        </div>
      </Card>
      <div className="period-grid">
        {[
          { label: "This week", aggregate: currentWeek },
          { label: "This month", aggregate: currentMonth },
        ].map(({ label, aggregate }) => (
          <Card key={label}>
            <span className="eyebrow">{label}</span>
            <h2>{distance(aggregate.distanceMeters)}</h2>
            <p>
              {aggregate.completedRuns} runs · {aggregate.pointsEarned} pts
            </p>
            <small>
              {duration(aggregate.durationSeconds)} ·{" "}
              {pace(aggregate.averagePaceSecondsPerKm)}
            </small>
          </Card>
        ))}{" "}
      </div>
      <Card>
        <span className="eyebrow">MONTH OVER MONTH</span>
        <h2>
          {comparison.monthlyDistanceDeltaMeters >= 0 ? "+" : "−"}
          {distance(Math.abs(comparison.monthlyDistanceDeltaMeters))}
        </h2>
        <p>{paceComparison(comparison.monthlyAveragePaceDeltaSecondsPerKm)}</p>
      </Card>
      {!chart.length ? (
        <Empty title="Your first chapter starts outside">
          Finish a demo run to see your progress here.
        </Empty>
      ) : (
        <>
          <Card>
            <h2 className="section-title">
              Recent distance <small>km</small>
            </h2>
            <div
              className="chart"
              role="img"
              aria-label="Recent run distance chart in kilometres"
            >
              <ResponsiveContainer width="100%" height={180}>
                <AreaChart data={chart}>
                  <XAxis
                    dataKey="date"
                    tick={{ fill: "#a7afa0", fontSize: 11 }}
                  />
                  <YAxis width={30} tick={{ fill: "#a7afa0", fontSize: 11 }} />
                  <Tooltip
                    contentStyle={{
                      background: "#22291e",
                      border: "1px solid #57634d",
                      borderRadius: 12,
                    }}
                  />
                  <Area
                    type="monotone"
                    dataKey="km"
                    stroke="#b7ef53"
                    fill="#b7ef53"
                    fillOpacity={0.15}
                    isAnimationActive={false}
                  />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          </Card>
          <Card>
            <h2 className="section-title">Recent pace</h2>
            <p className="footnote">Seconds per kilometre · lower is faster</p>
            <div
              className="chart"
              role="img"
              aria-label="Recent pace chart, lower seconds per kilometre is faster"
            >
              <ResponsiveContainer width="100%" height={180}>
                <LineChart data={chart}>
                  <XAxis
                    dataKey="date"
                    tick={{ fill: "#a7afa0", fontSize: 11 }}
                  />
                  <YAxis width={35} tick={{ fill: "#a7afa0", fontSize: 11 }} />
                  <Tooltip
                    contentStyle={{
                      background: "#22291e",
                      border: "1px solid #57634d",
                      borderRadius: 12,
                    }}
                  />
                  <Line
                    dataKey="pace"
                    stroke="#84d5db"
                    strokeWidth={3}
                    connectNulls={false}
                    isAnimationActive={false}
                  />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </Card>
          <section>
            <h2 className="section-title">Recent runs</h2>
            {history.data.items.map((r) => (
              <Card key={r.id} className="history-card">
                <div className="flex justify-between">
                  <strong>{date(r.startedAtUtc)}</strong>
                  <span className="accent">+{r.pointsEarned} pts</span>
                </div>
                <h2>{distance(r.distanceMeters)}</h2>
                <p>
                  {duration(r.durationSeconds)} ·{" "}
                  {pace(r.averagePaceSecondsPerKm)} · {r.overtakesCount}{" "}
                  overtakes
                </p>
              </Card>
            ))}
          </section>
        </>
      )}
    </div>
  );
}
