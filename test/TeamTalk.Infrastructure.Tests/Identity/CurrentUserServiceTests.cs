using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using TeamTalk.Infrastructure.Identity;

namespace TeamTalk.Infrastructure.Tests.Identity;

public class CurrentUserServiceTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CurrentUserService _sut;

    public CurrentUserServiceTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _sut = new CurrentUserService(_httpContextAccessor);
    }

    private void SetUser(params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
        };
        _httpContextAccessor.HttpContext.Returns(context);
    }

    [Fact]
    public void UserId_WhenNameIdentifierClaimIsGuid_ReturnsUserId()
    {
        // arrange
        var userId = Guid.NewGuid();
        SetUser(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        // act
        var result = _sut.UserId;

        // assert
        Assert.Equal(userId, result);
    }

    [Fact]
    public void UserId_WhenThereIsNoHttpContext_Throws()
    {
        // arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // act & assert
        Assert.Throws<InvalidOperationException>(() => _sut.UserId);
    }

    [Fact]
    public void UserId_WhenClaimIsMissing_Throws()
    {
        // arrange
        SetUser(new Claim(ClaimTypes.Email, "alice@example.com"));

        // act & assert
        Assert.Throws<InvalidOperationException>(() => _sut.UserId);
    }

    [Fact]
    public void UserId_WhenClaimIsNotAGuid_Throws()
    {
        // arrange
        SetUser(new Claim(ClaimTypes.NameIdentifier, "not-a-guid"));

        // act & assert
        Assert.Throws<InvalidOperationException>(() => _sut.UserId);
    }

    [Fact]
    public void Email_WhenEmailClaimIsPresent_ReturnsEmail()
    {
        // arrange
        SetUser(new Claim(ClaimTypes.Email, "alice@example.com"));

        // act
        var result = _sut.Email;

        // assert
        Assert.Equal("alice@example.com", result);
    }

    [Fact]
    public void Email_WhenThereIsNoHttpContext_Throws()
    {
        // arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // act & assert
        Assert.Throws<InvalidOperationException>(() => _sut.Email);
    }

    [Fact]
    public void Email_WhenClaimIsMissing_Throws()
    {
        // arrange
        SetUser(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        // act & assert
        Assert.Throws<InvalidOperationException>(() => _sut.Email);
    }
}
