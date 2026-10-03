using CitySurfers.Application.Running;
using CitySurfers.Infrastructure.Persistence.MongoDb.Documents;
using MongoDB.Driver;

namespace CitySurfers.Infrastructure.Persistence.MongoDb;

internal sealed class MongoRunHistoryReader(IMongoDatabase database) : IRunHistoryReader
{
    private readonly IMongoCollection<RunDocument> runs = database.GetCollection<RunDocument>("runs");

    public async Task<IReadOnlyList<RunHistoryItem>> GetCompletedAsync(string userId,
        DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (limit is <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit));
        var filters = Builders<RunDocument>.Filter;
        var filter = filters.Eq(run => run.UserId, userId) & filters.Eq(run => run.Status, "completed");
        if (fromUtc.HasValue)
            filter &= filters.Gte(run => run.StartedAtUtc, fromUtc.Value.UtcDateTime);
        if (toUtc.HasValue)
            filter &= filters.Lt(run => run.StartedAtUtc, toUtc.Value.UtcDateTime);
        var documents = await runs.Find(filter)
            .Sort(Builders<RunDocument>.Sort.Descending(run => run.StartedAtUtc).Ascending(run => run.Id))
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(run => new RunHistoryItem(run.Id, run.StartedAtUtc, run.FinishedAtUtc,
            run.DistanceMeters, run.DurationSeconds, run.AveragePaceSecondsPerKm,
            run.Overtakes.Count, run.SeasonPointsEarned)).ToArray();
    }
}
