export interface LoginRequest {
  username: string;
  password: string;
}
export interface LoginResponse {
  id: string;
  username: string;
  displayName: string;
}
export interface CurrentUserRank {
  rank: number;
  points: number;
}
export interface RunProgressRequest {
  distanceMeters: number;
  durationSeconds: number;
}
export interface ActiveRunSummary extends RunProgressRequest {
  id: string;
  startedAtUtc: string;
  averagePaceSecondsPerKm: number | null;
}
export interface NextGoal {
  type: "run_overtake" | "rival_points";
  source: string;
  targetDisplayName: string;
  runId: string | null;
  remainingDistanceMeters: number | null;
  remainingPoints: number | null;
  potentialPoints: number | null;
  currentRank: number | null;
  targetRank: number | null;
}
export interface HomeResponse {
  today: CurrentUserRank;
  activeRun: ActiveRunSummary | null;
  nextGoal: NextGoal | null;
}
export interface CompetitionTarget {
  opponent: string;
  distanceToOvertakeMeters: number;
  potentialPoints: number;
}
export interface CompetitionSnapshot {
  rank: number;
  seasonPointsEarned: number;
  currentTarget: CompetitionTarget | null;
}
export interface OvertakeEvent {
  type: "OVERTAKE";
  opponent: string;
  rankBefore: number;
  rankAfter: number;
  pointsAwarded: number;
}
export interface ActiveRunResponse extends ActiveRunSummary {
  status: "active";
  updatedAtUtc: string;
  competition: CompetitionSnapshot;
  events: OvertakeEvent[];
}
export interface StoredOvertake extends Omit<OvertakeEvent, "type"> {
  opponentKey: string;
  completedAtUtc: string;
  distanceThresholdMeters: number;
}
export interface RunFinishResponse extends RunProgressRequest {
  runId: string;
  startedAtUtc: string;
  finishedAtUtc: string;
  averagePaceSecondsPerKm: number | null;
  overtakesCount: number;
  overtakes: StoredOvertake[];
  rankBefore: number;
  rankAfter: number;
  seasonPointsEarned: number;
  nextTarget: CompetitionTarget | null;
}
export type RunDetail = ActiveRunResponse | RunFinishResponse;
export interface RunHistoryItem extends ActiveRunSummary {
  finishedAtUtc: string;
  overtakesCount: number;
  pointsEarned: number;
}
export interface RunHistoryResponse {
  items: RunHistoryItem[];
}
export interface ProgressAggregate {
  completedRuns: number;
  distanceMeters: number;
  durationSeconds: number;
  averagePaceSecondsPerKm: number | null;
  pointsEarned: number;
}
export interface ProgressComparison {
  monthlyDistanceDeltaMeters: number;
  monthlyAveragePaceDeltaSecondsPerKm: number | null;
}
export interface ProgressResponse {
  lifetime: {
    completedRuns: number;
    totalDistanceMeters: number;
    totalDurationSeconds: number;
    averagePaceSecondsPerKm: number | null;
    totalOvertakes: number;
    totalPointsEarned: number;
    longestRunDistanceMeters: number;
    fastestRunAveragePaceSecondsPerKm: number | null;
  };
  currentWeek: ProgressAggregate;
  currentMonth: ProgressAggregate;
  previousMonth: ProgressAggregate;
  comparison: ProgressComparison;
}
export interface LeaderboardRow extends CurrentUserRank {
  displayName: string;
  isCurrentUser: boolean;
}
export interface LeaderboardResponse {
  period: string;
  periodStartUtc: string;
  periodEndUtc: string;
  currentUser: CurrentUserRank;
  top: LeaderboardRow[];
  aroundMe: LeaderboardRow[];
}
export interface RivalSnapshot extends CurrentUserRank {
  displayName: string;
  pointsGap: number;
  pointsToPass: number;
}
export interface RivalResponse {
  period: string;
  periodStartUtc: string;
  periodEndUtc: string;
  currentUser: CurrentUserRank;
  rival: RivalSnapshot | null;
}
export type MapPeriod = "live" | "today" | "month";
export interface ActivityZone {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  activeRunners: number;
  runs: number;
  averagePaceSecondsPerKm: number | null;
  activityLevel: string;
}
export interface ActivityMapResponse {
  period: MapPeriod;
  generatedAtUtc: string;
  zones: ActivityZone[];
}
export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
