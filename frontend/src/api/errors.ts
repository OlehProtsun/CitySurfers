import type { ProblemDetails } from "./types";
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;
  constructor(status: number, problem: ProblemDetails = {}) {
    super(problem.title || "Request failed");
    this.status = typeof problem.status === "number" ? problem.status : status;
    this.problem = problem;
  }
}
export function parseProblem(value: unknown): ProblemDetails {
  if (!value || typeof value !== "object") return {};
  const p = value as Record<string, unknown>;
  return {
    status: typeof p.status === "number" ? p.status : undefined,
    title: typeof p.title === "string" ? p.title : undefined,
    detail: typeof p.detail === "string" ? p.detail : undefined,
    traceId: typeof p.traceId === "string" ? p.traceId : undefined,
    errors:
      p.errors && typeof p.errors === "object"
        ? Object.fromEntries(
            Object.entries(p.errors).filter(
              ([, v]) =>
                Array.isArray(v) && v.every((x) => typeof x === "string"),
            ),
          )
        : undefined,
  };
}
export function errorMessage(error: unknown) {
  if (error instanceof ApiError && error.status === 401)
    return "Invalid demo credentials.";
  if (error instanceof ApiError && error.status === 400)
    return "Please check your details and try again.";
  if (error instanceof ApiError && error.status === 409)
    return "Your run changed. Recover the latest state to continue.";
  return "Connection interrupted. Please try again.";
}
