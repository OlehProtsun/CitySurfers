using CitySurfers.Application.Goals;
using CitySurfers.Application.Running;
using CitySurfers.Domain.Running;

namespace CitySurfers.UnitTests;

public sealed class NextGoalServiceTests
{
    [Fact]
    public async Task RunTargetWinsAndUsesExactSnapshotWithoutReadingLeaderboard()
    {
        var data = new MotivationData { Active = Run(), FailLeaderboard = true };
        var provider = new SnapshotProvider(new(23, 10, new("Custom target", 321.5, 14)));
        var goal = (await Service(data, provider).GetAsync()).Goal!;
        Assert.Equal("run_overtake", goal.Type);
        Assert.Equal("active_run", goal.Source);
        Assert.Equal("Custom target", goal.TargetDisplayName);
        Assert.Equal("run", goal.RunId);
        Assert.Equal(321.5, goal.RemainingDistanceMeters);
        Assert.Equal(14, goal.PotentialPoints);
        Assert.Equal(23, goal.CurrentRank);
        Assert.Equal(22, goal.TargetRank);
        Assert.Null(goal.RemainingPoints);
        Assert.Same(data.Active, provider.ReadRun);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IdleOrExhaustedRunFallsBackToRivalPointsToPass(bool active)
    {
        var data = new MotivationData { Active = active ? Run() : null };
        var provider = new SnapshotProvider(new(37, 59, null));
        var rival = (await data.Rivals().GetAsync()).Rival!;
        var goal = (await Service(data, provider).GetAsync()).Goal!;
        Assert.Equal("rival_points", goal.Type);
        Assert.Equal("monthly_leaderboard", goal.Source);
        Assert.Equal(rival.DisplayName, goal.TargetDisplayName);
        Assert.Equal(rival.PointsToPass, goal.RemainingPoints);
        Assert.Equal(18, goal.CurrentRank);
        Assert.Equal(rival.Rank, goal.TargetRank);
        Assert.Null(goal.RunId);
        Assert.Null(goal.RemainingDistanceMeters);
        Assert.Null(goal.PotentialPoints);
        if (!active) Assert.Null(provider.ReadRun);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoTargetAndNoRivalReturnsNull(bool active)
    {
        var data = new MotivationData { User = new(1, 4000), Active = active ? Run() : null };
        Assert.Null((await Service(data, new SnapshotProvider(new(37, 59, null))).GetAsync()).Goal);
    }

    private static RunSession Run() => new("run", "owner", 41, DateTime.UtcNow);
    private static NextGoalService Service(MotivationData data, IRunCompetitionProvider provider) =>
        new(data, data, provider, data.Rivals());

    private sealed class SnapshotProvider(CompetitionSnapshot snapshot) : IRunCompetitionProvider
    {
        public RunSession? ReadRun { get; private set; }
        public int StartingRank => 41;
        public IReadOnlyList<Overtake> Evaluate(RunSession run, DateTime now) => throw new NotSupportedException();
        public CompetitionSnapshot GetSnapshot(RunSession run)
        {
            ReadRun = run;
            return snapshot;
        }
    }
}
