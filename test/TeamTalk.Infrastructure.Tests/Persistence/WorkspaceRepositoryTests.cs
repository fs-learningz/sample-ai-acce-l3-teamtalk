using TeamTalk.Domain.Entities;
using TeamTalk.Infrastructure.Persistence.Repositories;

namespace TeamTalk.Infrastructure.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public class WorkspaceRepositoryTests
{
    private readonly PostgresContainerFixture _fixture;

    public WorkspaceRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsPersistedWorkspace()
    {
        // arrange
        var creatorId = Guid.NewGuid();
        var workspace = new Workspace(Guid.CreateVersion7(), "Engineering", creatorId, DateTime.UtcNow);

        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            var repository = new WorkspaceRepository(writeDbContext);
            await repository.AddAsync(workspace);
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var readRepository = new WorkspaceRepository(readDbContext);
        var found = await readRepository.GetByIdAsync(workspace.Id);

        // assert
        Assert.NotNull(found);
        Assert.Equal("Engineering", found!.Name);
        Assert.Equal(creatorId, found.CreatedByUserId);
    }

    [Fact]
    public async Task GetForUserAsync_ReturnsOnlyWorkspacesTheUserBelongsTo()
    {
        // arrange
        var userId = Guid.NewGuid();
        var workspace = new Workspace(Guid.CreateVersion7(), "Design", userId, DateTime.UtcNow);
        var member = new WorkspaceMember(Guid.CreateVersion7(), workspace.Id, userId, Domain.Enums.WorkspaceRole.Admin, DateTime.UtcNow);

        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            var workspaceRepository = new WorkspaceRepository(writeDbContext);
            await workspaceRepository.AddAsync(workspace);

            var memberRepository = new WorkspaceMemberRepository(writeDbContext);
            await memberRepository.AddAsync(member);
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var readRepository = new WorkspaceRepository(readDbContext);
        var found = await readRepository.GetForUserAsync(userId);

        // assert
        Assert.Contains(found, w => w.Id == workspace.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenWorkspaceDoesNotExist_ReturnsNull()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceRepository(dbContext).GetByIdAsync(Guid.NewGuid());

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task GetForUserAsync_ReturnsWorkspacesOrderedByCreationDate()
    {
        // arrange
        var userId = Guid.NewGuid();
        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var newer = new Workspace(Guid.CreateVersion7(), "Newer", userId, baseTime.AddDays(1));
        var older = new Workspace(Guid.CreateVersion7(), "Older", userId, baseTime);

        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            var workspaceRepository = new WorkspaceRepository(writeDbContext);
            var memberRepository = new WorkspaceMemberRepository(writeDbContext);
            await workspaceRepository.AddAsync(newer);
            await workspaceRepository.AddAsync(older);
            await memberRepository.AddAsync(new WorkspaceMember(Guid.CreateVersion7(), newer.Id, userId, Domain.Enums.WorkspaceRole.Admin, baseTime));
            await memberRepository.AddAsync(new WorkspaceMember(Guid.CreateVersion7(), older.Id, userId, Domain.Enums.WorkspaceRole.Admin, baseTime));
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceRepository(readDbContext).GetForUserAsync(userId);

        // assert
        Assert.Equal(["Older", "Newer"], found.Select(w => w.Name).ToArray());
    }

    [Fact]
    public async Task GetForUserAsync_WhenUserBelongsToNoWorkspace_ReturnsEmptyList()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceRepository(dbContext).GetForUserAsync(Guid.NewGuid());

        // assert
        Assert.Empty(found);
    }

    [Fact]
    public async Task UpdateAsync_PersistsDetailsAndIcon()
    {
        // arrange
        var workspace = new Workspace(Guid.CreateVersion7(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            await new WorkspaceRepository(writeDbContext).AddAsync(workspace);
        }

        workspace.UpdateDetails("Platform", "The platform team", DateTime.UtcNow);
        workspace.SetIcon([1, 2, 3], "image/png", DateTime.UtcNow);
        await using (var updateDbContext = _fixture.CreateDbContext())
        {
            await new WorkspaceRepository(updateDbContext).UpdateAsync(workspace);
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceRepository(readDbContext).GetByIdAsync(workspace.Id);

        // assert
        Assert.Equal("Platform", found!.Name);
        Assert.Equal("The platform team", found.Description);
        Assert.True(found.HasIcon);
        Assert.Equal(new byte[] { 1, 2, 3 }, found.IconData);
        Assert.Equal("image/png", found.IconContentType);
    }

    [Fact]
    public async Task UpdateAsync_WhenIconIsRemoved_ClearsIconColumns()
    {
        // arrange
        var workspace = new Workspace(Guid.CreateVersion7(), "Engineering", Guid.NewGuid(), DateTime.UtcNow);
        workspace.SetIcon([1, 2, 3], "image/png", DateTime.UtcNow);
        await using (var writeDbContext = _fixture.CreateDbContext())
        {
            await new WorkspaceRepository(writeDbContext).AddAsync(workspace);
        }

        workspace.RemoveIcon(DateTime.UtcNow);
        await using (var updateDbContext = _fixture.CreateDbContext())
        {
            await new WorkspaceRepository(updateDbContext).UpdateAsync(workspace);
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceRepository(readDbContext).GetByIdAsync(workspace.Id);

        // assert
        Assert.False(found!.HasIcon);
        Assert.Null(found.IconContentType);
    }

    [Fact]
    public async Task DeleteAsync_RemovesWorkspaceAndAllDependentData()
    {
        // arrange
        var userId = Guid.NewGuid();
        var workspace = new Workspace(Guid.CreateVersion7(), "Doomed", userId, DateTime.UtcNow);
        var channel = new Channel(Guid.CreateVersion7(), workspace.Id, "general", Domain.Enums.ChannelType.Public, userId, DateTime.UtcNow);
        var message = new Message(Guid.CreateVersion7(), channel.Id, userId, "hello", DateTime.UtcNow);
        var invitation = new WorkspaceInvitation(Guid.CreateVersion7(), workspace.Id, "x@example.com", Domain.Enums.WorkspaceRole.Member, userId, DateTime.UtcNow);
        var survivor = new Workspace(Guid.CreateVersion7(), "Survivor", userId, DateTime.UtcNow);

        await using (var seedDbContext = _fixture.CreateDbContext())
        {
            await new WorkspaceRepository(seedDbContext).AddAsync(workspace);
            await new WorkspaceRepository(seedDbContext).AddAsync(survivor);
            await new WorkspaceMemberRepository(seedDbContext)
                .AddAsync(new WorkspaceMember(Guid.CreateVersion7(), workspace.Id, userId, Domain.Enums.WorkspaceRole.Admin, DateTime.UtcNow));
            await new ChannelRepository(seedDbContext).AddAsync(channel);
            await new ChannelMemberRepository(seedDbContext)
                .AddAsync(new ChannelMember(Guid.CreateVersion7(), channel.Id, userId, DateTime.UtcNow));
            await new MessageRepository(seedDbContext).AddAsync(message);
            await new WorkspaceInvitationRepository(seedDbContext).AddAsync(invitation);
        }

        await using (var deleteDbContext = _fixture.CreateDbContext())
        {
            // act
            await new WorkspaceRepository(deleteDbContext).DeleteAsync(workspace.Id);
        }

        // assert
        await using var readDbContext = _fixture.CreateDbContext();
        Assert.Null(await new WorkspaceRepository(readDbContext).GetByIdAsync(workspace.Id));
        Assert.False(await new WorkspaceMemberRepository(readDbContext).IsMemberAsync(workspace.Id, userId));
        Assert.Null(await new ChannelRepository(readDbContext).GetByIdAsync(channel.Id));
        Assert.False(await new ChannelMemberRepository(readDbContext).IsMemberAsync(channel.Id, userId));
        Assert.Null(await new MessageRepository(readDbContext).GetByIdAsync(message.Id));
        Assert.Null(await new WorkspaceInvitationRepository(readDbContext).GetByIdAsync(invitation.Id));
        Assert.NotNull(await new WorkspaceRepository(readDbContext).GetByIdAsync(survivor.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenWorkspaceDoesNotExist_DoesNotThrow()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var exception = await Record.ExceptionAsync(() => new WorkspaceRepository(dbContext).DeleteAsync(Guid.NewGuid()));

        // assert
        Assert.Null(exception);
    }
}
