using CitySurfers.Application.Periods;
using CitySurfers.Application.Running;
using CitySurfers.Application.Users;

namespace CitySurfers.Application.Leaderboards;

public sealed class LeaderboardService(ICurrentUserAccessor currentUser, IRunHistoryReader history,
    IRunSessionStore store, ILeaderboardProvider provider, KrakowPeriodResolver periods, TimeProvider time)
{
    public async Task<LeaderboardResponse> GetAsync(LeaderboardPeriod period,
        CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        var range = period switch
        {
            LeaderboardPeriod.Today => periods.GetTodayUtcRange(now),
            LeaderboardPeriod.Month => periods.GetCurrentMonthUtcRange(now),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
        var userId = await currentUser.GetUserIdAsync(cancellationToken);
        // Read active first: if it finishes before history is read, its completed row wins.
        var active = await store.GetActiveByUserIdAsync(userId, cancellationToken);
        var completed = await history.GetCompletedAsync(userId, range.StartUtc, range.EndUtc,
            cancellationToken: cancellationToken);
        var points = completed.Sum(run => (long)run.PointsEarned);
        if (active is not null && range.Contains(active.StartedAtUtc)
            && !completed.Any(run => run.Id == active.Id))
            points += active.SeasonPointsEarned;
        var standings = await provider.GetAsync(period, points, cancellationToken);
        return new LeaderboardResponse(period == LeaderboardPeriod.Today ? "today" : "month",
            range.StartUtc, range.EndUtc, standings.CurrentUser, standings.Top, standings.AroundMe);
    }
}
