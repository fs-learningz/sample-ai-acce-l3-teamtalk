using System.Net;
using System.Net.Http.Json;
using TeamTalk.Api.IntegrationTests.Infrastructure;
using static TeamTalk.Api.IntegrationTests.Infrastructure.ApiTestHelper;

namespace TeamTalk.Api.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
public class WorkspacesEndpointsTests
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4];

    private readonly ApiWebApplicationFactory _factory;

    public WorkspacesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_WithValidName_ReturnsOkWithWorkspace()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var owner = await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequestDto { Name = "Engineering" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var workspace = await response.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.NotNull(workspace);
        Assert.NotEqual(Guid.Empty, workspace!.Id);
        Assert.Equal("Engineering", workspace.Name);
        Assert.Equal(owner.UserId, workspace.CreatedByUserId);
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

        // act
        var response = await client.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequestDto { Name = name },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNameExceedingMaxLength_ReturnsBadRequest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequestDto { Name = new string('a', 500) },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetForCurrentUser_ReturnsOnlyWorkspacesTheUserBelongsTo()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        var otherClient = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        await RegisterAsync(otherClient, cancellationToken);
        var mine = await CreateWorkspaceAsync(client, cancellationToken, "Mine");
        await CreateWorkspaceAsync(otherClient, cancellationToken, "Theirs");

        // act
        var response = await client.GetAsync("/api/workspaces", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var workspaces = await response.Content.ReadFromJsonAsync<List<WorkspaceDto>>(cancellationToken);
        Assert.NotNull(workspaces);
        var single = Assert.Single(workspaces!);
        Assert.Equal(mine.Id, single.Id);
    }

    [Fact]
    public async Task GetForCurrentUser_WhenUserHasNoWorkspaces_ReturnsEmptyList()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.GetAsync("/api/workspaces", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var workspaces = await response.Content.ReadFromJsonAsync<List<WorkspaceDto>>(cancellationToken);
        Assert.NotNull(workspaces);
        Assert.Empty(workspaces!);
    }

    [Fact]
    public async Task GetById_WhenMember_ReturnsWorkspaceWithRoleAndMemberCount()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken, "Owner");
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.GetAsync($"/api/workspaces/{workspace.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var found = await response.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.NotNull(found);
        Assert.Equal(workspace.Id, found!.Id);
        Assert.Equal("Admin", found.CurrentUserRole);
        Assert.Equal(1, found.MemberCount);
        Assert.Equal("Owner", found.CreatedByDisplayName);
    }

    [Fact]
    public async Task GetById_WhenUserIsNotMember_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var outsiderClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        await RegisterAsync(outsiderClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);

        // act
        var response = await outsiderClient.GetAsync($"/api/workspaces/{workspace.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.GetAsync($"/api/workspaces/{Guid.NewGuid()}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenIdIsNotAGuid_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.GetAsync("/api/workspaces/not-a-guid", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenAdmin_ReturnsUpdatedWorkspace()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}",
            new UpdateWorkspaceRequestDto { Name = "Renamed", Description = "About us" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.Equal("Renamed", updated!.Name);
        Assert.Equal("About us", updated.Description);
    }

    [Fact]
    public async Task Update_WithBlankName_ReturnsBadRequest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}",
            new UpdateWorkspaceRequestDto { Name = " " },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.PutAsJsonAsync(
            $"/api/workspaces/{Guid.NewGuid()}",
            new UpdateWorkspaceRequestDto { Name = "Renamed" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenUserIsNotMember_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var outsiderClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        await RegisterAsync(outsiderClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);

        // act
        var response = await outsiderClient.PutAsJsonAsync(
            $"/api/workspaces/{workspace.Id}",
            new UpdateWorkspaceRequestDto { Name = "Hijacked" },
            cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateIcon_WhenAdminUploadsPng_ReturnsWorkspaceWithIcon()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        using var content = CreateIconContent(PngBytes, "image/png", "icon.png");

        // act
        var response = await client.PutAsync($"/api/workspaces/{workspace.Id}/icon", content, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.True(updated!.HasIcon);
    }

    [Fact]
    public async Task UpdateIcon_WithUnsupportedContentType_ReturnsBadRequest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        using var content = CreateIconContent(PngBytes, "image/gif", "icon.gif");

        // act
        var response = await client.PutAsync($"/api/workspaces/{workspace.Id}/icon", content, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateIcon_WhenIconExceedsMaxSize_ReturnsBadRequest()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        using var content = CreateIconContent(new byte[(2 * 1024 * 1024) + 1], "image/png", "big.png");

        // act
        var response = await client.PutAsync($"/api/workspaces/{workspace.Id}/icon", content, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateIcon_WhenUserIsNotAdmin_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var memberClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        var member = await RegisterAsync(memberClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);
        await InviteAndAcceptAsync(ownerClient, memberClient, workspace.Id, member.Email, "Member", cancellationToken);
        using var content = CreateIconContent(PngBytes, "image/png", "icon.png");

        // act
        var response = await memberClient.PutAsync($"/api/workspaces/{workspace.Id}/icon", content, cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetIcon_WhenWorkspaceHasIcon_ReturnsFileWithContentType()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        using var content = CreateIconContent(PngBytes, "image/png", "icon.png");
        (await client.PutAsync($"/api/workspaces/{workspace.Id}/icon", content, cancellationToken)).EnsureSuccessStatusCode();

        // act
        var response = await client.GetAsync($"/api/workspaces/{workspace.Id}/icon", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngBytes, await response.Content.ReadAsByteArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task GetIcon_WhenWorkspaceHasNoIcon_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.GetAsync($"/api/workspaces/{workspace.Id}/icon", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetIcon_WhenUserIsNotMember_ReturnsForbidden()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var ownerClient = _factory.CreateClient();
        var outsiderClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, cancellationToken);
        await RegisterAsync(outsiderClient, cancellationToken);
        var workspace = await CreateWorkspaceAsync(ownerClient, cancellationToken);

        // act
        var response = await outsiderClient.GetAsync($"/api/workspaces/{workspace.Id}/icon", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RemoveIcon_WhenAdmin_ReturnsWorkspaceWithoutIcon()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);
        using var content = CreateIconContent(PngBytes, "image/png", "icon.png");
        (await client.PutAsync($"/api/workspaces/{workspace.Id}/icon", content, cancellationToken)).EnsureSuccessStatusCode();

        // act
        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}/icon", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<WorkspaceDto>(cancellationToken);
        Assert.False(updated!.HasIcon);
    }

    [Fact]
    public async Task RemoveIcon_WhenUserIsNotAdmin_ReturnsForbidden()
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
        var response = await memberClient.DeleteAsync($"/api/workspaces/{workspace.Id}/icon", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenAdmin_ReturnsNoContentAndWorkspaceIsGone()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);
        var workspace = await CreateWorkspaceAsync(client, cancellationToken);

        // act
        var response = await client.DeleteAsync($"/api/workspaces/{workspace.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await client.GetAsync($"/api/workspaces/{workspace.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenUserIsNotAdmin_ReturnsForbidden()
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
        var response = await memberClient.DeleteAsync($"/api/workspaces/{workspace.Id}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenWorkspaceDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();
        await RegisterAsync(client, cancellationToken);

        // act
        var response = await client.DeleteAsync($"/api/workspaces/{Guid.NewGuid()}", cancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
