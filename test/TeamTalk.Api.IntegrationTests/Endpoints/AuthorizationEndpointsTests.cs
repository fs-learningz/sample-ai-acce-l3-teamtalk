using System.Net;
using System.Net.Http.Headers;
using TeamTalk.Api.IntegrationTests.Infrastructure;
using static TeamTalk.Api.IntegrationTests.Infrastructure.ApiTestHelper;

namespace TeamTalk.Api.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
public class AuthorizationEndpointsTests
{
    private static readonly Guid AnyId = Guid.NewGuid();

    private readonly ApiWebApplicationFactory _factory;

    public AuthorizationEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public static TheoryData<string, string> ProtectedEndpoints()
    {
        return new TheoryData<string, string>
        {
            { "POST", "/api/workspaces" },
            { "GET", "/api/workspaces" },
            { "GET", $"/api/workspaces/{AnyId}" },
            { "PUT", $"/api/workspaces/{AnyId}" },
            { "PUT", $"/api/workspaces/{AnyId}/icon" },
            { "DELETE", $"/api/workspaces/{AnyId}/icon" },
            { "GET", $"/api/workspaces/{AnyId}/icon" },
            { "DELETE", $"/api/workspaces/{AnyId}" },
            { "GET", $"/api/workspaces/{AnyId}/members" },
            { "POST", $"/api/workspaces/{AnyId}/members/leave" },
            { "DELETE", $"/api/workspaces/{AnyId}/members/{AnyId}" },
            { "PATCH", $"/api/workspaces/{AnyId}/members/{AnyId}/role" },
            { "POST", $"/api/workspaces/{AnyId}/invitations" },
            { "GET", $"/api/workspaces/{AnyId}/invitations" },
            { "DELETE", $"/api/workspaces/{AnyId}/invitations/{AnyId}" },
            { "GET", "/api/invitations" },
            { "POST", $"/api/invitations/{AnyId}/accept" },
            { "POST", $"/api/invitations/{AnyId}/decline" },
            { "GET", $"/api/invitations/{AnyId}/workspace-icon" },
            { "POST", $"/api/workspaces/{AnyId}/channels" },
            { "GET", $"/api/workspaces/{AnyId}/channels" },
            { "POST", $"/api/workspaces/{AnyId}/channels/{AnyId}/join" },
            { "GET", $"/api/workspaces/{AnyId}/channels/{AnyId}/messages" },
            { "POST", $"/api/workspaces/{AnyId}/channels/{AnyId}/messages" },
            { "PUT", $"/api/workspaces/{AnyId}/channels/{AnyId}/messages/{AnyId}" },
            { "DELETE", $"/api/workspaces/{AnyId}/channels/{AnyId}/messages/{AnyId}" },
        };
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized(string method, string url)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        // act
        var response = await client.SendAsync(request, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_WithMalformedToken_ReturnsUnauthorized(string method, string url)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.jwt");
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        // act
        var response = await client.SendAsync(request, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTokenSignedByDifferentKey_ReturnsUnauthorized()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var forgedToken = CreateToken("a-completely-different-signing-key-0123456789-abcdef", DateTime.UtcNow.AddHours(1));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forgedToken);

        // act
        var response = await client.GetAsync("/api/workspaces", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var expiredToken = CreateToken(
            "integration-test-signing-key-please-do-not-use-in-prod-0123456789",
            DateTime.UtcNow.AddHours(-2));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // act
        var response = await client.GetAsync("/api/workspaces", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_IsAccepted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.GetAsync("/api/workspaces", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string CreateToken(string key, DateTime expiresUtc)
    {
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)),
            Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            claims: claims,
            notBefore: expiresUtc.AddHours(-1),
            expires: expiresUtc,
            signingCredentials: credentials);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
