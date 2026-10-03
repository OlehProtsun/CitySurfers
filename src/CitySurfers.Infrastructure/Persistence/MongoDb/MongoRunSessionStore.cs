using CitySurfers.Application.Running;
using CitySurfers.Domain.Running;
using CitySurfers.Infrastructure.Persistence.MongoDb.Documents;
using MongoDB.Driver;

namespace CitySurfers.Infrastructure.Persistence.MongoDb;

internal sealed class MongoRunSessionStore(IMongoDatabase database) : IRunSessionStore
{
    private readonly IMongoCollection<RunDocument> runs = database.GetCollection<RunDocument>("runs");

    public async Task<RunSession?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var document = await runs.Find(run => run.UserId == userId && run.Status == "active")
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<RunSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await runs.Find(run => run.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task AddAsync(RunSession run, CancellationToken cancellationToken = default)
    {
        try
        {
            await runs.InsertOneAsync(RunDocument.FromDomain(run), cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new RunRuleException(RunError.Conflict, "An active run already exists.");
        }
    }

    public async Task SaveAsync(RunSession run, double previousDistance, double previousDuration,
        CancellationToken cancellationToken = default)
    {
        var result = await runs.ReplaceOneAsync(document => document.Id == run.Id && document.UserId == run.UserId
            && document.Status == "active" && document.DistanceMeters == previousDistance
            && document.DurationSeconds == previousDuration, RunDocument.FromDomain(run),
            cancellationToken: cancellationToken);
        if (result.MatchedCount == 0)
            throw new RunRuleException(RunError.Conflict, "The run changed. Reload it before retrying.");
    }
}
