namespace CitySurfers.Application.Leaderboards;

public interface ILeaderboardProvider
{
    Task<LeaderboardStandings> GetAsync(LeaderboardPeriod period, long currentUserPoints,
        CancellationToken cancellationToken = default);
}
