using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TeamTalk.Infrastructure.Identity;

namespace TeamTalk.Infrastructure.Tests.Identity;

public class JwtTokenGeneratorTests
{
    private const string SigningKey = "unit-test-signing-key-please-do-not-use-in-prod-0123456789";

    private static JwtTokenGenerator CreateSut(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new JwtTokenGenerator(configuration);
    }

    private static Dictionary<string, string?> ValidSettings()
    {
        return new Dictionary<string, string?>
        {
            ["Jwt:Key"] = SigningKey,
            ["Jwt:Issuer"] = "teamtalk-issuer",
            ["Jwt:Audience"] = "teamtalk-audience",
        };
    }

    [Fact]
    public void GenerateToken_WithValidConfiguration_ContainsIdentityClaims()
    {
        // arrange
        var sut = CreateSut(ValidSettings());
        var userId = Guid.NewGuid();

        // act
        var token = sut.GenerateToken(userId, "alice@example.com", "Alice");

        // assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(userId.ToString(), jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("alice@example.com", jwt.Claims.Single(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Alice", jwt.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
    }

    [Fact]
    public void GenerateToken_WithValidConfiguration_SetsIssuerAndAudience()
    {
        // arrange
        var sut = CreateSut(ValidSettings());

        // act
        var token = sut.GenerateToken(Guid.NewGuid(), "alice@example.com", "Alice");

        // assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("teamtalk-issuer", jwt.Issuer);
        Assert.Contains("teamtalk-audience", jwt.Audiences);
    }

    [Fact]
    public void GenerateToken_ExpiresInAboutEightHours()
    {
        // arrange
        var sut = CreateSut(ValidSettings());
        var before = DateTime.UtcNow;

        // act
        var token = sut.GenerateToken(Guid.NewGuid(), "alice@example.com", "Alice");

        // assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var lifetime = jwt.ValidTo - before;
        Assert.InRange(lifetime, TimeSpan.FromHours(8) - TimeSpan.FromMinutes(1), TimeSpan.FromHours(8) + TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateToken_ProducesTokenThatValidatesWithSameKey()
    {
        // arrange
        var sut = CreateSut(ValidSettings());
        var token = sut.GenerateToken(Guid.NewGuid(), "alice@example.com", "Alice");
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
        };

        // act
        var principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out var validated);

        // assert
        Assert.NotNull(validated);
        Assert.True(principal.Identity?.IsAuthenticated);
    }

    [Fact]
    public void GenerateToken_ProducesTokenThatFailsValidationWithDifferentKey()
    {
        // arrange
        var sut = CreateSut(ValidSettings());
        var token = sut.GenerateToken(Guid.NewGuid(), "alice@example.com", "Alice");
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-different-signing-key-0123456789-abcdefghij")),
        };

        // act & assert
        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _));
    }

    [Fact]
    public void GenerateToken_WhenIssuerAndAudienceAreNotConfigured_StillGeneratesToken()
    {
        // arrange
        var sut = CreateSut(new Dictionary<string, string?> { ["Jwt:Key"] = SigningKey });

        // act
        var token = sut.GenerateToken(Guid.NewGuid(), "alice@example.com", "Alice");

        // assert
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateToken_WhenKeyIsMissing_Throws(string? key)
    {
        // arrange
        var sut = CreateSut(new Dictionary<string, string?> { ["Jwt:Key"] = key });

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            sut.GenerateToken(Guid.NewGuid(), "alice@example.com", "Alice"));
        Assert.Contains("Jwt:Key", exception.Message);
    }
}
