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

    private sealed class PreferencesDto
    {
        public bool IsPinned { get; init; }
        public bool IsMuted { get; init; }
    }

    private sealed class ChannelListItemDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsPinned { get; init; }
        public bool IsMuted { get; init; }
        public bool HasUnread { get; init; }
    }

    private sealed class ChannelDetailsDto
    {
        public Guid Id { get; init; }
        public Guid CreatedByUserId { get; init; }
        public string CreatedByDisplayName { get; init; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; }
        public int MemberCount { get; init; }
        public bool IsMember { get; init; }
    }

    [Fact]
    public async Task GetDetails_ReturnsCreatorNameAndCreationDate()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var owner = await RegisterAsync(client, cancellationToken, "Ada Lovelace");
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);

        // act
        var response = await client.GetAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var details = await response.Content.ReadFromJsonAsync<ChannelDetailsDto>(cancellationToken);
        Assert.NotNull(details);
        Assert.Equal(owner.UserId, details!.CreatedByUserId);
        Assert.Equal("Ada Lovelace", details.CreatedByDisplayName);
        // Postgres stores microseconds, so the round-tripped timestamp can differ by sub-microsecond ticks.
        Assert.InRange((details.CreatedAtUtc - channel.CreatedAtUtc).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
        Assert.Equal(1, details.MemberCount);
        Assert.True(details.IsMember);
    }

    [Fact]
    public async Task GetDetails_ForPrivateChannelNonMember_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        var channel = await CreateChannelAsync(ownerClient, workspace.Id, "secret", "Private", cancellationToken);
        var memberClient = _factory.CreateClient();
        var member = await RegisterAsync(memberClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, "Member", cancellationToken);

        // act
        var response = await memberClient.GetAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePreferences_PinsAndMutesChannelAndListsPinnedFirst()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        await CreateChannelAsync(client, workspace.Id, "alpha", "Public", cancellationToken);
        var zulu = await CreateChannelAsync(client, workspace.Id, "zulu", "Public", cancellationToken);

        // act
        var response = await client.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{zulu.Id}/preferences",
            new PreferencesDto { IsPinned = true, IsMuted = true },
            cancellationToken);
        var list = await client.GetFromJsonAsync<List<ChannelListItemDto>>(
            $"/api/workspaces/{workspace.Id}/channels",
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(list);
        Assert.Equal(new[] { "zulu", "alpha" }, list!.Select(c => c.Name).ToArray());
        Assert.True(list[0].IsPinned);
        Assert.True(list[0].IsMuted);
        Assert.False(list[1].IsPinned);
    }

    [Fact]
    public async Task MarkRead_ClearsUnreadFlagForMessagesFromOthers()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        var channel = await CreateChannelAsync(ownerClient, workspace.Id, "general", "Public", cancellationToken);
        var memberClient = _factory.CreateClient();
        var member = await RegisterAsync(memberClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, "Member", cancellationToken);
        await SendMessageAsync(ownerClient, workspace.Id, channel.Id, "hello", cancellationToken);
        var listUrl = $"/api/workspaces/{workspace.Id}/channels";

        // act
        var before = await memberClient.GetFromJsonAsync<List<ChannelListItemDto>>(listUrl, cancellationToken);
        var markResponse = await memberClient.PostAsync($"{listUrl}/{channel.Id}/read", null, cancellationToken);
        var after = await memberClient.GetFromJsonAsync<List<ChannelListItemDto>>(listUrl, cancellationToken);
        var senderView = await ownerClient.GetFromJsonAsync<List<ChannelListItemDto>>(listUrl, cancellationToken);

        // assert
        Assert.True(before!.Single().HasUnread);
        Assert.Equal(HttpStatusCode.NoContent, markResponse.StatusCode);
        Assert.False(after!.Single().HasUnread);
        Assert.False(senderView!.Single().HasUnread);
    }

    [Fact]
    public async Task Leave_WhenChannelMember_RemovesMembershipFromPrivateChannel()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "secret", "Private", cancellationToken);

        // act
        var response = await client.PostAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/leave",
            null,
            cancellationToken);
        var list = await client.GetFromJsonAsync<List<ChannelListItemDto>>(
            $"/api/workspaces/{workspace.Id}/channels",
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(list!);
    }

    [Fact]
    public async Task Delete_AsWorkspaceAdmin_RemovesChannelAndItsMessages()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);
        await SendMessageAsync(client, workspace.Id, channel.Id, "hello", cancellationToken);
        await client.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/preferences",
            new PreferencesDto { IsPinned = true, IsMuted = false },
            cancellationToken);

        // act
        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}", cancellationToken);
        var list = await client.GetFromJsonAsync<List<ChannelListItemDto>>(
            $"/api/workspaces/{workspace.Id}/channels",
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(list!);
    }

    [Fact]
    public async Task Delete_AsRegularMember_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        var channel = await CreateChannelAsync(ownerClient, workspace.Id, "general", "Public", cancellationToken);
        var memberClient = _factory.CreateClient();
        var member = await RegisterAsync(memberClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, "Member", cancellationToken);

        // act
        var response = await memberClient.DeleteAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
