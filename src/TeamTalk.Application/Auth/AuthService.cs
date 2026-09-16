using ErrorOr;
using TeamTalk.Application.Auth.Contracts;
using TeamTalk.Application.Common.Interfaces;

namespace TeamTalk.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IIdentityService identityService, IJwtTokenGenerator jwtTokenGenerator)
    {
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<ErrorOr<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityService.RegisterAsync(
            request.Email,
            request.DisplayName,
            request.Password,
            cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        return BuildAuthResponse(result.Value.UserId, result.Value.Email, result.Value.DisplayName);
    }

    public async Task<ErrorOr<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        return BuildAuthResponse(result.Value.UserId, result.Value.Email, result.Value.DisplayName);
    }

    private AuthResponse BuildAuthResponse(Guid userId, string email, string displayName)
    {
        var token = _jwtTokenGenerator.GenerateToken(userId, email, displayName);

        return new AuthResponse
        {
            UserId = userId,
            Email = email,
            DisplayName = displayName,
            Token = token,
        };
    }
}
