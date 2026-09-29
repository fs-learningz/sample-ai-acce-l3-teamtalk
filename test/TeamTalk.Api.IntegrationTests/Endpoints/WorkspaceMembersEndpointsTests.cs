using System.Net;
using System.Net.Http.Json;
using TeamTalk.Api.IntegrationTests.Infrastructure;
using static TeamTalk.Api.IntegrationTests.Infrastructure.ApiTestHelper;

namespace TeamTalk.Api.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
public class WorkspaceMembersEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public WorkspaceMembersEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetForWorkspace_WhenMember_ReturnsAllMembersWithRoles()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();
        var owner = await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, "Member", cancellationToken);

        // act
        var response = await memberClient.GetAsync($"/api/workspaces/{workspace.Id}/members", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadFromJsonAsync<List<MemberDto>>(cancellationToken);
        Assert.NotNull(members);
        Assert.Equal(2, members!.Count);
        Assert.Equal("Admin", members.Single(m => m.UserId == owner.UserId).Role);
        Assert.Equal("Member", members.Single(m => m.UserId == member.UserId).Role);
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
        var response = await outsiderClient.GetAsync($"/api/workspaces/{workspace.Id}/members", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetForWorkspace_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.GetAsync($"/api/workspaces/{Guid.NewGuid()}/members", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Leave_WhenRegularMember_ReturnsNoContentAndMembershipIsRemoved()
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
        var response = await memberClient.PostAsync($"/api/workspaces/{workspace.Id}/members/leave", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await memberClient.GetAsync($"/api/workspaces/{workspace.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
    }

    [Fact]
    public async Task Leave_WhenSoleAdmin_ReturnsConflict()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.PostAsync($"/api/workspaces/{workspace.Id}/members/leave", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Leave_WhenUserIsNotMember_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var outsiderClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        await RegisterAsync(outsiderClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);

        // act
        var response = await outsiderClient.PostAsync($"/api/workspaces/{workspace.Id}/members/leave", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Leave_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.PostAsync($"/api/workspaces/{Guid.NewGuid()}/members/leave", null, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Remove_WhenAdminRemovesMember_ReturnsNoContent()
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
        var response = await ownerClient.DeleteAsync($"/api/workspaces/{workspace.Id}/members/{member.UserId}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var membersResponse = await ownerClient.GetAsync($"/api/workspaces/{workspace.Id}/members", cancellationToken);
        var members = await membersResponse.Content.ReadFromJsonAsync<List<MemberDto>>(cancellationToken);
        Assert.DoesNotContain(members!, m => m.UserId == member.UserId);
    }

    [Fact]
    public async Task Remove_WhenAdminTargetsSelf_ReturnsBadRequest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var owner = await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}/members/{owner.UserId}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Remove_WhenActorIsNotAdmin_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();
        var owner = await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, "Member", cancellationToken);

        // act
        var response = await memberClient.DeleteAsync($"/api/workspaces/{workspace.Id}/members/{owner.UserId}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Remove_WhenTargetIsNotAMember_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}/members/{Guid.NewGuid()}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_WhenAdminPromotesMember_ReturnsNoContentAndRoleIsUpdated()
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
        var response = await ownerClient.PatchAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/members/{member.UserId}/role",
            new ChangeMemberRoleRequestDto { Role = "Admin" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var membersResponse = await ownerClient.GetAsync($"/api/workspaces/{workspace.Id}/members", cancellationToken);
        var members = await membersResponse.Content.ReadFromJsonAsync<List<MemberDto>>(cancellationToken);
        Assert.Equal("Admin", members!.Single(m => m.UserId == member.UserId).Role);
    }

    [Fact]
    public async Task ChangeRole_WhenDemotingSoleAdmin_ReturnsConflict()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var owner = await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.PatchAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/members/{owner.UserId}/role",
            new ChangeMemberRoleRequestDto { Role = "Member" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_WhenActorIsNotAdmin_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();
        var owner = await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, "Member", cancellationToken);

        // act
        var response = await memberClient.PatchAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/members/{owner.UserId}/role",
            new ChangeMemberRoleRequestDto { Role = "Member" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_WhenTargetIsNotAMember_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.PatchAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/members/{Guid.NewGuid()}/role",
            new ChangeMemberRoleRequestDto { Role = "Admin" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_WithUnknownRoleName_ReturnsBadRequest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var owner = await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.PatchAsJsonAsync(
            $"/api/workspaces/{workspace.Id}/members/{owner.UserId}/role",
            new ChangeMemberRoleRequestDto { Role = "SuperUser" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
