using CitySurfers.Application.Authentication;
using CitySurfers.Application.Users;

namespace CitySurfers.Infrastructure.Authentication;

public sealed class DemoAuthService(IUserStore users) : IAuthService
{
    public async Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return null;

        var user = await users.FindByUsernameAsync(request.Username, cancellationToken);
        return user is not null && string.Equals(user.DemoPassword, request.Password, StringComparison.Ordinal)
            ? new LoginResult(user.Id, user.Username, user.DisplayName)
            : null;
    }
}
