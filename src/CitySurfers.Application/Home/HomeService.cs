using CitySurfers.Application.Goals;
using CitySurfers.Application.Leaderboards;
using CitySurfers.Application.Running;
using CitySurfers.Application.Users;

namespace CitySurfers.Application.Home;

public sealed class HomeService(ICurrentUserAccessor currentUser, IRunSessionStore store,
    LeaderboardService leaderboards, NextGoalService goals)
{
    public async Task<HomeResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetUserIdAsync(cancellationToken);
        var today = await leaderboards.GetAsync(LeaderboardPeriod.Today, cancellationToken);
        var run = await store.GetActiveByUserIdAsync(userId, cancellationToken);
        var goal = await goals.GetAsync(cancellationToken);
        return new HomeResponse(today.CurrentUser, run is null ? null : new ActiveRunSummary(
            run.Id, run.StartedAtUtc, run.DistanceMeters, run.DurationSeconds, run.AveragePaceSecondsPerKm),
            goal.Goal);
    }
}
