namespace CitySurfers.Application.Goals;

public sealed record NextGoal(string Type, string Source, string TargetDisplayName, string? RunId,
    double? RemainingDistanceMeters, long? RemainingPoints, int? PotentialPoints, int? CurrentRank, int? TargetRank);
public sealed record NextGoalResponse(NextGoal? Goal);
