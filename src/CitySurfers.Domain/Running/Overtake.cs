namespace CitySurfers.Domain.Running;

public sealed record Overtake(string OpponentKey, string OpponentDisplayName, DateTime CompletedAtUtc,
    double DistanceThresholdMeters, int PointsAwarded, int RankBefore, int RankAfter);

public enum RunStatus { Active, Completed }
