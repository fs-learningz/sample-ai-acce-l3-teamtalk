using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamTalk.Api.IntegrationTests.Infrastructure;

namespace TeamTalk.Api.IntegrationTests.Flows;

public class WorkspaceSettingsFlowTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public WorkspaceSettingsFlowTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateAsync_WhenAdmin_UpdatesNameAndDescription()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}",
            new UpdateWorkspaceRequestDto { Name = "Renamed", Description = "New description" },
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // assert
        var updated = await updateResponse.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("Renamed", updated!.Name);
        Assert.Equal("New description", updated.Description);
        Assert.Equal(1, updated.MemberCount);
        Assert.False(string.IsNullOrEmpty(updated.CreatedByDisplayName));
    }

    [Fact]
    public async Task UpdateAsync_WhenMember_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();

        await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);

        // act
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, cancellationToken);

        // assert
        var updateResponse = await memberClient.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}",
            new UpdateWorkspaceRequestDto { Name = "Renamed", Description = null },
            cancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task IconLifecycle_UploadFetchAndRemove_Succeeds()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        using var content = new MultipartFormDataContent();
        var iconBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4 };
        var fileContent = new ByteArrayContent(iconBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "icon.png");

        var uploadResponse = await client.PutAsync($"/api/workspaces/{workspace.Id}/icon", content, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var updated = await uploadResponse.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.NotNull(updated);
        Assert.True(updated!.HasIcon);

        var getIconResponse = await client.GetAsync($"/api/workspaces/{workspace.Id}/icon", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, getIconResponse.StatusCode);
        Assert.Equal("image/png", getIconResponse.Content.Headers.ContentType?.MediaType);
        var downloadedBytes = await getIconResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        Assert.Equal(iconBytes, downloadedBytes);

        // act
        var removeResponse = await client.DeleteAsync($"/api/workspaces/{workspace.Id}/icon", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        var afterRemove = await removeResponse.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.NotNull(afterRemove);
        Assert.False(afterRemove!.HasIcon);

        // assert
        var getIconAfterRemoveResponse = await client.GetAsync($"/api/workspaces/{workspace.Id}/icon", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getIconAfterRemoveResponse.StatusCode);
    }

    [Fact]
    public async Task ChangeRoleAsync_WhenPromotingThenDemoting_LastAdminIsProtected()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();

        var owner = await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);

        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, cancellationToken);

        var promoteResponse = await ownerClient.PatchAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/members/{member.UserId}/role",
            new ChangeMemberRoleRequestDto { Role = "Admin" },
            cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, promoteResponse.StatusCode);

        // act
        var demoteOwnerResponse = await ownerClient.PatchAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/members/{owner.UserId}/role",
            new ChangeMemberRoleRequestDto { Role = "Member" },
            cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, demoteOwnerResponse.StatusCode);

        // assert
        var demoteLastAdminResponse = await memberClient.PatchAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/members/{member.UserId}/role",
            new ChangeMemberRoleRequestDto { Role = "Member" },
            cancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, demoteLastAdminResponse.StatusCode);
    }

    private static async Task InviteAndAcceptAsync(
        HttpClient ownerClient,
        HttpClient memberClient,
        Guid workspaceId,
        string memberEmail,
        CancellationToken cancellationToken)
    {
        var inviteResponse = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/invitations",
            new InviteMemberRequestDto { Email = memberEmail, Role = "Member" },
            cancellationToken);
        inviteResponse.EnsureSuccessStatusCode();
        var invitation = await inviteResponse.Content.ReadFromJsonAsync<InvitationDto>(cancellationToken);
        Assert.NotNull(invitation);

        var acceptResponse = await memberClient.PostAsync($"/api/invitations/{invitation!.Id}/accept", null, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, acceptResponse.StatusCode);
    }

    private static async Task<WorkspaceDto> CreateWorkspaceAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var createResponse = await client.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequestDto { Name = "Engineering" },
            cancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var workspace = await createResponse.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.NotNull(workspace);

        return workspace!;
    }

    private static async Task<AuthResponseDto> RegisterAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto { Email = email, DisplayName = "Test User", Password = "Passw0rd!" },
            cancellationToken);
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken);
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        return auth;
    }

    private class RegisterRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    private class AuthResponseDto
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Token { get; init; } = string.Empty;
    }

    private class CreateWorkspaceRequestDto
    {
        public string Name { get; init; } = string.Empty;
    }

    private class UpdateWorkspaceRequestDto
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }

    private class ChangeMemberRoleRequestDto
    {
        public string Role { get; init; } = string.Empty;
    }

    private class WorkspaceDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool HasIcon { get; init; }
        public Guid CreatedByUserId { get; init; }
        public string? CreatedByDisplayName { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public int? MemberCount { get; init; }
    }

    private class InviteMemberRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
    }

    private class InvitationDto
    {
        public Guid Id { get; init; }
        public Guid WorkspaceId { get; init; }
        public string WorkspaceName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public Guid InvitedByUserId { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}
