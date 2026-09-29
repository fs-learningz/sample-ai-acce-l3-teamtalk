using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TeamTalk.Api.IntegrationTests.Infrastructure;

public static class ApiTestHelper
{
    public const string DefaultPassword = "Passw0rd!";

    public static async Task<AuthResponseDto> RegisterAsync(
        HttpClient client,
        CancellationToken cancellationToken,
        string displayName = "Test User")
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto { Email = email, DisplayName = displayName, Password = DefaultPassword },
            cancellationToken);
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken);
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        return auth;
    }

    public static async Task<WorkspaceDto> CreateWorkspaceAsync(
        HttpClient client,
        CancellationToken cancellationToken,
        string name = "Engineering")
    {
        var createResponse = await client.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequestDto { Name = name },
            cancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.NotNull(workspace);

        return workspace!;
    }

    public static async Task<ChannelDto> CreateChannelAsync(
        HttpClient client,
        Guid workspaceId,
        string name,
        string type,
        CancellationToken cancellationToken)
    {
        var createResponse = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/channels",
            new CreateChannelRequestDto { Name = name, Type = type },
            cancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var channel = await createResponse.Content.ReadFromJsonAsync<ChannelDto>(cancellationToken);
        Assert.NotNull(channel);

        return channel!;
    }

    public static async Task<InvitationDto> InviteAsync(
        HttpClient ownerClient,
        Guid workspaceId,
        string email,
        string role,
        CancellationToken cancellationToken)
    {
        var inviteResponse = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/invitations",
            new InviteMemberRequestDto { Email = email, Role = role },
            cancellationToken);
        inviteResponse.EnsureSuccessStatusCode();
        var invitation = await inviteResponse.Content.ReadFromJsonAsync<InvitationDto>(cancellationToken);
        Assert.NotNull(invitation);

        return invitation!;
    }

    public static async Task InviteAndAcceptAsync(
        HttpClient ownerClient,
        HttpClient memberClient,
        Guid workspaceId,
        string memberEmail,
        string role,
        CancellationToken cancellationToken)
    {
        var invitation = await InviteAsync(ownerClient, workspaceId, memberEmail, role, cancellationToken);

        var acceptResponse = await memberClient.PostAsync($"/api/invitations/{invitation.Id}/accept", null, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, acceptResponse.StatusCode);
    }

    public static async Task<MessageDto> SendMessageAsync(
        HttpClient client,
        Guid workspaceId,
        Guid channelId,
        string content,
        CancellationToken cancellationToken)
    {
        var sendResponse = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/channels/{channelId}/messages",
            new SendMessageRequestDto { Content = content },
            cancellationToken);
        sendResponse.EnsureSuccessStatusCode();
        var message = await sendResponse.Content.ReadFromJsonAsync<MessageDto>(cancellationToken);
        Assert.NotNull(message);

        return message!;
    }

    public static MultipartFormDataContent CreateIconContent(byte[] bytes, string contentType, string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        return content;
    }

    public class RegisterRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    public class LoginRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Token { get; init; } = string.Empty;
    }

    public class CreateWorkspaceRequestDto
    {
        public string Name { get; init; } = string.Empty;
    }

    public class UpdateWorkspaceRequestDto
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }

    public class WorkspaceDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool HasIcon { get; init; }
        public Guid CreatedByUserId { get; init; }
        public string? CreatedByDisplayName { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public int? MemberCount { get; init; }
        public string? CurrentUserRole { get; init; }
    }

    public class CreateChannelRequestDto
    {
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
    }

    public class ChannelDto
    {
        public Guid Id { get; init; }
        public Guid WorkspaceId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public Guid CreatedByUserId { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }

    public class InviteMemberRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
    }

    public class InvitationDto
    {
        public Guid Id { get; init; }
        public Guid WorkspaceId { get; init; }
        public string WorkspaceName { get; init; } = string.Empty;
        public bool WorkspaceHasIcon { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public Guid InvitedByUserId { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }

    public class MemberDto
    {
        public Guid Id { get; init; }
        public Guid WorkspaceId { get; init; }
        public Guid UserId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public DateTime JoinedAtUtc { get; init; }
    }

    public class ChangeMemberRoleRequestDto
    {
        public string Role { get; init; } = string.Empty;
    }

    public class SendMessageRequestDto
    {
        public string Content { get; init; } = string.Empty;
    }

    public class EditMessageRequestDto
    {
        public string Content { get; init; } = string.Empty;
    }

    public class MessageDto
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

    public class ChannelMessagesDto
    {
        public List<MessageDto> Items { get; init; } = new();
        public DateTime? NextCursor { get; init; }
    }
}
