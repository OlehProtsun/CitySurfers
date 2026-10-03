namespace CitySurfers.Application.Periods;

public sealed record UtcPeriodRange(DateTimeOffset StartUtc, DateTimeOffset EndUtc)
{
    public bool Contains(DateTimeOffset timestamp) => timestamp >= StartUtc && timestamp < EndUtc;
}

public sealed class KrakowPeriodResolver
{
    private readonly TimeZoneInfo timeZone = ResolveTimeZone();

    public UtcPeriodRange GetTodayUtcRange(DateTimeOffset now)
    {
        var start = LocalDate(now);
        return Range(start, start.AddDays(1));
    }

    public UtcPeriodRange GetCurrentWeekUtcRange(DateTimeOffset now)
    {
        var date = LocalDate(now);
        var start = date.AddDays(-((int)date.DayOfWeek + 6) % 7);
        return Range(start, start.AddDays(7));
    }

    public UtcPeriodRange GetCurrentMonthUtcRange(DateTimeOffset now)
    {
        var date = LocalDate(now);
        var start = new DateTime(date.Year, date.Month, 1);
        return Range(start, start.AddMonths(1));
    }

    public UtcPeriodRange GetPreviousMonthUtcRange(DateTimeOffset now)
    {
        var date = LocalDate(now);
        var end = new DateTime(date.Year, date.Month, 1);
        return Range(end.AddMonths(-1), end);
    }

    private DateTime LocalDate(DateTimeOffset now) => TimeZoneInfo.ConvertTime(now, timeZone).Date;

    private UtcPeriodRange Range(DateTime start, DateTime end) => new(
        new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(start, timeZone)),
        new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(end, timeZone)));

    private static TimeZoneInfo ResolveTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); }
    }
}
