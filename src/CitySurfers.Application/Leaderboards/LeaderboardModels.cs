namespace CitySurfers.Application.Leaderboards;

public enum LeaderboardPeriod { Today, Month }

public sealed record CurrentUserRank(int Rank, long Points);
public sealed record LeaderboardRow(int Rank, string DisplayName, long Points, bool IsCurrentUser);
public sealed record LeaderboardStandings(CurrentUserRank CurrentUser,
    IReadOnlyList<LeaderboardRow> Top, IReadOnlyList<LeaderboardRow> AroundMe);
public sealed record LeaderboardResponse(string Period, DateTimeOffset PeriodStartUtc, DateTimeOffset PeriodEndUtc,
    CurrentUserRank CurrentUser, IReadOnlyList<LeaderboardRow> Top, IReadOnlyList<LeaderboardRow> AroundMe);
