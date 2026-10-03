import { lazy, Suspense } from "react";
import { motion } from "motion/react";
import { ArrowUpRight, Flag, Pause, Play } from "lucide-react";
import { useHome, useActivity } from "../api/queries";
import { useRun } from "../features/run/context";
import {
  Button,
  Card,
  ErrorState,
  GoalCard,
  Loading,
  Metric,
} from "../components/ui";
import { distance, duration, pace } from "../lib/format";
import { env } from "../lib/env";
const ActivityMap = lazy(() => import("../features/map/ActivityMap"));
export default function HomePage() {
  const home = useHome();
  const activity = useActivity("live");
  const state = useRun();
  if (!home.data)
    return home.isError ? (
      <ErrorState retry={() => void home.refetch()} />
    ) : (
      <Loading />
    );
  const run = state.run;
  const summary = state.summary;
  if (summary)
    return (
      <div className="page-stack summary">
        <span className="eyebrow accent">YOU SHOWED UP. YOU MOVED UP.</span>
        <h1>
          Run complete<span className="accent">.</span>
        </h1>
        <Card className="summary-hero">
          <Flag size={30} />
          <strong>{distance(summary.distanceMeters)}</strong>
          <span>
            {duration(summary.durationSeconds)} ·{" "}
            {pace(summary.averagePaceSecondsPerKm)}
          </span>
          <div className="rank-change">
            #{summary.rankBefore} <span>→</span> #{summary.rankAfter}
          </div>
          <p>+{summary.seasonPointsEarned} season points</p>
        </Card>
        <Card>
          <span className="eyebrow">{summary.overtakesCount} OVERTAKES</span>
          {summary.overtakes.map((o) => (
            <div className="list-row" key={o.opponentKey}>
              <span>{o.opponent}</span>
              <strong>+{o.pointsAwarded} pts</strong>
            </div>
          ))}
        </Card>
        {home.isError ? (
          <ErrorState retry={() => void home.refetch()} />
        ) : (
          <GoalCard goal={home.data.nextGoal} />
        )}
        <Button onClick={state.dismissSummary}>
          Back to City
          <ArrowUpRight size={20} />
        </Button>
      </div>
    );
  return (
    <div className={`page-stack ${run ? "live-page" : ""}`}>
      <div className="page-title">
        <div>
          <span className="eyebrow">YOUR CITY IS YOUR PLAYGROUND</span>
          <h1>
            Kraków<span className="accent">.</span>
          </h1>
        </div>
        <div className="rank-chip">
          <span>{run ? "This run" : "Today"}</span>
          <strong>#{run?.competition.rank ?? home.data.today.rank}</strong>
          <span>
            {run?.competition.seasonPointsEarned ?? home.data.today.points} pts
          </span>
        </div>
      </div>
      {run && (
        <div className="metrics">
          <Metric label="Distance" value={distance(run.distanceMeters)} />
          <Metric label="Time" value={duration(run.durationSeconds)} />
          <Metric label="Pace" value={pace(run.averagePaceSecondsPerKm)} />
        </div>
      )}
      <div className="city-map">
        <Suspense fallback={<div className="map-placeholder" />}>
          <ActivityMap zones={activity.data?.zones ?? []} />
        </Suspense>
        <div className="map-label">
          <span className="live-dot" />{" "}
          {run ? "YOUR NEXT OVERTAKE STARTS HERE" : "A CITY IN MOTION"}
        </div>
      </div>
      {activity.isError && (
        <button className="text-button" onClick={() => void activity.refetch()}>
          Activity unavailable · Retry
        </button>
      )}
      {state.error ? (
        <ErrorState
          retry={state.recover}
          message="Run paused. Let’s reconnect."
        />
      ) : null}
      {run ? (
        <>
          <motion.section
            key={run.competition.currentTarget?.opponent ?? "clear"}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="card live-target"
          >
            <div className="flex justify-between">
              <span className="eyebrow">CLOSEST TARGET</span>
              {env.simulation && <span className="demo-pill">DEMO RUN</span>}
            </div>
            {run.competition.currentTarget ? (
              <>
                <h2>{run.competition.currentTarget.opponent}</h2>
                <div className="target-distance">
                  {Math.ceil(
                    run.competition.currentTarget.distanceToOvertakeMeters,
                  )}
                  <span> m</span>
                </div>
                <p>to your next overtake</p>
                <div className="target-footer">
                  <span>Current rank #{run.competition.rank}</span>
                  <strong>
                    +{run.competition.currentTarget.potentialPoints} pts
                  </strong>
                </div>
              </>
            ) : (
              <>
                <h2>Clear road ahead.</h2>
                <p>Every target passed. Finish strong.</p>
                <div className="target-footer">
                  <span>Rank #{run.competition.rank}</span>
                  <strong>
                    {run.competition.seasonPointsEarned} pts earned
                  </strong>
                </div>
              </>
            )}
          </motion.section>
          <div className="run-controls">
            {env.simulation && (
              <Button
                secondary
                disabled={
                  state.busy || !!state.error || run.distanceMeters >= 6800
                }
                onClick={state.toggle}
              >
                {state.running ? <Pause size={18} /> : <Play size={18} />}
                {state.running ? "Pause demo" : "Resume demo"}
              </Button>
            )}
            <Button
              disabled={state.busy || !!state.error || state.events.length > 0}
              onClick={state.finish}
            >
              <Flag size={18} />
              {state.busy ? "Saving…" : "Finish run"}
            </Button>
          </div>
        </>
      ) : home.data.activeRun ? (
        <Loading />
      ) : (
        <>
          <GoalCard goal={home.data.nextGoal} />
          <Button disabled={state.busy || !!state.error} onClick={state.start}>
            {state.busy ? "Starting…" : "Start run"}
            <ArrowUpRight size={21} />
          </Button>
          <p className="footnote">Short goals. Real momentum.</p>
        </>
      )}
    </div>
  );
}
