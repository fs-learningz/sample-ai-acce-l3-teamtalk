using System.Net;
using System.Net.Http.Json;
using TeamTalk.Api.IntegrationTests.Infrastructure;
using static TeamTalk.Api.IntegrationTests.Infrastructure.ApiTestHelper;

namespace TeamTalk.Api.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
public class WorkspaceInvitationsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public WorkspaceInvitationsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Invite_WithValidRequest_ReturnsPendingInvitationWithNormalizedEmail()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var email = $"Invitee-{Guid.NewGuid():N}@Example.com";

        // act
        var response = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/invitations",
            new InviteMemberRequestDto { Email = $"  {email}  ", Role = "Member" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invitation = await response.Content.ReadFromJsonAsync<InvitationDto>(cancellationToken);
        Assert.NotNull(invitation);
        Assert.Equal(workspace.Id, invitation!.WorkspaceId);
        Assert.Equal(email.ToLowerInvariant(), invitation.Email);
        Assert.Equal("Member", invitation.Role);
        Assert.Equal("Pending", invitation.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public async Task Invite_WithInvalidEmail_ReturnsBadRequest(string email)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/invitations",
            new InviteMemberRequestDto { Email = email, Role = "Member" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invite_WhenPendingInvitationAlreadyExists_ReturnsConflict()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var email = $"invitee-{Guid.NewGuid():N}@example.com";
        await InviteAsync(client, workspace.Id, email, "Member", cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/invitations",
            new InviteMemberRequestDto { Email = email, Role = "Member" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Invite_WhenTargetIsAlreadyAMember_ReturnsConflict()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var owner = await RegisterAsync(ownerClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);

        // act
        var response = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/invitations",
            new InviteMemberRequestDto { Email = owner.Email, Role = "Member" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Invite_WhenInviterIsNotAdmin_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, "Member", cancellationToken);

        // act
        var response = await memberClient.PostAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/invitations",
            new InviteMemberRequestDto { Email = $"x-{Guid.NewGuid():N}@example.com", Role = "Member" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Invite_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            $"/api/workspaces/{Guid.NewGuid()}/invitations",
            new InviteMemberRequestDto { Email = "someone@example.com", Role = "Member" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetForWorkspace_WhenAdmin_ReturnsInvitations()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var invitation = await InviteAsync(client, workspace.Id, $"a-{Guid.NewGuid():N}@example.com", "Member", cancellationToken);

        // act
        var response = await client.GetAsync($"/api/workspaces/{workspace.Id}/invitations", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invitations = await response.Content.ReadFromJsonAsync<List<InvitationDto>>(cancellationToken);
        Assert.Contains(invitations!, i => i.Id == invitation.Id);
    }

    [Fact]
    public async Task GetForWorkspace_WhenUserIsNotAdmin_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, "Member", cancellationToken);

        // act
        var response = await memberClient.GetAsync($"/api/workspaces/{workspace.Id}/invitations", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetForWorkspace_WhenUserIsNotMember_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var outsiderClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        await RegisterAsync(outsiderClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);

        // act
        var response = await outsiderClient.GetAsync($"/api/workspaces/{workspace.Id}/invitations", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_WhenPending_ReturnsNoContentAndStatusIsRevoked()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var invitation = await InviteAsync(client, workspace.Id, $"a-{Guid.NewGuid():N}@example.com", "Member", cancellationToken);

        // act
        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}/invitations/{invitation.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var listResponse = await client.GetAsync($"/api/workspaces/{workspace.Id}/invitations", cancellationToken);
        var invitations = await listResponse.Content.ReadFromJsonAsync<List<InvitationDto>>(cancellationToken);
        Assert.Equal("Revoked", invitations!.Single(i => i.Id == invitation.Id).Status);
    }

    [Fact]
    public async Task Revoke_WhenAlreadyRevoked_ReturnsConflict()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        var invitation = await InviteAsync(client, workspace.Id, $"a-{Guid.NewGuid():N}@example.com", "Member", cancellationToken);
        (await client.DeleteAsync($"/api/workspaces/{workspace.Id}/invitations/{invitation.Id}", cancellationToken)).EnsureSuccessStatusCode();

        // act
        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}/invitations/{invitation.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_WhenInvitationDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}/invitations/{Guid.NewGuid()}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_WhenInvitationBelongsToAnotherWorkspace_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken, "First");
        var otherWorkspace = await CreateWorkspaceAsync(client, cancellationToken, "Second");
        var invitation = await InviteAsync(client, otherWorkspace.Id, $"a-{Guid.NewGuid():N}@example.com", "Member", cancellationToken);

        // act
        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}/invitations/{invitation.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_WhenUserIsNotAdmin_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, "Member", cancellationToken);
        var invitation = await InviteAsync(ownerClient, workspace.Id, $"a-{Guid.NewGuid():N}@example.com", "Member", cancellationToken);

        // act
        var response = await memberClient.DeleteAsync($"/api/workspaces/{workspace.Id}/invitations/{invitation.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
