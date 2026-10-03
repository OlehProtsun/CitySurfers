using CitySurfers.Domain.Running;
using MongoDB.Bson.Serialization.Attributes;

namespace CitySurfers.Infrastructure.Persistence.MongoDb.Documents;

[BsonIgnoreExtraElements]
internal sealed class RunDocument
{
    [BsonId] public string Id { get; set; } = "";
    [BsonElement("userId")] public string UserId { get; set; } = "";
    [BsonElement("status")] public string Status { get; set; } = "active";
    [BsonElement("startedAtUtc"), BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime StartedAtUtc { get; set; }
    [BsonElement("updatedAtUtc"), BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAtUtc { get; set; }
    [BsonElement("finishedAtUtc"), BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? FinishedAtUtc { get; set; }
    [BsonElement("distanceMeters")] public double DistanceMeters { get; set; }
    [BsonElement("durationSeconds")] public double DurationSeconds { get; set; }
    [BsonElement("averagePaceSecondsPerKm")] public double? AveragePaceSecondsPerKm { get; set; }
    [BsonElement("startingRank")] public int StartingRank { get; set; }
    [BsonElement("currentRank")] public int CurrentRank { get; set; }
    [BsonElement("seasonPointsEarned")] public int SeasonPointsEarned { get; set; }
    [BsonElement("overtakes")] public List<OvertakeDocument> Overtakes { get; set; } = [];

    public RunSession ToDomain() => RunSession.Restore(Id, UserId,
        Status switch { "active" => RunStatus.Active, "completed" => RunStatus.Completed,
            _ => throw new InvalidOperationException("Invalid stored run status.") },
        StartedAtUtc, UpdatedAtUtc, FinishedAtUtc, DistanceMeters, DurationSeconds,
        AveragePaceSecondsPerKm, StartingRank, CurrentRank, SeasonPointsEarned,
        Overtakes.Select(item => new Overtake(item.OpponentKey, item.OpponentDisplayName, item.CompletedAtUtc,
            item.DistanceThresholdMeters, item.PointsAwarded, item.RankBefore, item.RankAfter)));

    public static RunDocument FromDomain(RunSession run) => new()
    {
        Id = run.Id, UserId = run.UserId, Status = run.Status == RunStatus.Active ? "active" : "completed",
        StartedAtUtc = run.StartedAtUtc, UpdatedAtUtc = run.UpdatedAtUtc, FinishedAtUtc = run.FinishedAtUtc,
        DistanceMeters = run.DistanceMeters, DurationSeconds = run.DurationSeconds,
        AveragePaceSecondsPerKm = run.AveragePaceSecondsPerKm, StartingRank = run.StartingRank,
        CurrentRank = run.CurrentRank, SeasonPointsEarned = run.SeasonPointsEarned,
        Overtakes = run.Overtakes.Select(item => new OvertakeDocument
        {
            OpponentKey = item.OpponentKey, OpponentDisplayName = item.OpponentDisplayName,
            CompletedAtUtc = item.CompletedAtUtc, DistanceThresholdMeters = item.DistanceThresholdMeters,
            PointsAwarded = item.PointsAwarded, RankBefore = item.RankBefore, RankAfter = item.RankAfter
        }).ToList()
    };
}

internal sealed class OvertakeDocument
{
    [BsonElement("opponentKey")] public string OpponentKey { get; set; } = "";
    [BsonElement("opponentDisplayName")] public string OpponentDisplayName { get; set; } = "";
    [BsonElement("completedAtUtc"), BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CompletedAtUtc { get; set; }
    [BsonElement("distanceThresholdMeters")] public double DistanceThresholdMeters { get; set; }
    [BsonElement("pointsAwarded")] public int PointsAwarded { get; set; }
    [BsonElement("rankBefore")] public int RankBefore { get; set; }
    [BsonElement("rankAfter")] public int RankAfter { get; set; }
}
