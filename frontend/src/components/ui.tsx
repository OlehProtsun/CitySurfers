import type { ButtonHTMLAttributes, ReactNode } from "react";
import { RotateCcw, ArrowUpRight } from "lucide-react";
import type { NextGoal } from "../api/types";
export function Button({
  secondary,
  className = "",
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { secondary?: boolean }) {
  return (
    <button
      className={`button ${secondary ? "secondary" : ""} ${className}`}
      {...props}
    />
  );
}
export function Card({
  children,
  className = "",
}: {
  children: ReactNode;
  className?: string;
}) {
  return <section className={`card ${className}`}>{children}</section>;
}
export function Metric({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="metric">
      <strong>{value}</strong>
      <span>{label}</span>
    </div>
  );
}
export function Loading() {
  return (
    <div className="loading card" role="status">
      <span className="eyebrow">CONNECTING TO YOUR CITY</span>
      <div />
      <div />
      <span className="sr-only">Loading</span>
    </div>
  );
}
export function ErrorState({
  retry,
  message = "City is waking up…",
}: {
  retry: () => void;
  message?: string;
}) {
  return (
    <div className="card error" role="alert">
      <h2>{message}</h2>
      <p>Your progress stays with the city. Try connecting again.</p>
      <Button secondary onClick={retry}>
        <RotateCcw size={17} />
        Retry
      </Button>
    </div>
  );
}
export function Empty({
  title,
  children,
}: {
  title: string;
  children?: ReactNode;
}) {
  return (
    <Card>
      <h2>{title}</h2>
      <p>{children}</p>
    </Card>
  );
}
export function Segments<T extends string>({
  options,
  value,
  onChange,
  label,
}: {
  options: readonly T[];
  value: T;
  onChange: (value: T) => void;
  label: string;
}) {
  return (
    <div className="segments" role="group" aria-label={label}>
      {options.map((option) => (
        <button
          key={option}
          aria-pressed={value === option}
          onClick={() => onChange(option)}
        >
          {option}
        </button>
      ))}
    </div>
  );
}
export function GoalCard({ goal }: { goal: NextGoal | null }) {
  return (
    <Card className="goal">
      <div className="flex items-center justify-between">
        <span className="eyebrow">
          {goal?.type === "run_overtake" ? "NEXT OVERTAKE" : "YOUR NEXT MOVE"}
        </span>
        <ArrowUpRight size={22} />
      </div>
      {goal ? (
        <>
          <h2>{goal.targetDisplayName}</h2>
          <p className="goal-gap">
            {goal.type === "run_overtake"
              ? `${Math.ceil(goal.remainingDistanceMeters ?? 0)} m to overtake`
              : `${goal.remainingPoints} pts to pass`}
          </p>
          <div className="flex justify-between">
            <span>
              {goal.currentRank !== null && goal.targetRank !== null
                ? `#${goal.currentRank} → #${goal.targetRank}`
                : "Keep moving forward"}
            </span>
            {goal.potentialPoints !== null && (
              <span>+{goal.potentialPoints} pts</span>
            )}
          </div>
        </>
      ) : (
        <>
          <h2>You’re at the top.</h2>
          <p>Start a run to defend it.</p>
        </>
      )}
    </Card>
  );
}
