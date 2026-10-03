import type { LoginResponse } from "../api/types";
const key = "citysurfers.demoIdentity";
export function readIdentity(): LoginResponse | null {
  try {
    const v: unknown = JSON.parse(sessionStorage.getItem(key) || "null");
    if (
      v &&
      typeof v === "object" &&
      "id" in v &&
      typeof v.id === "string" &&
      "username" in v &&
      typeof v.username === "string" &&
      "displayName" in v &&
      typeof v.displayName === "string"
    )
      return { id: v.id, username: v.username, displayName: v.displayName };
  } catch {
    /* Storage is optional for the demo UX gate. */
  }
  return null;
}
export function saveIdentity(identity: LoginResponse) {
  try {
    sessionStorage.setItem(key, JSON.stringify(identity));
  } catch {
    /* In-memory session remains available. */
  }
}
