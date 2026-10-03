namespace CitySurfers.Application.ActivityMap;

public enum ActivityPeriod { Live, Today, Month }

public sealed record ActivityZone(string Id, string Name, double Latitude, double Longitude,
    int ActiveRunners, int Runs, double? AveragePaceSecondsPerKm, string ActivityLevel);

public sealed record ActivityMapResponse(string Period, DateTimeOffset GeneratedAtUtc, IReadOnlyList<ActivityZone> Zones);
