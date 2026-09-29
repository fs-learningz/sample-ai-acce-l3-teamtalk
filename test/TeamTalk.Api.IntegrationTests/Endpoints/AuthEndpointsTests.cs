using System.Net;
using System.Net.Http.Json;
using System.Text;
using TeamTalk.Api.IntegrationTests.Infrastructure;
using static TeamTalk.Api.IntegrationTests.Infrastructure.ApiTestHelper;

namespace TeamTalk.Api.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
public class AuthEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public AuthEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsOkWithToken()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";

        // act
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto { Email = email, DisplayName = "Alice", Password = DefaultPassword },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken);
        Assert.NotNull(auth);
        Assert.NotEqual(Guid.Empty, auth!.UserId);
        Assert.Equal(email, auth.Email);
        Assert.Equal("Alice", auth.DisplayName);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ReturnsConflict()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var existing = await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto { Email = existing.Email, DisplayName = "Other", Password = DefaultPassword },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(cancellationToken);
        Assert.Equal("Auth.EmailAlreadyExists", problem!.Title);
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("nouppercase1")]
    [InlineData("NoDigitsHere")]
    public async Task Register_WithWeakPassword_ReturnsBadRequest(string password)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";

        // act
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto { Email = email, DisplayName = "Alice", Password = password },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(cancellationToken);
        Assert.Equal("Auth.RegistrationFailed", problem!.Title);
    }

    [Fact]
    public async Task Register_WhenRequiredFieldsAreMissing_ReturnsBadRequest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // act
        var response = await client.PostAsync("/api/auth/register", content, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var registered = await RegisterAsync(_factory.CreateClient(), cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = registered.Email, Password = DefaultPassword },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken);
        Assert.NotNull(auth);
        Assert.Equal(registered.UserId, auth!.UserId);
        Assert.Equal(registered.Email, auth.Email);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var registered = await RegisterAsync(_factory.CreateClient(), cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = registered.Email, Password = "WrongPassw0rd!" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(cancellationToken);
        Assert.Equal("Auth.InvalidCredentials", problem!.Title);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();

        // act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto { Email = $"nobody-{Guid.NewGuid():N}@example.com", Password = DefaultPassword },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenRequiredFieldsAreMissing_ReturnsBadRequest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // act
        var response = await client.PostAsync("/api/auth/login", content, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private class ProblemDto
    {
        public string? Title { get; init; }
    }
}
