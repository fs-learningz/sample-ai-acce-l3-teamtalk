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
}
