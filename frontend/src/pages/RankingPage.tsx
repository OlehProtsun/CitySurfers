import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "../api/citySurfersApi";
import { keys } from "../api/queries";
import { Card, Empty, ErrorState, Loading, Segments } from "../components/ui";
import type { LeaderboardRow } from "../api/types";
export function RankingRows({ rows }: { rows: LeaderboardRow[] }) {
  return (
    <div>
      {rows.map((row) => (
        <div
          key={`${row.rank}-${row.displayName}`}
          className={`ranking-row ${row.isCurrentUser ? "current-user" : ""}`}
          aria-label={row.isCurrentUser ? "Your ranking" : undefined}
        >
          <span className={row.rank <= 3 ? "medal" : "position"}>
            #{row.rank}
          </span>
          <span className="avatar">
            {row.displayName.slice(0, 2).toUpperCase()}
          </span>
          <strong>
            {row.displayName}
            {row.isCurrentUser && <small>YOU</small>}
          </strong>
          <span>
            {row.points}
            <small>pts</small>
          </span>
        </div>
      ))}
    </div>
  );
}
export default function RankingPage() {
  const [period, setPeriod] = useState<"today" | "month">("today");
  const board = useQuery({
    queryKey: keys.leaderboard(period),
    queryFn: ({ signal }) => api.leaderboard(period, signal),
  });
  const rival = useQuery({
    queryKey: keys.rival,
    queryFn: ({ signal }) => api.rival(signal),
    enabled: period === "month",
  });
  const top = board.data?.top ?? [];
  const around =
    board.data?.aroundMe.filter(
      (row) =>
        !top.some(
          (t) => t.rank === row.rank && t.displayName === row.displayName,
        ),
    ) ?? [];
  return (
    <div className="page-stack">
      <div>
        <span className="eyebrow">ONE CITY. EVERY STEP COUNTS.</span>
        <h1>
          The climb<span className="accent">.</span>
        </h1>
      </div>
      <Segments
        options={["today", "month"]}
        value={period}
        onChange={setPeriod}
        label="Ranking period"
      />
      {board.isError ? (
        <ErrorState retry={() => void board.refetch()} />
      ) : !board.data ? (
        <Loading />
      ) : (
        <>
          <Card className="ranking-hero">
            <span className="eyebrow">
              YOUR {period.toUpperCase()} POSITION
            </span>
            <div>
              <strong>#{board.data.currentUser.rank}</strong>
              <span>{board.data.currentUser.points} pts</span>
            </div>
            <p>Your next place is closer than you think.</p>
          </Card>
          {period === "month" &&
            (rival.isError ? (
              <ErrorState retry={() => void rival.refetch()} />
            ) : !rival.data ? (
              <Loading />
            ) : (
              <Card className="rival">
                <span className="eyebrow">YOUR RIVAL</span>
                {rival.data.rival ? (
                  <>
                    <h2>{rival.data.rival.displayName}</h2>
                    <p>
                      #{rival.data.rival.rank} · {rival.data.rival.points} pts
                    </p>
                    <strong>{rival.data.rival.pointsToPass} pts to pass</strong>
                  </>
                ) : (
                  <p>You’re leading the monthly board.</p>
                )}
              </Card>
            ))}
          <section>
            <h2 className="section-title">City leaders</h2>
            {top.length ? (
              <RankingRows rows={top} />
            ) : (
              <Empty title="The board is open">
                Start a run and make your move.
              </Empty>
            )}
          </section>
          {around.length > 0 && (
            <section>
              <h2 className="section-title">Around you</h2>
              <RankingRows rows={around} />
            </section>
          )}
        </>
      )}
    </div>
  );
}
