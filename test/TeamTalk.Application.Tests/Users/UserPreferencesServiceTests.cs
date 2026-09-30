using NSubstitute;
using TeamTalk.Application.Common.Interfaces;
using TeamTalk.Application.Users;
using TeamTalk.Application.Users.Contracts;

namespace TeamTalk.Application.Tests.Users;

public class UserPreferencesServiceTests
{
    private readonly IIdentityService _identityService;
    private readonly UserPreferencesService _sut;

    public UserPreferencesServiceTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _sut = new UserPreferencesService(_identityService);
    }

    [Fact]
    public async Task UpdateThemeAsync_WhenThemeIsValid_SavesAndReturnsTheme()
    {
        // arrange
        var userId = Guid.NewGuid();
        _identityService.SetThemeAsync(userId, "dracula").Returns(true);

        // act
        var result = await _sut.UpdateThemeAsync(userId, new UpdateUserThemeRequest { Theme = "dracula" });

        // assert
        Assert.False(result.IsError);
        Assert.Equal("dracula", result.Value.Theme);
    }

    [Fact]
    public async Task UpdateThemeAsync_WhenThemeIsUnknown_ReturnsValidationErrorAndDoesNotSave()
    {
        // arrange
        var userId = Guid.NewGuid();

        // act
        var result = await _sut.UpdateThemeAsync(userId, new UpdateUserThemeRequest { Theme = "neon" });

        // assert
        Assert.True(result.IsError);
        Assert.Equal("User.InvalidTheme", result.FirstError.Code);
        await _identityService.DidNotReceive().SetThemeAsync(Arg.Any<Guid>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetThemeAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var userId = Guid.NewGuid();
        _identityService.GetThemeAsync(userId).Returns((string?)null);

        // act
        var result = await _sut.GetThemeAsync(userId);

        // assert
        Assert.True(result.IsError);
        Assert.Equal("User.NotFound", result.FirstError.Code);
    }
}
