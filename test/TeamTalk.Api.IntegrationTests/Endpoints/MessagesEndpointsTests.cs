using System.Net;
using System.Net.Http.Json;
using TeamTalk.Api.IntegrationTests.Infrastructure;
using static TeamTalk.Api.IntegrationTests.Infrastructure.ApiTestHelper;

namespace TeamTalk.Api.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
public class MessagesEndpointsTests
{
    private const int MaxContentLength = 4000;

    private readonly ApiWebApplicationFactory _factory;

    public MessagesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Send_WithValidContent_ReturnsMessage()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var owner = await RegisterAsync(client, cancellationToken, "Alice");
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages",
            new SendMessageRequestDto { Content = "hello team" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<MessageDto>(cancellationToken);
        Assert.NotNull(message);
        Assert.Equal("hello team", message!.Content);
        Assert.Equal(channel.Id, message.ChannelId);
        Assert.Equal(owner.UserId, message.SenderId);
        Assert.Equal("Alice", message.SenderDisplayName);
        Assert.False(message.IsDeleted);
        Assert.Null(message.EditedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Send_WithBlankContent_ReturnsBadRequest(string content)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages",
            new SendMessageRequestDto { Content = content },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Send_WithContentExceedingMaxLength_ReturnsBadRequest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages",
            new SendMessageRequestDto { Content = new string('a', MaxContentLength + 1) },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Send_WhenChannelDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{Guid.NewGuid()}/messages",
            new SendMessageRequestDto { Content = "hello" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Send_WhenPrivateChannelAndUserIsNotMember_ReturnsForbidden()
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
        var response = await memberClient.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages",
            new SendMessageRequestDto { Content = "hello" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Send_WhenUserIsNotWorkspaceMember_ReturnsForbidden()
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
        var response = await outsiderClient.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages",
            new SendMessageRequestDto { Content = "hello" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_WithoutPageSize_ReturnsMessagesNewestFirst()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);
        await SendMessageAsync(client, workspace.Id, channel.Id, "first", cancellationToken);
        await SendMessageAsync(client, workspace.Id, channel.Id, "second", cancellationToken);

        // act
        var response = await client.GetAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = await response.Content.ReadFromJsonAsync<ChannelMessagesDto>(cancellationToken);
        Assert.Equal(["second", "first"], history!.Items.Select(m => m.Content).ToArray());
        Assert.Null(history.NextCursor);
    }

    [Fact]
    public async Task GetHistory_WhenMoreMessagesThanPageSize_ReturnsCursorForNextPage()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);
        await SendMessageAsync(client, workspace.Id, channel.Id, "one", cancellationToken);
        await SendMessageAsync(client, workspace.Id, channel.Id, "two", cancellationToken);
        await SendMessageAsync(client, workspace.Id, channel.Id, "three", cancellationToken);
        var firstPageResponse = await client.GetAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages?pageSize=2", cancellationToken);
        var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<ChannelMessagesDto>(cancellationToken);
        Assert.NotNull(firstPage!.NextCursor);

        // act
        var cursor = Uri.EscapeDataString(firstPage.NextCursor!.Value.ToString("O"));
        var secondPageResponse = await client.GetAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages?pageSize=2&cursor={cursor}", cancellationToken);

        // assert
        Assert.Equal(["three", "two"], firstPage.Items.Select(m => m.Content).ToArray());
        Assert.Equal(HttpStatusCode.OK, secondPageResponse.StatusCode);
        var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<ChannelMessagesDto>(cancellationToken);
        Assert.Equal(["one"], secondPage!.Items.Select(m => m.Content).ToArray());
        Assert.Null(secondPage.NextCursor);
    }

    [Fact]
    public async Task GetHistory_WhenChannelIsEmpty_ReturnsEmptyItems()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);

        // act
        var response = await client.GetAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages?pageSize=10", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = await response.Content.ReadFromJsonAsync<ChannelMessagesDto>(cancellationToken);
        Assert.Empty(history!.Items);
        Assert.Null(history.NextCursor);
    }

    [Fact]
    public async Task GetHistory_WhenPrivateChannelAndUserIsNotMember_ReturnsForbidden()
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
        var response = await memberClient.GetAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_WhenChannelDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.GetAsync($"/api/workspaces/{workspace.Id}/channels/{Guid.NewGuid()}/messages", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Edit_WhenAuthor_ReturnsMessageWithEditedTimestamp()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);
        var message = await SendMessageAsync(client, workspace.Id, channel.Id, "original", cancellationToken);

        // act
        var response = await client.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{message.Id}",
            new EditMessageRequestDto { Content = "updated" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var edited = await response.Content.ReadFromJsonAsync<MessageDto>(cancellationToken);
        Assert.Equal("updated", edited!.Content);
        Assert.NotNull(edited.EditedAtUtc);
    }

    [Fact]
    public async Task Edit_WithBlankContent_ReturnsBadRequest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);
        var message = await SendMessageAsync(client, workspace.Id, channel.Id, "original", cancellationToken);

        // act
        var response = await client.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{message.Id}",
            new EditMessageRequestDto { Content = "  " },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Edit_WhenUserIsNotAuthor_ReturnsForbidden()
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
        var message = await SendMessageAsync(ownerClient, workspace.Id, channel.Id, "original", cancellationToken);

        // act
        var response = await memberClient.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{message.Id}",
            new EditMessageRequestDto { Content = "hijacked" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Edit_WhenMessageDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);

        // act
        var response = await client.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{Guid.NewGuid()}",
            new EditMessageRequestDto { Content = "updated" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Edit_WhenMessageIsDeleted_ReturnsBadRequest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);
        var message = await SendMessageAsync(client, workspace.Id, channel.Id, "original", cancellationToken);
        (await client.DeleteAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{message.Id}", cancellationToken)).EnsureSuccessStatusCode();

        // act
        var response = await client.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{message.Id}",
            new EditMessageRequestDto { Content = "updated" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenAuthor_ReturnsNoContentAndHistoryShowsPlaceholder()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);
        var message = await SendMessageAsync(client, workspace.Id, channel.Id, "secret text", cancellationToken);

        // act
        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{message.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var historyResponse = await client.GetAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages", cancellationToken);
        var history = await historyResponse.Content.ReadFromJsonAsync<ChannelMessagesDto>(cancellationToken);
        var deleted = Assert.Single(history!.Items);
        Assert.True(deleted.IsDeleted);
        Assert.Equal("[deleted]", deleted.Content);
    }

    [Fact]
    public async Task Delete_WhenAdminDeletesAnotherUsersMessage_ReturnsNoContent()
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
        var message = await SendMessageAsync(memberClient, workspace.Id, channel.Id, "spam", cancellationToken);

        // act
        var response = await ownerClient.DeleteAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{message.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenNonAuthorIsNotAdmin_ReturnsForbidden()
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
        var message = await SendMessageAsync(ownerClient, workspace.Id, channel.Id, "keep me", cancellationToken);

        // act
        var response = await memberClient.DeleteAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{message.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenAlreadyDeleted_ReturnsBadRequest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);
        var message = await SendMessageAsync(client, workspace.Id, channel.Id, "bye", cancellationToken);
        (await client.DeleteAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{message.Id}", cancellationToken)).EnsureSuccessStatusCode();

        // act
        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{message.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenMessageDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var channel = await CreateChannelAsync(client, workspace.Id, "general", "Public", cancellationToken);

        // act
        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{Guid.NewGuid()}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
