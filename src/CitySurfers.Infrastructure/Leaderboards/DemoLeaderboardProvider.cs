using CitySurfers.Application.Leaderboards;

namespace CitySurfers.Infrastructure.Leaderboards;

public sealed class DemoLeaderboardProvider : ILeaderboardProvider
{
    private static readonly string[] Names = ["Marta", "Piotr", "Anna", "Tomasz", "Kasia", "Jakub", "Zofia", "Adam"];

    public Task<LeaderboardStandings> GetAsync(LeaderboardPeriod period, long currentUserPoints,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (period is not (LeaderboardPeriod.Today or LeaderboardPeriod.Month))
            throw new ArgumentOutOfRangeException(nameof(period));
        if (currentUserPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(currentUserPoints));
        var competitors = Enumerable.Range(0, 40).Select(index =>
        {
            var dailyPoints = index switch { 0 => 8, 1 => 24, 2 => 36, 3 => 50, _ => 64 + (index - 4) * 12 };
            return new Competitor($"competitor-{index:D2}", $"{Names[index % Names.Length]}_{index + 1}",
                period == LeaderboardPeriod.Today ? dailyPoints : dailyPoints * 25L + index * 7L, false);
        }).Append(new Competitor("current-user", "You", currentUserPoints, true));
        var ranked = competitors.OrderByDescending(row => row.Points).ThenBy(row => row.Key, StringComparer.Ordinal)
            .Select((row, index) => new LeaderboardRow(index + 1, row.DisplayName, row.Points, row.IsCurrentUser))
            .ToArray();
        var current = ranked.Single(row => row.IsCurrentUser);
        var around = ranked.Skip(Math.Max(0, current.Rank - 3)).Take(5).ToArray();
        return Task.FromResult(new LeaderboardStandings(new CurrentUserRank(current.Rank, current.Points),
            ranked.Take(10).ToArray(), around));
    }

    private sealed record Competitor(string Key, string DisplayName, long Points, bool IsCurrentUser);
}
