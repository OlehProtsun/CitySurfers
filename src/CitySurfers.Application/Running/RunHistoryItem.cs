namespace CitySurfers.Application.Running;

public sealed record RunHistoryItem(string Id, DateTimeOffset StartedAtUtc, DateTimeOffset? FinishedAtUtc,
    double DistanceMeters, double DurationSeconds, double? AveragePaceSecondsPerKm,
    int OvertakesCount, int PointsEarned);
