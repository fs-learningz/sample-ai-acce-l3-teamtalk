using System.Net;
using System.Net.Http.Json;
using TeamTalk.Api.IntegrationTests.Infrastructure;
using static TeamTalk.Api.IntegrationTests.Infrastructure.ApiTestHelper;

namespace TeamTalk.Api.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
public class InvitationsEndpointsTests
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4];

    private readonly ApiWebApplicationFactory _factory;

    public InvitationsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPendingForCurrentUser_ReturnsOnlyInvitationsAddressedToUser()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var inviteeClient = _factory.CreateClient();
        var otherClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var invitee = await RegisterAsync(inviteeClient, cancellationToken);
        var other = await RegisterAsync(otherClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        var mine = await InviteAsync(ownerClient, workspace.Id, invitee.Email, "Member", cancellationToken);
        await InviteAsync(ownerClient, workspace.Id, other.Email, "Member", cancellationToken);

        // act
        var response = await inviteeClient.GetAsync("/api/invitations", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invitations = await response.Content.ReadFromJsonAsync<List<InvitationDto>>(cancellationToken);
        var single = Assert.Single(invitations!);
        Assert.Equal(mine.Id, single.Id);
        Assert.Equal(workspace.Name, single.WorkspaceName);
    }

    [Fact]
    public async Task GetPendingForCurrentUser_WhenNoInvitations_ReturnsEmptyList()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.GetAsync("/api/invitations", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invitations = await response.Content.ReadFromJsonAsync<List<InvitationDto>>(cancellationToken);
        Assert.Empty(invitations!);
    }

    [Fact]
    public async Task Accept_WhenPendingAndEmailMatches_ReturnsNoContentAndUserBecomesMember()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var inviteeClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var invitee = await RegisterAsync(inviteeClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        var invitation = await InviteAsync(ownerClient, workspace.Id, invitee.Email, "Member", cancellationToken);

        // act
        var response = await inviteeClient.PostAsync($"/api/invitations/{invitation.Id}/accept", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await inviteeClient.GetAsync($"/api/workspaces/{workspace.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Accept_WhenEmailDoesNotMatch_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var strangerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        await RegisterAsync(strangerClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        var invitation = await InviteAsync(ownerClient, workspace.Id, $"someone-{Guid.NewGuid():N}@example.com", "Member", cancellationToken);

        // act
        var response = await strangerClient.PostAsync($"/api/invitations/{invitation.Id}/accept", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Accept_WhenAlreadyAccepted_ReturnsConflict()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var inviteeClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var invitee = await RegisterAsync(inviteeClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        var invitation = await InviteAsync(ownerClient, workspace.Id, invitee.Email, "Member", cancellationToken);
        (await inviteeClient.PostAsync($"/api/invitations/{invitation.Id}/accept", null, cancellationToken)).EnsureSuccessStatusCode();

        // act
        var response = await inviteeClient.PostAsync($"/api/invitations/{invitation.Id}/accept", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Accept_WhenInvitationDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.PostAsync($"/api/invitations/{Guid.NewGuid()}/accept", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Decline_WhenPendingAndEmailMatches_ReturnsNoContentAndUserIsNotMember()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var inviteeClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var invitee = await RegisterAsync(inviteeClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        var invitation = await InviteAsync(ownerClient, workspace.Id, invitee.Email, "Member", cancellationToken);

        // act
        var response = await inviteeClient.PostAsync($"/api/invitations/{invitation.Id}/decline", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await inviteeClient.GetAsync($"/api/workspaces/{workspace.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
        var pendingResponse = await inviteeClient.GetAsync("/api/invitations", cancellationToken);
        var pending = await pendingResponse.Content.ReadFromJsonAsync<List<InvitationDto>>(cancellationToken);
        Assert.Empty(pending!);
    }

    [Fact]
    public async Task Decline_WhenEmailDoesNotMatch_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var strangerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        await RegisterAsync(strangerClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        var invitation = await InviteAsync(ownerClient, workspace.Id, $"someone-{Guid.NewGuid():N}@example.com", "Member", cancellationToken);

        // act
        var response = await strangerClient.PostAsync($"/api/invitations/{invitation.Id}/decline", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Decline_WhenAlreadyDeclined_ReturnsConflict()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var inviteeClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var invitee = await RegisterAsync(inviteeClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        var invitation = await InviteAsync(ownerClient, workspace.Id, invitee.Email, "Member", cancellationToken);
        (await inviteeClient.PostAsync($"/api/invitations/{invitation.Id}/decline", null, cancellationToken)).EnsureSuccessStatusCode();

        // act
        var response = await inviteeClient.PostAsync($"/api/invitations/{invitation.Id}/decline", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkspaceIcon_WhenWorkspaceHasIcon_ReturnsFile()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var inviteeClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var invitee = await RegisterAsync(inviteeClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        using var content = CreateIconContent(PngBytes, "image/png", "icon.png");
        (await ownerClient.PutAsync($"/api/workspaces/{workspace.Id}/icon", content, cancellationToken)).EnsureSuccessStatusCode();
        var invitation = await InviteAsync(ownerClient, workspace.Id, invitee.Email, "Member", cancellationToken);

        // act
        var response = await inviteeClient.GetAsync($"/api/invitations/{invitation.Id}/workspace-icon", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngBytes, await response.Content.ReadAsByteArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task GetWorkspaceIcon_WhenWorkspaceHasNoIcon_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var inviteeClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var invitee = await RegisterAsync(inviteeClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        var invitation = await InviteAsync(ownerClient, workspace.Id, invitee.Email, "Member", cancellationToken);

        // act
        var response = await inviteeClient.GetAsync($"/api/invitations/{invitation.Id}/workspace-icon", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkspaceIcon_WhenEmailDoesNotMatch_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var strangerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        await RegisterAsync(strangerClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        using var content = CreateIconContent(PngBytes, "image/png", "icon.png");
        (await ownerClient.PutAsync($"/api/workspaces/{workspace.Id}/icon", content, cancellationToken)).EnsureSuccessStatusCode();
        var invitation = await InviteAsync(ownerClient, workspace.Id, $"someone-{Guid.NewGuid():N}@example.com", "Member", cancellationToken);

        // act
        var response = await strangerClient.GetAsync($"/api/invitations/{invitation.Id}/workspace-icon", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
