using CitySurfers.Application.Authentication;
using CitySurfers.Application.Users;
using CitySurfers.Infrastructure.Authentication;

namespace CitySurfers.UnitTests;

public sealed class DemoAuthServiceTests
{
    [Fact]
    public async Task Valid_credentials_return_persisted_user_without_password()
    {
        var store = new FakeUserStore();
        var result = await new DemoAuthService(store).LoginAsync(new LoginRequest("demo", "1234"));

        Assert.Equal(new LoginResult("demo-user-1", "demo", "Persisted Runner"), result);
        Assert.Equal("demo", store.RequestedUsername);
    }

    [Theory]
    [InlineData("demo", "wrong")]
    [InlineData("unknown", "1234")]
    [InlineData("Demo", "1234")]
    [InlineData("demo", "1234 ")]
    public async Task Invalid_credentials_return_failure(string username, string password)
    {
        Assert.Null(await new DemoAuthService(new FakeUserStore()).LoginAsync(new LoginRequest(username, password)));
    }

    [Theory]
    [InlineData("", "1234")]
    [InlineData(" ", "1234")]
    [InlineData("demo", "")]
    [InlineData("demo", " ")]
    [InlineData(null, "1234")]
    [InlineData("demo", null)]
    public async Task Empty_credentials_do_not_query_store(string? username, string? password)
    {
        var store = new FakeUserStore();
        Assert.Null(await new DemoAuthService(store).LoginAsync(new LoginRequest(username!, password!)));
        Assert.Null(store.RequestedUsername);
    }

    [Fact]
    public async Task Cancellation_is_forwarded_to_store()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new DemoAuthService(new FakeUserStore()).LoginAsync(new LoginRequest("demo", "1234"), source.Token));
    }

    private sealed class FakeUserStore : IUserStore
    {
        public string? RequestedUsername { get; private set; }
        public Task<UserData?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestedUsername = username;
            return Task.FromResult(username == "demo"
                ? new UserData("demo-user-1", "demo", "Persisted Runner", "1234")
                : null);
        }
    }
}
