using CitySurfers.Infrastructure.Persistence.MongoDb.Documents;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CitySurfers.Infrastructure.Persistence.MongoDb;

internal sealed class DemoDataSeeder(IMongoDatabase database, IOptions<DemoDataOptions> options) : IDataInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var users = database.GetCollection<UserDocument>("users");
        await users.Indexes.CreateOneAsync(
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(user => user.Username),
                new CreateIndexOptions { Unique = true, Name = "username_unique" }),
            cancellationToken: cancellationToken);

        if (!options.Value.SeedOnStartup)
            return;

        var update = Builders<UserDocument>.Update
            .SetOnInsert(user => user.Id, "demo-user-1")
            .SetOnInsert(user => user.Username, "demo")
            .SetOnInsert(user => user.DisplayName, "Demo Runner")
            .SetOnInsert(user => user.DemoPassword, "1234")
            .SetOnInsert(user => user.CreatedAtUtc, DateTime.UtcNow);
        try
        {
            await users.UpdateOneAsync(user => user.Username == "demo", update,
                new UpdateOptions { IsUpsert = true }, cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Another instance may have inserted the same account during startup.
            if (!await users.Find(user => user.Username == "demo").AnyAsync(cancellationToken))
                throw;
        }
    }
}
