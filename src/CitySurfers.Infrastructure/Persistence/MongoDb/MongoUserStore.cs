using CitySurfers.Application.Users;
using CitySurfers.Infrastructure.Persistence.MongoDb.Documents;
using MongoDB.Driver;

namespace CitySurfers.Infrastructure.Persistence.MongoDb;

internal sealed class MongoUserStore(IMongoDatabase database) : IUserStore
{
    public async Task<UserData?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await database.GetCollection<UserDocument>("users")
            .Find(user => user.Username == username)
            .FirstOrDefaultAsync(cancellationToken);
        return user is null ? null : new UserData(user.Id, user.Username, user.DisplayName, user.DemoPassword);
    }
}
