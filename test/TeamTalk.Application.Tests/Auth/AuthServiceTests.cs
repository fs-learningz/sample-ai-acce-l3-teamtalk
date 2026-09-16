using ErrorOr;
using NSubstitute;
using TeamTalk.Application.Auth;
using TeamTalk.Application.Auth.Contracts;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Common.Models;

namespace TeamTalk.Application.Tests.Auth;

public class AuthServiceTests
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _sut = new AuthService(_identityService, _jwtTokenGenerator);
    }

    [Fact]
    public async Task RegisterAsync_WhenIdentityServiceSucceeds_ReturnsAuthResponseWithToken()
    {
        var userId = Guid.NewGuid();
        var identityResult = new IdentityUserResult
        {
            UserId = userId,
            Email = "alice@example.com",
            DisplayName = "Alice",
        };
        _identityService
            .RegisterAsync("alice@example.com", "Alice", "Passw0rd!", Arg.Any<CancellationToken>())
            .Returns(identityResult);
        _jwtTokenGenerator
            .GenerateToken(userId, "alice@example.com", "Alice")
            .Returns("fake-jwt-token");

        var request = new RegisterRequest { Email = "alice@example.com", DisplayName = "Alice", Password = "Passw0rd!" };
        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal("fake-jwt-token", result.Value.Token);
    }

    [Fact]
    public async Task RegisterAsync_WhenIdentityServiceFails_PropagatesError()
    {
        _identityService
            .RegisterAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthErrors.EmailAlreadyExists);

        var request = new RegisterRequest { Email = "alice@example.com", DisplayName = "Alice", Password = "Passw0rd!" };
        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(AuthErrors.EmailAlreadyExists, result.FirstError);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsInvalid_ReturnsUnauthorizedError()
    {
        _identityService
            .ValidateCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthErrors.InvalidCredentials);

        var request = new LoginRequest { Email = "alice@example.com", Password = "wrong" };
        var result = await _sut.LoginAsync(request, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Unauthorized, result.FirstError.Type);
    }
}
