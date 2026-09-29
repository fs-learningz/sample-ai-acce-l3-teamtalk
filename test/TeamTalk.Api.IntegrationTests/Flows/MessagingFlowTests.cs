using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamTalk.Api.IntegrationTests.Infrastructure;

namespace TeamTalk.Api.IntegrationTests.Flows;

public class MessagingFlowTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public MessagingFlowTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullFlow_RegisterCreateWorkspaceCreateChannelSendEditDeleteMessage_Succeeds()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto { Email = email, DisplayName = "Test User", Password = "Passw0rd!" },
            cancellationToken);
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken);
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var createWorkspaceResponse = await client.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequestDto { Name = "Engineering" },
            cancellationToken);
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspace = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.NotNull(workspace);

        var createChannelResponse = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspace!.Id}/channels",
            new CreateChannelRequestDto { Name = "general", Type = "Public" },
            cancellationToken);
        createChannelResponse.EnsureSuccessStatusCode();
        var channel = await createChannelResponse.Content.ReadFromJsonAsync<ChannelDto>(cancellationToken);
        Assert.NotNull(channel);

        // act
        var sendResponse = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel!.Id}/messages",
            new SendMessageRequestDto { Content = "hello team" },
            cancellationToken);
        sendResponse.EnsureSuccessStatusCode();
        var message = await sendResponse.Content.ReadFromJsonAsync<MessageDto>(cancellationToken);
        Assert.NotNull(message);
        Assert.Equal("hello team", message!.Content);

        var editResponse = await client.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{message.Id}",
            new EditMessageRequestDto { Content = "hello team, edited" },
            cancellationToken);
        editResponse.EnsureSuccessStatusCode();
        var edited = await editResponse.Content.ReadFromJsonAsync<MessageDto>(cancellationToken);
        Assert.NotNull(edited);
        Assert.Equal("hello team, edited", edited!.Content);
        Assert.NotNull(edited.EditedAtUtc);

        var deleteResponse = await client.DeleteAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages/{message.Id}",
            cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // assert
        var historyResponse = await client.GetAsync(
            $"/api/workspaces/{workspace.Id}/channels/{channel.Id}/messages?pageSize=10",
            cancellationToken);
        historyResponse.EnsureSuccessStatusCode();
        var history = await historyResponse.Content.ReadFromJsonAsync<ChannelMessagesDto>(cancellationToken);
        Assert.NotNull(history);
        var deletedMessage = Assert.Single(history!.Items);
        Assert.True(deletedMessage.IsDeleted);
        Assert.Equal("[deleted]", deletedMessage.Content);
    }

    private class RegisterRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    private class CreateWorkspaceRequestDto
    {
        public string Name { get; init; } = string.Empty;
    }

    private class CreateChannelRequestDto
    {
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
    }

    private class SendMessageRequestDto
    {
        public string Content { get; init; } = string.Empty;
    }

    private class EditMessageRequestDto
    {
        public string Content { get; init; } = string.Empty;
    }

    private class AuthResponseDto
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Token { get; init; } = string.Empty;
    }

    private class WorkspaceDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public Guid CreatedByUserId { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }

    private class ChannelDto
    {
        public Guid Id { get; init; }
        public Guid WorkspaceId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public Guid CreatedByUserId { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }

    private class MessageDto
    {
        public Guid Id { get; init; }
        public Guid ChannelId { get; init; }
        public Guid SenderId { get; init; }
        public string SenderDisplayName { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public DateTime SentAtUtc { get; init; }
        public DateTime? EditedAtUtc { get; init; }
        public bool IsDeleted { get; init; }
    }

    private class ChannelMessagesDto
    {
        public List<MessageDto> Items { get; init; } = new();
        public DateTime? NextCursor { get; init; }
    }
}
