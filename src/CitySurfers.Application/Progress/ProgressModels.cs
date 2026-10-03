namespace CitySurfers.Application.Progress;

public sealed record LifetimeProgress(int CompletedRuns, double TotalDistanceMeters,
    double TotalDurationSeconds, double? AveragePaceSecondsPerKm, long TotalOvertakes,
    long TotalPointsEarned, double LongestRunDistanceMeters, double? FastestRunAveragePaceSecondsPerKm);

public sealed record PeriodProgress(int CompletedRuns, double DistanceMeters, double DurationSeconds,
    double? AveragePaceSecondsPerKm, long PointsEarned);

public sealed record ProgressComparison(double MonthlyDistanceDeltaMeters, double? MonthlyAveragePaceDeltaSecondsPerKm);

public sealed record PersonalProgress(LifetimeProgress Lifetime, PeriodProgress CurrentWeek,
    PeriodProgress CurrentMonth, PeriodProgress PreviousMonth, ProgressComparison Comparison);
