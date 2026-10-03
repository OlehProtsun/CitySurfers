using CitySurfers.Application.Leaderboards;
using CitySurfers.Application.Periods;
using CitySurfers.Application.Running;
using CitySurfers.Application.Users;
using CitySurfers.Domain.Running;
using CitySurfers.Infrastructure.Leaderboards;

namespace CitySurfers.UnitTests;

public sealed class LeaderboardServiceTests
{
    [Theory]
    [InlineData(LeaderboardPeriod.Today, 16)]
    [InlineData(LeaderboardPeriod.Month, 46)]
    public async Task ScoresUseCompletedRunPeriodsAndCurrentUser(LeaderboardPeriod period, long expected)
    {
        var data = new Data
        {
            Runs = [Item("today", "2026-10-03T12:00:00Z", 16),
                Item("month", "2026-10-01T12:00:00Z", 30), Item("previous", "2026-09-30T21:59:59Z", 59)]
        };
        var result = await Service(data).GetAsync(period);
        Assert.Equal(expected, result.CurrentUser.Points);
        Assert.Equal(period == LeaderboardPeriod.Today ? "today" : "month", result.Period);
    }

    [Fact]
    public async Task ActiveThenCompletedTransitionDoesNotDoubleCountEvenDuringReads()
    {
        var data = new Data { Active = Active("run", "2026-10-03T12:00:00Z", 30) };
        Assert.Equal(30, (await Service(data).GetAsync(LeaderboardPeriod.Today)).CurrentUser.Points);
        data.Runs = [Item("run", "2026-10-03T12:00:00Z", 59)];
        // Simulates an active snapshot read immediately before the same run was completed.
        Assert.Equal(59, (await Service(data).GetAsync(LeaderboardPeriod.Today)).CurrentUser.Points);
        data.Active = null;
        Assert.Equal(59, (await Service(data).GetAsync(LeaderboardPeriod.Today)).CurrentUser.Points);
    }

    [Theory]
    [InlineData(LeaderboardPeriod.Today, "2026-10-02T12:00:00Z", 0)]
    [InlineData(LeaderboardPeriod.Month, "2026-10-02T12:00:00Z", 30)]
    [InlineData(LeaderboardPeriod.Month, "2026-09-30T21:59:59Z", 0)]
    [InlineData(LeaderboardPeriod.Today, "2026-10-02T22:00:00Z", 30)]
    [InlineData(LeaderboardPeriod.Today, "2026-10-03T22:00:00Z", 0)]
    public async Task ActiveScoreRequiresStartedDateInsideLocalPeriod(LeaderboardPeriod period, string start, long expected)
    {
        var data = new Data { Active = Active("run", start, 30) };
        Assert.Equal(expected, (await Service(data).GetAsync(period)).CurrentUser.Points);
    }

    [Fact]
    public async Task NoActivityIsValidZeroScore() =>
        Assert.Equal(0, (await Service(new Data()).GetAsync(LeaderboardPeriod.Today)).CurrentUser.Points);

    private static RunHistoryItem Item(string id, string start, int points) =>
        new(id, DateTimeOffset.Parse(start), DateTimeOffset.Parse(start).AddHours(1), 1000, 300, 300, 0, points);

    private static RunSession Active(string id, string start, int points) => RunSession.Restore(id, "owner", RunStatus.Active,
        DateTimeOffset.Parse(start).UtcDateTime, DateTimeOffset.Parse(start).UtcDateTime, null, 2800, 840, 300, 41, 39, points, []);

    private static LeaderboardService Service(Data data) =>
        new(data, data, data, new DemoLeaderboardProvider(), new KrakowPeriodResolver(), new FixedTime());

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-10-03T12:00:00Z");
    }

    private sealed class Data : ICurrentUserAccessor, IRunHistoryReader, IRunSessionStore
    {
        public IReadOnlyList<RunHistoryItem> Runs { get; set; } = [];
        public RunSession? Active { get; set; }
        public Task<string> GetUserIdAsync(CancellationToken cancellationToken = default) => Task.FromResult("owner");
        public Task<IReadOnlyList<RunHistoryItem>> GetCompletedAsync(string userId, DateTimeOffset? fromUtc = null,
            DateTimeOffset? toUtc = null, int? limit = null, CancellationToken cancellationToken = default)
        {
            Assert.Equal("owner", userId);
            Assert.NotNull(fromUtc);
            Assert.NotNull(toUtc);
            return Task.FromResult<IReadOnlyList<RunHistoryItem>>(Runs.Where(run => run.StartedAtUtc >= fromUtc
                && run.StartedAtUtc < toUtc).ToArray());
        }
        public Task<RunSession?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            Assert.Equal("owner", userId);
            return Task.FromResult(Active);
        }
        public Task<RunSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(RunSession run, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveAsync(RunSession run, double previousDistance, double previousDuration,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
