using CitySurfers.Application.Users;
using CitySurfers.Infrastructure.Authentication;

namespace CitySurfers.UnitTests;

public sealed class DemoCurrentUserAccessorTests
{
    [Fact]
    public async Task Resolves_persisted_demo_id() =>
        Assert.Equal("persisted-id", await new DemoCurrentUserAccessor(new Users()).GetUserIdAsync());

    [Fact]
    public async Task Missing_demo_user_fails() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DemoCurrentUserAccessor(new Users(false)).GetUserIdAsync());

    [Fact]
    public async Task Forwards_cancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new DemoCurrentUserAccessor(new Users()).GetUserIdAsync(source.Token));
    }

    private sealed class Users(bool exists = true) : IUserStore
    {
        public Task<UserData?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal("demo", username);
            return Task.FromResult(exists ? new UserData("persisted-id", username, "Runner", "1234") : null);
        }
    }
}
