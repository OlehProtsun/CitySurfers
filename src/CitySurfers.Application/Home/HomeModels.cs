using CitySurfers.Application.Goals;
using CitySurfers.Application.Leaderboards;

namespace CitySurfers.Application.Home;

public sealed record HomeResponse(CurrentUserRank Today, ActiveRunSummary? ActiveRun, NextGoal? NextGoal);

public sealed record ActiveRunSummary(string Id, DateTime StartedAtUtc, double DistanceMeters,
    double DurationSeconds, double? AveragePaceSecondsPerKm);
