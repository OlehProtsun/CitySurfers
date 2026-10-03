using CitySurfers.Application.Leaderboards;
using CitySurfers.Application.Periods;
using CitySurfers.Application.Rivals;
using CitySurfers.Application.Running;
using CitySurfers.Application.Users;
using CitySurfers.Domain.Running;

namespace CitySurfers.UnitTests;

internal sealed class MotivationData : ICurrentUserAccessor, IRunSessionStore, IRunHistoryReader, ILeaderboardProvider
{
    public CurrentUserRank User { get; set; } = new(18, 1438);
    public IReadOnlyList<LeaderboardRow> AroundMe { get; set; } =
        [new(19, "Below", 1410, false), new(18, "You", 1438, true), new(17, "Marta", 1451, false)];
    public IReadOnlyList<LeaderboardRow> Top { get; set; } = [];
    public RunSession? Active { get; set; }
    public bool FailLeaderboard { get; set; }
    public RivalService Rivals() => new(new LeaderboardService(this, this, this, this,
        new KrakowPeriodResolver(), new FixedTime()));

    public Task<string> GetUserIdAsync(CancellationToken cancellationToken = default) => Task.FromResult("owner");
    public Task<RunSession?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        Assert.Equal("owner", userId);
        return Task.FromResult(Active);
    }
    public Task<IReadOnlyList<RunHistoryItem>> GetCompletedAsync(string userId, DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null, int? limit = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RunHistoryItem>>([]);
    public Task<LeaderboardStandings> GetAsync(LeaderboardPeriod period, long currentUserPoints,
        CancellationToken cancellationToken = default)
    {
        Assert.Equal(LeaderboardPeriod.Month, period);
        if (FailLeaderboard) throw new InvalidOperationException("Leaderboard must not be read for a run goal.");
        return Task.FromResult(new LeaderboardStandings(User, Top, AroundMe));
    }
    public Task<RunSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task AddAsync(RunSession run, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task SaveAsync(RunSession run, double previousDistance, double previousDuration,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();
    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-10-03T12:00:00Z");
    }
}
