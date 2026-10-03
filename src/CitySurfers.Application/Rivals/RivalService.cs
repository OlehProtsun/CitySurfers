using CitySurfers.Application.Leaderboards;

namespace CitySurfers.Application.Rivals;

public sealed class RivalService(LeaderboardService leaderboards)
{
    public async Task<RivalResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var board = await leaderboards.GetAsync(LeaderboardPeriod.Month, cancellationToken);
        var row = board.CurrentUser.Rank <= 1 ? null :
            board.AroundMe.Concat(board.Top).FirstOrDefault(row =>
                row.Rank == board.CurrentUser.Rank - 1 && !row.IsCurrentUser);
        var rival = row is null ? null : new RivalSnapshot(row.DisplayName, row.Rank, row.Points,
            Math.Max(0, row.Points - board.CurrentUser.Points),
            Math.Max(1, row.Points - board.CurrentUser.Points + 1));
        return new RivalResponse(board.Period, board.PeriodStartUtc, board.PeriodEndUtc, board.CurrentUser, rival);
    }
}
