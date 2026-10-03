using CitySurfers.Domain.Running;
using CitySurfers.Infrastructure.Running;

namespace CitySurfers.UnitTests;

public sealed class RunCompetitionTests
{
    [Theory]
    [InlineData(1010, 0, 41, 0, "Runner_92", 190)]
    [InlineData(1200, 1, 40, 16, "Marta", 1300)]
    [InlineData(2800, 2, 39, 30, "Runner_17", 1200)]
    [InlineData(6800, 4, 37, 59, null, 0)]
    public void Evaluates_targets_once_and_selects_next(double distance, int count, int rank, int points,
        string? next, double remaining)
    {
        var provider = new DemoRunCompetitionProvider();
        var run = new RunSession("id", "user", provider.StartingRank, DateTime.UtcNow);
        run.UpdateProgress(distance, 1000, null, DateTime.UtcNow);
        var events = provider.Evaluate(run, DateTime.UtcNow);
        Assert.Equal(count, events.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(41 - i, events[i].RankBefore);
            Assert.Equal(40 - i, events[i].RankAfter);
        }
        Assert.Empty(provider.Evaluate(run, DateTime.UtcNow));
        var snapshot = provider.GetSnapshot(run);
        Assert.Equal(rank, snapshot.Rank);
        Assert.Equal(points, snapshot.SeasonPointsEarned);
        Assert.Equal(next, snapshot.CurrentTarget?.Opponent);
        Assert.Equal(remaining, snapshot.CurrentTarget?.DistanceToOvertakeMeters ?? 0);
        Assert.Equal(count, run.Overtakes.Count);
    }

    [Fact]
    public void Snapshot_never_has_negative_distance()
    {
        var run = new RunSession("id", "user", 41, DateTime.UtcNow);
        run.UpdateProgress(2000, 600, 300, DateTime.UtcNow);
        Assert.Equal(0, new DemoRunCompetitionProvider().GetSnapshot(run).CurrentTarget!.DistanceToOvertakeMeters);
    }
}
