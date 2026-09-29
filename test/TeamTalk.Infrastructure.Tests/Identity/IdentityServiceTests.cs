using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamTalk.Application.Common.Errors;
using TeamTalk.Infrastructure.Identity;
using TeamTalk.Infrastructure.Persistence;

namespace TeamTalk.Infrastructure.Tests.Identity;

[Collection(PostgresCollection.Name)]
public class IdentityServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IdentityService _sut;

    public IdentityServiceTests(PostgresContainerFixture fixture)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TeamTalkDbContext>(options => options.UseNpgsql(fixture.ConnectionString));
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<TeamTalkDbContext>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        var userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _sut = new IdentityService(userManager);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    private static string NewEmail()
    {
        return $"user-{Guid.NewGuid():N}@example.com";
    }

    [Fact]
    public async Task RegisterAsync_WithValidDetails_CreatesUserAndReturnsResult()
    {
        // arrange
        var email = NewEmail();

        // act
        var result = await _sut.RegisterAsync(email, "Alice", "Passw0rd!");

        // assert
        Assert.False(result.IsError);
        Assert.NotEqual(Guid.Empty, result.Value.UserId);
        Assert.Equal(email, result.Value.Email);
        Assert.Equal("Alice", result.Value.DisplayName);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ReturnsConflict()
    {
        // arrange
        var email = NewEmail();
        await _sut.RegisterAsync(email, "Alice", "Passw0rd!");

        // act
        var result = await _sut.RegisterAsync(email, "Other", "Passw0rd!");

        // assert
        Assert.True(result.IsError);
        Assert.Equal(AuthErrors.EmailAlreadyExists, result.FirstError);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailDiffersOnlyByCase_ReturnsConflict()
    {
        // arrange
        var email = NewEmail();
        await _sut.RegisterAsync(email, "Alice", "Passw0rd!");

        // act
        var result = await _sut.RegisterAsync(email.ToUpperInvariant(), "Other", "Passw0rd!");

        // assert
        Assert.True(result.IsError);
        Assert.Equal(AuthErrors.EmailAlreadyExists, result.FirstError);
    }

    [Theory]
    [InlineData("Sh0rt")]
    [InlineData("nouppercase1")]
    [InlineData("NoDigitsHere")]
    public async Task RegisterAsync_WithWeakPassword_ReturnsRegistrationFailedWithDescriptions(string password)
    {
        // arrange
        var email = NewEmail();

        // act
        var result = await _sut.RegisterAsync(email, "Alice", password);

        // assert
        Assert.True(result.IsError);
        Assert.Equal("Auth.RegistrationFailed", result.FirstError.Code);
        Assert.False(string.IsNullOrWhiteSpace(result.FirstError.Description));
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithCorrectPassword_ReturnsUser()
    {
        // arrange
        var email = NewEmail();
        var registered = await _sut.RegisterAsync(email, "Alice", "Passw0rd!");

        // act
        var result = await _sut.ValidateCredentialsAsync(email, "Passw0rd!");

        // assert
        Assert.False(result.IsError);
        Assert.Equal(registered.Value.UserId, result.Value.UserId);
        Assert.Equal("Alice", result.Value.DisplayName);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithWrongPassword_ReturnsInvalidCredentials()
    {
        // arrange
        var email = NewEmail();
        await _sut.RegisterAsync(email, "Alice", "Passw0rd!");

        // act
        var result = await _sut.ValidateCredentialsAsync(email, "WrongPassw0rd!");

        // assert
        Assert.True(result.IsError);
        Assert.Equal(AuthErrors.InvalidCredentials, result.FirstError);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithUnknownEmail_ReturnsInvalidCredentials()
    {
        // act
        var result = await _sut.ValidateCredentialsAsync(NewEmail(), "Passw0rd!");

        // assert
        Assert.True(result.IsError);
        Assert.Equal(AuthErrors.InvalidCredentials, result.FirstError);
    }

    [Fact]
    public async Task FindUserIdByEmailAsync_WhenUserExists_ReturnsUserId()
    {
        // arrange
        var email = NewEmail();
        var registered = await _sut.RegisterAsync(email, "Alice", "Passw0rd!");

        // act
        var userId = await _sut.FindUserIdByEmailAsync(email);

        // assert
        Assert.Equal(registered.Value.UserId, userId);
    }

    [Fact]
    public async Task FindUserIdByEmailAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // act
        var userId = await _sut.FindUserIdByEmailAsync(NewEmail());

        // assert
        Assert.Null(userId);
    }
}
