using CitySurfers.Application.Goals;
using CitySurfers.Application.Home;
using CitySurfers.Application.Leaderboards;
using CitySurfers.Application.Periods;
using CitySurfers.Domain.Running;
using CitySurfers.Infrastructure.Running;

namespace CitySurfers.UnitTests;

public sealed class HomeServiceTests
{
    [Fact]
    public async Task IdleHomeReusesRankAndMonthlyGoal()
    {
        var data = new MotivationData();
        var home = await Service(data).GetAsync();
        Assert.Equal(data.User, home.Today);
        Assert.Null(home.ActiveRun);
        Assert.Equal("rival_points", home.NextGoal!.Type);
        Assert.Equal(14, home.NextGoal.RemainingPoints);
    }

    [Fact]
    public async Task ActiveHomePreservesStoredMetricsAndCompetitionGoal()
    {
        var run = new RunSession("run", "owner", 41, DateTime.UtcNow);
        run.UpdateProgress(600, 180, 300, DateTime.UtcNow);
        var home = await Service(new MotivationData { Active = run }).GetAsync();
        Assert.Equal(new ActiveRunSummary(run.Id, run.StartedAtUtc, 600, 180, 300), home.ActiveRun);
        Assert.Equal("run_overtake", home.NextGoal!.Type);
        Assert.Equal(run.Id, home.NextGoal.RunId);
        Assert.Equal(600, home.NextGoal.RemainingDistanceMeters);
    }

    [Fact]
    public async Task NoRivalProducesNormalNullGoal()
    {
        var home = await Service(new MotivationData { User = new(1, 4000) }).GetAsync();
        Assert.Null(home.ActiveRun);
        Assert.Null(home.NextGoal);
    }

    private static HomeService Service(MotivationData data)
    {
        var board = new LeaderboardService(data, data, data, new TodayProvider(data), new KrakowPeriodResolver(), TimeProvider.System);
        return new HomeService(data, data, board, new NextGoalService(data, data,
            new DemoRunCompetitionProvider(), data.Rivals()));
    }
    private sealed class TodayProvider(MotivationData data) : ILeaderboardProvider
    {
        public Task<LeaderboardStandings> GetAsync(LeaderboardPeriod period, long currentUserPoints,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(LeaderboardPeriod.Today, period);
            return Task.FromResult(new LeaderboardStandings(data.User, data.Top, data.AroundMe));
        }
    }
}
