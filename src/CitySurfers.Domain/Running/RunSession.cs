namespace CitySurfers.Domain.Running;

public sealed class RunSession
{
    private readonly List<Overtake> overtakes = [];
    public string Id { get; }
    public string UserId { get; }
    public RunStatus Status { get; private set; } = RunStatus.Active;
    public DateTime StartedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? FinishedAtUtc { get; private set; }
    public double DistanceMeters { get; private set; }
    public double DurationSeconds { get; private set; }
    public double? AveragePaceSecondsPerKm { get; private set; }
    public int StartingRank { get; }
    public int CurrentRank { get; private set; }
    public int SeasonPointsEarned { get; private set; }
    public IReadOnlyList<Overtake> Overtakes => overtakes.AsReadOnly();

    public RunSession(string id, string userId, int startingRank, DateTime now)
    {
        Id = id;
        UserId = userId;
        StartingRank = CurrentRank = startingRank;
        StartedAtUtc = UpdatedAtUtc = now;
    }

    public void UpdateProgress(double distance, double duration, double? pace, DateTime now)
    {
        EnsureActive();
        if (!double.IsFinite(distance) || !double.IsFinite(duration) || distance < 0 || duration < 0
            || distance < DistanceMeters || duration < DurationSeconds
            || (pace.HasValue && !double.IsFinite(pace.Value)))
            throw new RunRuleException(RunError.Validation, "Distance and duration must be finite, non-negative and must not decrease.");
        DistanceMeters = distance;
        DurationSeconds = duration;
        AveragePaceSecondsPerKm = pace;
        UpdatedAtUtc = now;
    }

    public Overtake? TryOvertake(string key, string displayName, double threshold, int reward, DateTime now)
    {
        EnsureActive();
        if (DistanceMeters < threshold || overtakes.Any(item => item.OpponentKey == key))
            return null;
        var overtake = new Overtake(key, displayName, now, threshold, reward, CurrentRank, CurrentRank - 1);
        overtakes.Add(overtake);
        CurrentRank = overtake.RankAfter;
        SeasonPointsEarned += reward;
        return overtake;
    }

    public void Finish(DateTime now)
    {
        EnsureActive();
        Status = RunStatus.Completed;
        FinishedAtUtc = UpdatedAtUtc = now;
    }

    private void EnsureActive()
    {
        if (Status != RunStatus.Active)
            throw new RunRuleException(RunError.Conflict, "The run is already completed.");
    }

    public static RunSession Restore(string id, string userId, RunStatus status, DateTime startedAtUtc,
        DateTime updatedAtUtc, DateTime? finishedAtUtc, double distance, double duration, double? pace,
        int startingRank, int currentRank, int points, IEnumerable<Overtake> history)
    {
        var run = new RunSession(id, userId, startingRank, startedAtUtc)
        {
            Status = status, UpdatedAtUtc = updatedAtUtc, FinishedAtUtc = finishedAtUtc,
            DistanceMeters = distance, DurationSeconds = duration, AveragePaceSecondsPerKm = pace,
            CurrentRank = currentRank, SeasonPointsEarned = points
        };
        run.overtakes.AddRange(history);
        return run;
    }
}
