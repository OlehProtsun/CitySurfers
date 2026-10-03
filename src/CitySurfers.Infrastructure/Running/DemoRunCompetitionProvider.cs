using CitySurfers.Application.Running;
using CitySurfers.Domain.Running;

namespace CitySurfers.Infrastructure.Running;

public sealed class DemoRunCompetitionProvider : IRunCompetitionProvider
{
    private static readonly RunTarget[] Targets =
    [
        new("runner-92", "Runner_92", 1200, 16),
        new("marta", "Marta", 2500, 14),
        new("runner-17", "Runner_17", 4000, 11),
        new("kamil-24", "Kamil_24", 5500, 18)
    ];

    public int StartingRank => 41;

    public IReadOnlyList<Overtake> Evaluate(RunSession run, DateTime now)
    {
        var events = new List<Overtake>();
        foreach (var target in Targets)
        {
            var overtake = run.TryOvertake(target.OpponentKey, target.OpponentDisplayName,
                target.DistanceThresholdMeters, target.Points, now);
            if (overtake is not null)
                events.Add(overtake);
        }
        return events;
    }

    public CompetitionSnapshot GetSnapshot(RunSession run)
    {
        var target = Targets.FirstOrDefault(target =>
            !run.Overtakes.Any(overtake => overtake.OpponentKey == target.OpponentKey));
        return new CompetitionSnapshot(run.CurrentRank, run.SeasonPointsEarned, target is null ? null :
            new TargetSnapshot(target.OpponentDisplayName,
                Math.Max(0, target.DistanceThresholdMeters - run.DistanceMeters), target.Points));
    }
}
