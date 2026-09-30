using ErrorOr;
using TeamTalk.Application.Auth.Contracts;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Common.Models;

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

    public async Task<ErrorOr<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var result = await _identityService.RegisterAsync(
            request.Email,
            request.DisplayName,
            request.Password);

        if (result.IsError)
        {
            return result.Errors;
        }

        return BuildAuthResponse(result.Value);
    }

    public async Task<ErrorOr<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var result = await _identityService.ValidateCredentialsAsync(request.Email, request.Password);

        if (result.IsError)
        {
            return result.Errors;
        }

        return BuildAuthResponse(result.Value);
    }

    private AuthResponse BuildAuthResponse(IdentityUserResult user)
    {
        var token = _jwtTokenGenerator.GenerateToken(user.UserId, user.Email, user.DisplayName);

        return new AuthResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Token = token,
            Theme = user.Theme,
        };
    }
}
