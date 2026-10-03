using CitySurfers.Domain.Running;

namespace CitySurfers.Application.Running;

public sealed record RunTarget(string OpponentKey, string OpponentDisplayName, double DistanceThresholdMeters, int Points);
public sealed record TargetSnapshot(string Opponent, double DistanceToOvertakeMeters, int PotentialPoints);
public sealed record CompetitionSnapshot(int Rank, int SeasonPointsEarned, TargetSnapshot? CurrentTarget);

public interface IRunCompetitionProvider
{
    int StartingRank { get; }
    IReadOnlyList<Overtake> Evaluate(RunSession run, DateTime now);
    CompetitionSnapshot GetSnapshot(RunSession run);
}
