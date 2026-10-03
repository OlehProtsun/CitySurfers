import { useQuery } from "@tanstack/react-query";
import { api } from "./citySurfersApi";
import type { MapPeriod } from "./types";
export const keys = {
  home: ["home"],
  active: ["activeRun"],
  run: (id: string) => ["run", id],
  history: ["history", 10],
  progress: ["progress"],
  leaderboard: (period: string) => ["leaderboard", period],
  rival: ["rival"],
  goal: ["goal"],
  map: (period: MapPeriod) => ["activityMap", period],
};
export const useHome = () =>
  useQuery({ queryKey: keys.home, queryFn: ({ signal }) => api.home(signal) });
export const useActivity = (period: MapPeriod) =>
  useQuery({
    queryKey: keys.map(period),
    queryFn: ({ signal }) => api.map(period, signal),
  });
