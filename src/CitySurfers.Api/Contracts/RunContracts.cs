using System.ComponentModel.DataAnnotations;
using CitySurfers.Application.Running;
using CitySurfers.Domain.Running;

namespace CitySurfers.Api.Contracts;

public sealed record RunHistoryResponse(IReadOnlyList<RunHistoryItem> Items);

public sealed record RunProgressInput(
    [Required, Range(0, double.MaxValue)] double? DistanceMeters,
    [Required, Range(0, double.MaxValue)] double? DurationSeconds);

public sealed record OvertakeEvent(string Type, string Opponent, int RankBefore, int RankAfter, int PointsAwarded);
public sealed record OvertakeResponse(string OpponentKey, string Opponent, DateTime CompletedAtUtc,
    double DistanceThresholdMeters, int PointsAwarded, int RankBefore, int RankAfter)
{
    public static OvertakeResponse From(Overtake item) => new(item.OpponentKey, item.OpponentDisplayName,
        item.CompletedAtUtc, item.DistanceThresholdMeters, item.PointsAwarded, item.RankBefore, item.RankAfter);
}

public sealed record RunResponse(string Id, string Status, DateTime StartedAtUtc, DateTime UpdatedAtUtc,
    double DistanceMeters, double DurationSeconds, double? AveragePaceSecondsPerKm,
    CompetitionSnapshot Competition, IReadOnlyList<OvertakeEvent> Events)
{
    public static RunResponse From(RunSessionResult result) => new(result.Run.Id,
        result.Run.Status == RunStatus.Active ? "active" : "completed", result.Run.StartedAtUtc,
        result.Run.UpdatedAtUtc, result.Run.DistanceMeters, result.Run.DurationSeconds,
        result.Run.AveragePaceSecondsPerKm, result.Competition,
        result.Events.Select(item => new OvertakeEvent("OVERTAKE", item.OpponentDisplayName,
            item.RankBefore, item.RankAfter, item.PointsAwarded)).ToArray());
}

public sealed record PostRunSummary(string RunId, DateTime StartedAtUtc, DateTime? FinishedAtUtc,
    double DistanceMeters, double DurationSeconds, double? AveragePaceSecondsPerKm, int OvertakesCount,
    IReadOnlyList<OvertakeResponse> Overtakes, int RankBefore, int RankAfter, int SeasonPointsEarned,
    TargetSnapshot? NextTarget)
{
    public static PostRunSummary From(RunSessionResult result) => new(result.Run.Id, result.Run.StartedAtUtc,
        result.Run.FinishedAtUtc, result.Run.DistanceMeters, result.Run.DurationSeconds,
        result.Run.AveragePaceSecondsPerKm, result.Run.Overtakes.Count,
        result.Run.Overtakes.Select(OvertakeResponse.From).ToArray(), result.Run.StartingRank,
        result.Run.CurrentRank, result.Run.SeasonPointsEarned, result.Competition.CurrentTarget);
}
