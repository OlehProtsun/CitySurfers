using CitySurfers.Application.Running;
using CitySurfers.Application.Rivals;
using CitySurfers.Application.Users;

namespace CitySurfers.Application.Goals;

public sealed class NextGoalService(ICurrentUserAccessor currentUser, IRunSessionStore store,
    IRunCompetitionProvider competition, RivalService rivals)
{
    public async Task<NextGoalResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetUserIdAsync(cancellationToken);
        var run = await store.GetActiveByUserIdAsync(userId, cancellationToken);
        if (run is not null)
        {
            var snapshot = competition.GetSnapshot(run);
            if (snapshot.CurrentTarget is { } target)
                return new NextGoalResponse(new NextGoal("run_overtake", "active_run", target.Opponent,
                    run.Id, target.DistanceToOvertakeMeters, null, target.PotentialPoints,
                    snapshot.Rank, snapshot.Rank - 1));
        }

        var monthly = await rivals.GetAsync(cancellationToken);
        return new NextGoalResponse(monthly.Rival is { } rival
            ? new NextGoal("rival_points", "monthly_leaderboard", rival.DisplayName,
                null, null, rival.PointsToPass, null, monthly.CurrentUser.Rank, rival.Rank)
            : null);
    }
}
