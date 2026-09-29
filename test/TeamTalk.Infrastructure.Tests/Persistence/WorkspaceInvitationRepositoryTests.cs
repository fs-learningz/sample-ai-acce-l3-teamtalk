using TeamTalk.Domain.Entities;
using TeamTalk.Domain.Enums;
using TeamTalk.Infrastructure.Persistence.Repositories;

namespace TeamTalk.Infrastructure.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public class WorkspaceInvitationRepositoryTests
{
    private readonly PostgresContainerFixture _fixture;

    public WorkspaceInvitationRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private static WorkspaceInvitation NewInvitation(Guid workspaceId, string email, DateTime createdAtUtc)
    {
        return new WorkspaceInvitation(Guid.CreateVersion7(), workspaceId, email, WorkspaceRole.Member, Guid.NewGuid(), createdAtUtc);
    }

    private async Task SeedAsync(params WorkspaceInvitation[] invitations)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new WorkspaceInvitationRepository(dbContext);

        foreach (var invitation in invitations)
        {
            await repository.AddAsync(invitation);
        }
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsPersistedInvitation()
    {
        // arrange
        var invitation = NewInvitation(Guid.NewGuid(), $"a-{Guid.NewGuid():N}@example.com", DateTime.UtcNow);
        await SeedAsync(invitation);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceInvitationRepository(dbContext).GetByIdAsync(invitation.Id);

        // assert
        Assert.NotNull(found);
        Assert.Equal(invitation.Email, found!.Email);
        Assert.Equal(InvitationStatus.Pending, found.Status);
        Assert.Equal(WorkspaceRole.Member, found.Role);
    }

    [Fact]
    public async Task GetByIdAsync_WhenInvitationDoesNotExist_ReturnsNull()
    {
        // act
        await using var dbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceInvitationRepository(dbContext).GetByIdAsync(Guid.NewGuid());

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task GetPendingForEmailAsync_ReturnsOnlyPendingInvitationsNewestFirst()
    {
        // arrange
        var email = $"target-{Guid.NewGuid():N}@example.com";
        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var older = NewInvitation(Guid.NewGuid(), email, baseTime);
        var newer = NewInvitation(Guid.NewGuid(), email, baseTime.AddHours(1));
        var declined = NewInvitation(Guid.NewGuid(), email, baseTime.AddHours(2));
        declined.Decline(baseTime.AddHours(3));
        var otherEmail = NewInvitation(Guid.NewGuid(), $"other-{Guid.NewGuid():N}@example.com", baseTime);
        await SeedAsync(older, newer, declined, otherEmail);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var pending = await new WorkspaceInvitationRepository(dbContext).GetPendingForEmailAsync(email);

        // assert
        Assert.Equal([newer.Id, older.Id], pending.Select(i => i.Id).ToArray());
    }

    [Fact]
    public async Task GetForWorkspaceAsync_ReturnsAllStatusesForWorkspaceNewestFirst()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var pending = NewInvitation(workspaceId, $"a-{Guid.NewGuid():N}@example.com", baseTime);
        var revoked = NewInvitation(workspaceId, $"b-{Guid.NewGuid():N}@example.com", baseTime.AddHours(1));
        revoked.Revoke(baseTime.AddHours(2));
        var otherWorkspace = NewInvitation(Guid.NewGuid(), $"c-{Guid.NewGuid():N}@example.com", baseTime);
        await SeedAsync(pending, revoked, otherWorkspace);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var invitations = await new WorkspaceInvitationRepository(dbContext).GetForWorkspaceAsync(workspaceId);

        // assert
        Assert.Equal([revoked.Id, pending.Id], invitations.Select(i => i.Id).ToArray());
    }

    [Fact]
    public async Task GetPendingByWorkspaceAndEmailAsync_WhenPending_ReturnsInvitation()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var email = $"a-{Guid.NewGuid():N}@example.com";
        var invitation = NewInvitation(workspaceId, email, DateTime.UtcNow);
        await SeedAsync(invitation);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceInvitationRepository(dbContext).GetPendingByWorkspaceAndEmailAsync(workspaceId, email);

        // assert
        Assert.NotNull(found);
        Assert.Equal(invitation.Id, found!.Id);
    }

    [Fact]
    public async Task GetPendingByWorkspaceAndEmailAsync_WhenInvitationWasRevoked_ReturnsNull()
    {
        // arrange
        var workspaceId = Guid.NewGuid();
        var email = $"a-{Guid.NewGuid():N}@example.com";
        var invitation = NewInvitation(workspaceId, email, DateTime.UtcNow);
        invitation.Revoke(DateTime.UtcNow);
        await SeedAsync(invitation);

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceInvitationRepository(dbContext).GetPendingByWorkspaceAndEmailAsync(workspaceId, email);

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task GetPendingByWorkspaceAndEmailAsync_WhenWorkspaceDiffers_ReturnsNull()
    {
        // arrange
        var email = $"a-{Guid.NewGuid():N}@example.com";
        await SeedAsync(NewInvitation(Guid.NewGuid(), email, DateTime.UtcNow));

        // act
        await using var dbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceInvitationRepository(dbContext).GetPendingByWorkspaceAndEmailAsync(Guid.NewGuid(), email);

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusChange()
    {
        // arrange
        var invitation = NewInvitation(Guid.NewGuid(), $"a-{Guid.NewGuid():N}@example.com", DateTime.UtcNow);
        await SeedAsync(invitation);
        var respondedAtUtc = DateTime.UtcNow;
        invitation.Accept(respondedAtUtc);

        await using (var updateDbContext = _fixture.CreateDbContext())
        {
            await new WorkspaceInvitationRepository(updateDbContext).UpdateAsync(invitation);
        }

        // act
        await using var readDbContext = _fixture.CreateDbContext();
        var found = await new WorkspaceInvitationRepository(readDbContext).GetByIdAsync(invitation.Id);

        // assert
        Assert.Equal(InvitationStatus.Accepted, found!.Status);
        Assert.NotNull(found.RespondedAtUtc);
    }
}
