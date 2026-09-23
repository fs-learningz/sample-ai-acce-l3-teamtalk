using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamTalk.Api.IntegrationTests.Infrastructure;

namespace TeamTalk.Api.IntegrationTests.Flows;

public class WorkspaceManagementFlowTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public WorkspaceManagementFlowTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullFlow_RemoveMemberThenReInviteThenLeaveThenDeleteWorkspace_Succeeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();

        var owner = await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);

        var createWorkspaceResponse = await ownerClient.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequestDto { Name = "Engineering" },
            cancellationToken);
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspace = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.NotNull(workspace);

        await InviteAndAcceptAsync(ownerClient, memberClient, workspace!.Id, member.Email, cancellationToken);

        var membersResponse = await ownerClient.GetAsync($"/api/workspaces/{workspace.Id}/members", cancellationToken);
        membersResponse.EnsureSuccessStatusCode();
        var members = await membersResponse.Content.ReadFromJsonAsync<List<WorkspaceMemberDto>>(cancellationToken);
        Assert.NotNull(members);
        Assert.Equal(2, members!.Count);

        var removeResponse = await ownerClient.DeleteAsync(
            $"/api/workspaces/{workspace.Id}/members/{member.UserId}",
            cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var membersAfterRemoveResponse = await ownerClient.GetAsync($"/api/workspaces/{workspace.Id}/members", cancellationToken);
        membersAfterRemoveResponse.EnsureSuccessStatusCode();
        var membersAfterRemove = await membersAfterRemoveResponse.Content.ReadFromJsonAsync<List<WorkspaceMemberDto>>(cancellationToken);
        Assert.NotNull(membersAfterRemove);
        Assert.Single(membersAfterRemove!);

        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, cancellationToken);

        var leaveResponse = await memberClient.PostAsync($"/api/workspaces/{workspace.Id}/members/leave", null, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, leaveResponse.StatusCode);

        var membersAfterLeaveResponse = await ownerClient.GetAsync($"/api/workspaces/{workspace.Id}/members", cancellationToken);
        membersAfterLeaveResponse.EnsureSuccessStatusCode();
        var membersAfterLeave = await membersAfterLeaveResponse.Content.ReadFromJsonAsync<List<WorkspaceMemberDto>>(cancellationToken);
        Assert.NotNull(membersAfterLeave);
        Assert.Single(membersAfterLeave!);

        var deleteWorkspaceResponse = await ownerClient.DeleteAsync($"/api/workspaces/{workspace.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteWorkspaceResponse.StatusCode);

        var getDeletedWorkspaceResponse = await ownerClient.GetAsync($"/api/workspaces/{workspace.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getDeletedWorkspaceResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_WhenCallerIsAdminButNotCreator_Succeeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var adminClient = _factory.CreateClient();

        var owner = await RegisterAsync(ownerClient, cancellationToken);
        var admin = await RegisterAsync(adminClient, cancellationToken);

        var createWorkspaceResponse = await ownerClient.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequestDto { Name = "Engineering" },
            cancellationToken);
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspace = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.NotNull(workspace);

        await InviteAndAcceptAsync(ownerClient, adminClient, workspace!.Id, admin.Email, cancellationToken, role: "Admin");

        var deleteResponse = await adminClient.DeleteAsync($"/api/workspaces/{workspace.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_WhenCallerIsMemberNotAdmin_ReturnsForbidden()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();

        var owner = await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);

        var createWorkspaceResponse = await ownerClient.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequestDto { Name = "Engineering" },
            cancellationToken);
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspace = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.NotNull(workspace);

        await InviteAndAcceptAsync(ownerClient, memberClient, workspace!.Id, member.Email, cancellationToken);

        var deleteResponse = await memberClient.DeleteAsync($"/api/workspaces/{workspace.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task LeaveAsync_WhenCallerIsOnlyAdmin_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);

        var createWorkspaceResponse = await ownerClient.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequestDto { Name = "Engineering" },
            cancellationToken);
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspace = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.NotNull(workspace);

        var leaveResponse = await ownerClient.PostAsync($"/api/workspaces/{workspace!.Id}/members/leave", null, cancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, leaveResponse.StatusCode);
    }

    [Fact]
    public async Task LeaveAsync_WhenTwoAdminsLeaveConcurrently_OnlyOneSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var adminClient = _factory.CreateClient();

        var owner = await RegisterAsync(ownerClient, cancellationToken);
        var admin = await RegisterAsync(adminClient, cancellationToken);

        var createWorkspaceResponse = await ownerClient.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequestDto { Name = "Engineering" },
            cancellationToken);
        createWorkspaceResponse.EnsureSuccessStatusCode();
        var workspace = await createWorkspaceResponse.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.NotNull(workspace);

        await InviteAndAcceptAsync(ownerClient, adminClient, workspace!.Id, admin.Email, cancellationToken, role: "Admin");

        var leaveTasks = new[]
        {
            ownerClient.PostAsync($"/api/workspaces/{workspace.Id}/members/leave", null, cancellationToken),
            adminClient.PostAsync($"/api/workspaces/{workspace.Id}/members/leave", null, cancellationToken),
        };
        var leaveResponses = await Task.WhenAll(leaveTasks);

        Assert.Single(leaveResponses, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(leaveResponses, r => r.StatusCode == HttpStatusCode.Conflict);

        var membersResponse = await ownerClient.GetAsync($"/api/workspaces/{workspace.Id}/members", cancellationToken);
        if (membersResponse.StatusCode == HttpStatusCode.OK)
        {
            var members = await membersResponse.Content.ReadFromJsonAsync<List<WorkspaceMemberDto>>(cancellationToken);
            Assert.NotNull(members);
            Assert.Single(members!);
        }
    }

    private static async Task InviteAndAcceptAsync(
        HttpClient ownerClient,
        HttpClient memberClient,
        Guid workspaceId,
        string memberEmail,
        CancellationToken cancellationToken,
        string role = "Member")
    {
        var inviteResponse = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/invitations",
            new InviteMemberRequestDto { Email = memberEmail, Role = role },
            cancellationToken);
        inviteResponse.EnsureSuccessStatusCode();
        var invitation = await inviteResponse.Content.ReadFromJsonAsync<InvitationDto>(cancellationToken);
        Assert.NotNull(invitation);

        var acceptResponse = await memberClient.PostAsync($"/api/invitations/{invitation!.Id}/accept", null, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, acceptResponse.StatusCode);
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

    private class WorkspaceDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public Guid CreatedByUserId { get; init; }
        public DateTime CreatedAtUtc { get; init; }
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

    private class WorkspaceMemberDto
    {
        public Guid Id { get; init; }
        public Guid WorkspaceId { get; init; }
        public Guid UserId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public DateTime JoinedAtUtc { get; init; }
    }
}
