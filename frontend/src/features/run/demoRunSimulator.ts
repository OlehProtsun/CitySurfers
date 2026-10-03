import type { RunProgressRequest } from "../../api/types";
const distances = [600, 1200, 1800, 2500, 3200, 4000, 4700, 5500, 6200, 6800];
export const checkpoints: readonly RunProgressRequest[] = distances.map(
  (distanceMeters) => ({
    distanceMeters,
    durationSeconds: distanceMeters === 6800 ? 2210 : distanceMeters * 0.3,
  }),
);
export function nextCheckpoint(current: RunProgressRequest) {
  return checkpoints.find(
    (p) =>
      p.distanceMeters > current.distanceMeters &&
      p.durationSeconds >= current.durationSeconds,
  );
}
/** A single writer shared by start, progress, finish and recovery. */
export class RunWriteGate {
  private pending = false;
  get busy() {
    return this.pending;
  }
  async execute<T>(operation: () => Promise<T>): Promise<T> {
    if (this.pending) throw new Error("A run operation is already pending");
    this.pending = true;
    try {
      return await operation();
    } finally {
      this.pending = false;
    }
  }
}
