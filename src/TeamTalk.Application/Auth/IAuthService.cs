using ErrorOr;
using TeamTalk.Application.Auth.Contracts;

namespace TeamTalk.Application.Auth;

public interface IAuthService
{
    Task<ErrorOr<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<ErrorOr<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
