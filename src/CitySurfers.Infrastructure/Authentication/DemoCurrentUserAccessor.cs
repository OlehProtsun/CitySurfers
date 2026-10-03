using CitySurfers.Application.Users;

namespace CitySurfers.Infrastructure.Authentication;

public sealed class DemoCurrentUserAccessor(IUserStore users) : ICurrentUserAccessor
{
    public async Task<string> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        var user = await users.FindByUsernameAsync("demo", cancellationToken);
        return user?.Id ?? throw new InvalidOperationException("The demo user is unavailable.");
    }
}
