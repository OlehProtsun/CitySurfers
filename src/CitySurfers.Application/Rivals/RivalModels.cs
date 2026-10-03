using CitySurfers.Application.Leaderboards;

namespace CitySurfers.Application.Rivals;

public sealed record RivalSnapshot(string DisplayName, int Rank, long Points, long PointsGap, long PointsToPass);
public sealed record RivalResponse(string Period, DateTimeOffset PeriodStartUtc, DateTimeOffset PeriodEndUtc,
    CurrentUserRank CurrentUser, RivalSnapshot? Rival);
