using System.Net;
using System.Net.Http.Json;
using TeamTalk.Api.IntegrationTests.Infrastructure;
using static TeamTalk.Api.IntegrationTests.Infrastructure.ApiTestHelper;

namespace TeamTalk.Api.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
public class ChannelsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public ChannelsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Public")]
    [InlineData("Private")]
    public async Task Create_WithValidRequest_ReturnsChannel(string type)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var owner = await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels",
            new CreateChannelRequestDto { Name = "general", Type = type },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var channel = await response.Content.ReadFromJsonAsync<ChannelDto>(cancellationToken);
        Assert.NotNull(channel);
        Assert.Equal(workspace.Id, channel!.WorkspaceId);
        Assert.Equal("general", channel.Name);
        Assert.Equal(type, channel.Type);
        Assert.Equal(owner.UserId, channel.CreatedByUserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithBlankName_ReturnsBadRequest(string name)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels",
            new CreateChannelRequestDto { Name = name, Type = "Public" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenNameAlreadyExistsInWorkspace_ReturnsConflict()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels",
            new CreateChannelRequestDto { Name = "general", Type = "Public" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenSameNameInDifferentWorkspace_ReturnsOk()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var first = await CreateWorkspaceAsync(client, cancellationToken, "First");
        var second = await CreateWorkspaceAsync(client, cancellationToken, "Second");
        await CreateChannelAsync(client, first.Id, "general", "Public", cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            $"/api/workspaces/{second.Id}/channels",
            new CreateChannelRequestDto { Name = "general", Type = "Public" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenUserIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var outsiderClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        await RegisterAsync(outsiderClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);

        // act
        var response = await outsiderClient.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels",
            new CreateChannelRequestDto { Name = "general", Type = "Public" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetVisible_ReturnsPublicChannelsAndOwnPrivateChannelsOrderedByName()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, "Member", cancellationToken);
        await CreateChannelAsync(ownerClient, workspace.Id, "zeta", "Public", cancellationToken);
        await CreateChannelAsync(ownerClient, workspace.Id, "alpha", "Public", cancellationToken);
        await CreateChannelAsync(ownerClient, workspace.Id, "owners-only", "Private", cancellationToken);
        await CreateChannelAsync(memberClient, workspace.Id, "my-private", "Private", cancellationToken);

        // act
        var response = await memberClient.GetAsync($"/api/workspaces/{workspace.Id}/channels", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var channels = await response.Content.ReadFromJsonAsync<List<ChannelDto>>(cancellationToken);
        Assert.Equal(["alpha", "my-private", "zeta"], channels!.Select(c => c.Name).ToArray());
    }

    [Fact]
    public async Task GetVisible_WhenUserIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var outsiderClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        await RegisterAsync(outsiderClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);

        // act
        var response = await outsiderClient.GetAsync($"/api/workspaces/{workspace.Id}/channels", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Join_WhenPublicChannel_ReturnsNoContent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, "Member", cancellationToken);
        var channel = await CreateChannelAsync(ownerClient, workspace.Id, "general", "Public", cancellationToken);

        // act
        var response = await memberClient.PostAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/join", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Join_WhenAlreadyJoined_ReturnsNoContent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);

        // act
        var response = await client.PostAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/join", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Join_WhenPrivateChannel_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, "Member", cancellationToken);
        var channel = await CreateChannelAsync(ownerClient, workspace.Id, "secret", "Private", cancellationToken);

        // act
        var response = await memberClient.PostAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/join", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Join_WhenChannelDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.PostAsync($"/api/workspaces/{workspace.Id}/channels/{Guid.NewGuid()}/join", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Join_WhenChannelBelongsToAnotherWorkspace_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var first = await CreateWorkspaceAsync(client, cancellationToken, "First");
        var second = await CreateWorkspaceAsync(client, cancellationToken, "Second");
        var channel = await CreateChannelAsync(client, second.Id, "general", "Public", cancellationToken);

        // act
        var response = await client.PostAsync($"/api/workspaces/{first.Id}/channels/{channel.Id}/join", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Join_WhenUserIsNotWorkspaceMember_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var outsiderClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        await RegisterAsync(outsiderClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        var channel = await CreateChannelAsync(ownerClient, workspace.Id, "general", "Public", cancellationToken);

        // act
        var response = await outsiderClient.PostAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/join", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
